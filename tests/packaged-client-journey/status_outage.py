"""Local fixture for losing status confirmation after the real gateway accepts a command."""
from contextlib import contextmanager
import http.client
import http.server
import json
import os
from pathlib import Path
import socket
import subprocess
import threading
import time
import urllib.parse
import urllib.request

from provision import NoRedirect


@contextmanager
def _command_status_relay(port, binding):
    expected_status_path = None
    receipt_lock = threading.Lock()
    interrupted = threading.Event()

    class Relay(http.server.BaseHTTPRequestHandler):
        def log_message(self, *args):
            pass

        def do_POST(self):
            nonlocal expected_status_path
            if self.path != "/api/v1/commands/submit" or self.headers.get("dapr-app-id") != "eventstore":
                self.send_error(404)
                return
            # Only carry this API's human command to its own genuine sidecar.
            headers = {key: self.headers[key] for key in ("Authorization", "Content-Type", "dapr-app-id", "dapr-api-token")
                       if key in self.headers}
            headers.update({
                "X-Hexalith-Route-Catalog-Root": binding["rootdigest"],
                "X-Hexalith-Route-Catalog-Binding": binding["activationbindingdigest"],
                "X-Hexalith-Route-Catalog-Epoch": str(binding["epoch"]),
            })
            if self.headers.get("Transfer-Encoding", "").lower() == "chunked":
                chunks = bytearray()
                while True:
                    size = int(self.rfile.readline(128).split(b";", 1)[0].strip(), 16)
                    if size == 0:
                        self.rfile.readline(128)
                        break
                    if size < 0 or len(chunks) + size > 1_048_576:
                        self.send_error(400)
                        return
                    chunks.extend(self.rfile.read(size))
                    if self.rfile.read(2) != b"\r\n":
                        self.send_error(400)
                        return
                body = bytes(chunks)
            else:
                size = int(self.headers.get("Content-Length", "0"))
                if not 0 < size <= 1_048_576:
                    self.send_error(400)
                    return
                body = self.rfile.read(size)
            upstream = http.client.HTTPConnection("127.0.0.1", port, timeout=35)
            try:
                upstream.request("POST", self.path, body, headers)
                result = upstream.getresponse()
                body = result.read()
                if result.status != 202:
                    raise RuntimeError(f"The real gateway refused the interruption fixture submission with HTTP {result.status}")
                receipt = json.loads(body)
                message_id = receipt.get("messageId") if isinstance(receipt, dict) else None
                if not isinstance(message_id, str) or not message_id.strip():
                    raise RuntimeError("The gateway receipt did not identify the accepted command")
                status_path = "/api/v1/commands/status/" + urllib.parse.quote(message_id, safe="")
                with receipt_lock:
                    if expected_status_path is not None and expected_status_path != status_path:
                        raise RuntimeError("The outage fixture received a receipt for another command")
                    expected_status_path = status_path
                self.send_response(result.status)
                self.send_header("Content-Type", result.getheader("Content-Type", "application/json"))
                self.send_header("Content-Length", str(len(body)))
                self.end_headers()
                self.wfile.write(body)
            finally:
                upstream.close()

        def do_GET(self):
            with receipt_lock:
                matches_receipt = self.path == expected_status_path and self.headers.get("dapr-app-id") == "eventstore"
            if matches_receipt:
                self.send_error(503)
                interrupted.set()
            else:
                self.send_error(404)

    with http.server.ThreadingHTTPServer(("127.0.0.1", 0), Relay) as proxy:
        server_thread = threading.Thread(target=proxy.serve_forever, daemon=True)
        server_thread.start()
        try:
            yield proxy.server_port, interrupted
        finally:
            proxy.shutdown()
            server_thread.join()


@contextmanager
def interrupted_status(apphost, api_dll):
    description = subprocess.run(
        ["aspire", "describe", "conversations-api", "--apphost", str(apphost), "--format", "Json", "--non-interactive"],
        check=True, capture_output=True, text=True, timeout=20)
    service = json.loads(description.stdout)["resources"][0]
    settings = service["environment"]
    port = int(settings["DAPR_HTTP_PORT"])
    receipt = json.loads(Path(os.environ["CONVERSATIONS_CATALOG_RECEIPT"]).read_text())
    binding = {key.lower(): value for key, value in receipt.items()}

    with _command_status_relay(port, binding) as (proxy_port, interrupted):
        with socket.socket() as available:
            available.bind(("127.0.0.1", 0))
            origin = "http://127.0.0.1:" + str(available.getsockname()[1])
        env = {key: value for key, value in os.environ.items()
               if not key.upper().startswith(("CONVERSATIONS_", "PLATFORM__", "DAPR_", "ASPNETCORE_", "AUTHENTICATION__"))}
        env.update({key: value for key, value in settings.items()
                    if key.startswith("Authentication__JwtBearer__") or key == "DAPR_API_TOKEN"})
        env.update(ASPNETCORE_URLS=origin, ASPNETCORE_ENVIRONMENT="Development",
                   DOTNET_ENVIRONMENT="Development", DAPR_HTTP_ENDPOINT=f"http://127.0.0.1:{proxy_port}")
        child = subprocess.Popen(["dotnet", str(api_dll)], env=env, cwd=api_dll.parent, stdout=subprocess.DEVNULL)
        try:
            client = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())
            deadline = time.monotonic() + 20
            while time.monotonic() < deadline and child.poll() is None:
                try:
                    with client.open(origin + "/ready", timeout=1) as response:
                        if response.status == 200:
                            break
                except OSError:
                    time.sleep(0.1)
            else:
                raise RuntimeError("The local status-interruption API did not become ready")
            yield origin
            if not interrupted.is_set():
                raise RuntimeError("No status read for the accepted gateway receipt was interrupted")
        finally:
            child.terminate()
            try:
                child.wait(timeout=8)
            except subprocess.TimeoutExpired:
                child.kill()
                child.wait()
