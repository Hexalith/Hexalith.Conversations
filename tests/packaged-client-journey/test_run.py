import json
from pathlib import Path
import subprocess
import sys
import unittest
from unittest.mock import Mock, patch

from run import verify_host_configuration
from status_outage import describe_resource


class HostConfigurationTest(unittest.TestCase):
    def test_reader_binding_and_effective_gateway_retention(self):
        inputs = {
            "CONVERSATIONS_OTHER_TENANT": "second-tenant",
            "CONVERSATIONS_OTHER_SUBJECT": "reader",
            "CONVERSATIONS_OTHER_PARTY": "reader-party",
            "CONVERSATIONS_DOMAIN_DAPR_ORIGIN": "http://127.0.0.1:3505",
            "CONVERSATIONS_ORIGIN": "https://localhost:7480",
        }
        prefix = "Conversations__Pilot__HumanPartyBindings__"
        reader = {prefix + "0__TenantId": "second-tenant", prefix + "0__PrincipalId": "reader",
                  prefix + "0__PartyId": "reader-party"}
        duplicate = {key.replace("__0__", "__named__"): value for key, value in reader.items()}
        cases = (
            ("90000", reader, {}, None),
            ("90001", reader | {prefix + "1__TenantId": "first-tenant"}, {}, None),
            ("86400", reader, {}, "retain command status"),
            ("invalid", reader, {}, "retain command status"),
            ("90000", {}, {}, "one exact Party binding"),
            ("90000", reader | {prefix + "0__PartyId": "wrong-party"}, {}, "one exact Party binding"),
            ("90000", reader | duplicate, {}, "one exact Party binding"),
            ("90000", reader, {"CONVERSATIONS_DOMAIN_DAPR_ORIGIN": "http://localhost:3506"}, "selected domain sidecar"),
            ("90000", reader, {"CONVERSATIONS_ORIGIN": "https://localhost:7481"}, "selected pilot API"),
        )
        for ttl, domain, overrides, error in cases:
            with self.subTest(ttl=ttl, error=error), patch("run.describe_resource", side_effect=[
                {"environment": {"EventStore__CommandStatus__TtlSeconds": ttl}},
                {"environment": {"DAPR_HTTP_PORT": "3505"} | domain},
                {"environment": {"DAPR_HTTP_PORT": "3510"},
                 "urls": [{"url": "https://localhost:7480/", "isInternal": False}]},
            ]):
                if error is None:
                    self.assertEqual(verify_host_configuration(Path("apphost.cs"), inputs | overrides),
                                     {"DAPR_HTTP_PORT": "3510"})
                else:
                    with self.assertRaisesRegex(RuntimeError, error):
                        verify_host_configuration(Path("apphost.cs"), inputs | overrides)

    def test_describe_requires_the_named_running_resource(self):
        running = {"name": "conversations", "state": "Running", "environment": {}}
        cases = ([running], [], [running, running], [running | {"state": "Exited"}],
                 [running | {"name": "eventstore"}])
        for resources in cases:
            with self.subTest(resources=resources), patch("status_outage.subprocess.run", return_value=Mock(
                stdout=json.dumps({"resources": resources}),
            )):
                if resources == [running]:
                    self.assertEqual(describe_resource(Path("apphost.cs"), "conversations"), running)
                else:
                    with self.assertRaisesRegex(RuntimeError, "one running conversations"):
                        describe_resource(Path("apphost.cs"), "conversations")


class JourneyRunnerTest(unittest.TestCase):
    def test_live_modes_require_their_matching_arguments(self):
        runner = str(Path(__file__).with_name("run.py"))
        base = [sys.executable, runner, "--feed", "unused", "--version", "1.0.0"]
        cases = (
            ([], "Full qualification requires"),
            (["--fault-api-dll", "api"], "Full qualification requires"),
            (["--apphost", "app"], "Full qualification requires"),
            (["--build-only", "--apphost", "app"], "only used for full qualification"),
            (["--without-status-outage", "--fault-api-dll", "api"], "only used for full qualification"),
            (["--build-only", "--without-status-outage"], "not allowed with argument"),
        )
        for options, expected in cases:
            with self.subTest(options=options):
                result = subprocess.run(base + options, capture_output=True, text=True, timeout=10)
                self.assertEqual(result.returncode, 2)
                self.assertIn(expected, result.stderr)
