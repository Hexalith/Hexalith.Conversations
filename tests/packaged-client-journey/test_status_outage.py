import http.client
import http.server
import json
import threading
import unittest

from status_outage import _command_status_relay


class StatusOutageTest(unittest.TestCase):
    def test_only_the_accepted_status_path_is_interrupted(self):
        receipt = {"messageId": "accepted/receipt"}

        class Gateway(http.server.BaseHTTPRequestHandler):
            def log_message(self, *args):
                pass

            def do_POST(self):
                self.rfile.read(int(self.headers["Content-Length"]))
                self.send_response(202)
                self.end_headers()
                self.wfile.write(json.dumps(receipt).encode())

        with http.server.ThreadingHTTPServer(("127.0.0.1", 0), Gateway) as gateway:
            worker = threading.Thread(target=gateway.serve_forever)
            worker.start()
            try:
                binding = {"rootdigest": "root", "activationbindingdigest": "activation", "epoch": 1}
                with _command_status_relay(gateway.server_port, binding) as (port, interrupted):
                    connection = http.client.HTTPConnection("127.0.0.1", port, timeout=5)
                    try:
                        status_path = "/api/v1/commands/status/accepted%2Freceipt"
                        gateway_headers = {"dapr-app-id": "eventstore"}
                        connection.request("GET", status_path, headers=gateway_headers)
                        self.assert_response(connection, 404)
                        connection.request("POST", "/api/v1/commands/submit", b"{}", gateway_headers)
                        self.assertEqual(json.loads(self.assert_response(connection, 202)), receipt)
                        connection.request("GET", "/api/v1/commands/status/unrelated", headers=gateway_headers)
                        self.assert_response(connection, 404)
                        connection.request("GET", status_path, headers={"dapr-app-id": "another-app"})
                        self.assert_response(connection, 404)
                        connection.request("GET", status_path)
                        self.assert_response(connection, 404)
                        self.assertFalse(interrupted.is_set())
                        connection.request("GET", status_path, headers=gateway_headers)
                        self.assert_response(connection, 503)
                        self.assertTrue(interrupted.wait(timeout=2))
                    finally:
                        connection.close()
            finally:
                gateway.shutdown()
                worker.join()

    def assert_response(self, connection, expected_status):
        with connection.getresponse() as response:
            self.assertEqual(response.status, expected_status)
            return response.read()


if __name__ == "__main__":
    unittest.main()
