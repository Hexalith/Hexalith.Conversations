"""Provision two fresh proof tenants on a disposable local Platform deployment."""
import json
import os
from pathlib import Path
import secrets
import time
import urllib.error
import urllib.parse
import urllib.request


def loopback_origin(name):
    value = os.environ[name]
    parts = urllib.parse.urlsplit(value)
    if (parts.scheme not in {"http", "https"} or parts.hostname not in {"localhost", "127.0.0.1", "::1"}
            or parts.username or parts.password or parts.path not in {"", "/"} or parts.query or parts.fragment):
        raise ValueError(f"{name} must name a loopback service origin")
    return value.rstrip("/")


def message_id():
    # Encode the gateway's 128-bit ULID with its 48-bit millisecond timestamp.
    value = (int(time.time() * 1000) << 80) | secrets.randbits(80)
    alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"
    return "".join(alphabet[(value >> (5 * digit)) & 31] for digit in reversed(range(26)))


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


transport = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())


def request(url, headers, payload=None):
    data = None if payload is None else json.dumps(payload).encode()
    req = urllib.request.Request(url, data=data, headers=headers)
    try:
        response = transport.open(req, timeout=10)
    except urllib.error.HTTPError as error:
        return error.code, {}
    with response:
        body = response.read()
        return response.status, json.loads(body) if body else None


def submit(origin, headers, tenant, operation, payload):
    key = message_id()
    status, receipt = request(origin + "/api/v1/commands", headers, {
        "messageId": key, "correlationId": key, "tenant": "platform:tenants",
        "domain": "tenants", "aggregateId": tenant, "commandType": operation, "payload": payload,
    })
    if status != 202:
        raise RuntimeError(f"{operation} admission returned HTTP {status}")
    message = urllib.parse.quote(receipt["messageId"], safe="")
    deadline = time.monotonic() + 65
    while time.monotonic() < deadline:
        status, record = request(origin + "/api/v1/commands/status/" + message, headers)
        if status == 200:
            if record["status"] == "Completed":
                return
            if record["status"] in {"Rejected", "TimedOut", "PublishFailed"}:
                raise RuntimeError(f'{operation} ended with {record["status"]}. Use a fresh deployment.')
        elif status != 404:
            raise RuntimeError(f"{operation} confirmation returned HTTP {status}")
        time.sleep(0.35)
    raise TimeoutError(f"{operation} did not complete within 65 seconds")


def main():
    gateway = loopback_origin("CONVERSATIONS_GATEWAY_ORIGIN")
    sidecar = loopback_origin("CONVERSATIONS_DOMAIN_DAPR_ORIGIN")
    store = urllib.parse.quote(os.environ["CONVERSATIONS_READ_MODEL_STORE"], safe="")
    state_headers = {}
    if token := os.environ.get("CONVERSATIONS_DOMAIN_DAPR_TOKEN"):
        state_headers["dapr-api-token"] = token
    receipt = json.loads(Path(os.environ["CONVERSATIONS_CATALOG_RECEIPT"]).read_text())
    receipt = {key.lower(): value for key, value in receipt.items()}
    headers = {
        "Authorization": "Bearer " + os.environ["CONVERSATIONS_ADMIN_TOKEN"],
        "Content-Type": "application/json", "X-Hexalith-Scope": "platform:tenants",
        "X-Hexalith-Route-Catalog-Root": receipt["rootdigest"],
        "X-Hexalith-Route-Catalog-Binding": receipt["activationbindingdigest"],
        "X-Hexalith-Route-Catalog-Epoch": str(receipt["epoch"]),
    }
    tenants = [(os.environ["CONVERSATIONS_TENANT"], os.environ["CONVERSATIONS_SUBJECT"], "TenantContributor"),
               (os.environ["CONVERSATIONS_OTHER_TENANT"], os.environ["CONVERSATIONS_OTHER_SUBJECT"], "TenantReader")]
    if tenants[0][0] == tenants[1][0] or tenants[0][1] == tenants[1][1]:
        raise ValueError("The proof needs distinct tenants and subjects")
    for tenant, subject, role in tenants:
        if len(tenant) > 128 or not tenant or any(ch not in "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._-" for ch in tenant):
            raise ValueError("Use a simple local tenant identifier of at most 128 characters")
        submit(gateway, headers, tenant, "create-tenant", {"tenantId": tenant, "name": tenant, "description": "Local package qualification"})
        submit(gateway, headers, tenant, "add-user-to-tenant", {"tenantId": tenant, "userId": subject, "role": role})
        deadline = time.monotonic() + 60
        while time.monotonic() < deadline:
            status, record = request(f"{sidecar}/v1.0/state/{store}/tenants-projection:v1:{tenant}?consistency=strong", state_headers)
            if (status == 200 and record.get("isHealthy") is True
                    and record.get("state", {}).get("members", {}).get(subject) == role):
                break
            if status not in {200, 204, 404}:
                raise RuntimeError(f"Tenant checkpoint read returned HTTP {status}")
            time.sleep(0.35)
        else:
            raise TimeoutError(f"Healthy {role} membership did not reach the domain within 60 seconds")
    print("Both Tenants memberships committed and reached the domain.")


if __name__ == "__main__":
    main()
