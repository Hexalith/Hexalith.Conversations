import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch

from run import main, verify_host_configuration
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
        ttl, retention = "EventStore__CommandStatus__TtlSeconds", "EventStore__Idempotency__TerminalRetentionSeconds"
        gateway = {ttl: "90000", retention: "90000"}
        cases = (
            (gateway, reader, {}, None),
            ({ttl: "90001", retention: "90001"}, reader | {prefix + "1__TenantId": "first-tenant"}, {}, None),
            (gateway | {retention: "2147483647"}, reader, {}, None),
            (gateway | {ttl: "86400"}, reader, {}, "retain command status"),
            (gateway | {ttl: "invalid"}, reader, {}, "retain command status"),
            ({retention: "90000"}, reader, {}, "retain command status"),
            ({ttl: "90000"}, reader, {}, "terminal idempotency"),
            (gateway | {retention: "86400"}, reader, {}, "terminal idempotency"),
            (gateway | {ttl: "90001"}, reader, {}, "terminal idempotency"),
            ({ttl: "090000", retention: "00000090000"}, reader, {}, None),
            (gateway | {retention: "21474836470"}, reader, {}, "terminal idempotency"),
            (gateway | {retention: "2147483648"}, reader, {}, "terminal idempotency"),
            (gateway | {retention: "-90000"}, reader, {}, "terminal idempotency"),
            (gateway, {}, {}, "one exact Party binding"),
            (gateway, reader | {prefix + "0__PartyId": "wrong-party"}, {}, "one exact Party binding"),
            (gateway, reader | duplicate, {}, "one exact Party binding"),
            (gateway, reader, {"CONVERSATIONS_DOMAIN_DAPR_ORIGIN": "http://localhost:3506"}, "selected domain sidecar"),
            (gateway, reader, {"CONVERSATIONS_ORIGIN": "https://localhost:7481"}, "selected pilot API"),
        )
        for settings, domain, overrides, error in cases:
            with self.subTest(gateway=settings, error=error), patch("run.describe_resource", side_effect=[
                {"environment": settings},
                {"environment": {"DAPR_HTTP_PORT": "3505"} | domain},
                {"environment": {"DAPR_HTTP_PORT": "3510"},
                 "urls": [{"url": "https://localhost:7480/"}, {"url": "https://localhost:7481/", "isInternal": True}]},
            ]):
                if error is None:
                    self.assertEqual(verify_host_configuration(Path("apphost.cs"), inputs | overrides),
                                     {"DAPR_HTTP_PORT": "3510"})
                else:
                    with self.assertRaisesRegex(RuntimeError, error):
                        verify_host_configuration(Path("apphost.cs"), inputs | overrides)

    def test_describe_requires_the_named_running_resource(self):
        running = {"name": "conversations", "state": "Running", "environment": {}}
        isolated = running | {"name": "conversations-dfxmmxfb", "displayName": "conversations"}
        unnamed = running | {"displayName": None}
        cases = ([running], [isolated], [unnamed], [], [running, running], [running | {"state": "Exited"}],
                 [running | {"name": "eventstore"}], [running | {"displayName": "eventstore"}])
        for resources in cases:
            with self.subTest(resources=resources), patch("status_outage.subprocess.run", return_value=Mock(
                stdout=json.dumps({"resources": resources}),
            )):
                if resources in ([running], [isolated], [unnamed]):
                    self.assertEqual(describe_resource(Path("apphost.cs"), "conversations"), resources[0])
                else:
                    with self.assertRaisesRegex(RuntimeError, "one running conversations"):
                        describe_resource(Path("apphost.cs"), "conversations")


class JourneyRunnerTest(unittest.TestCase):
    def test_full_run_rejects_short_status_ttl_before_starting_outage_fixture(self):
        version = "1.0.0"
        packages = ("Hexalith.Conversations.Client", "Hexalith.Conversations.Contracts")
        with tempfile.TemporaryDirectory() as temp:
            feed = Path(temp)
            for package in packages:
                (feed / f"{package}.{version}.nupkg").touch()
            apphost = feed / "apphost.cs"
            apphost.touch()
            api = feed / "Hexalith.Conversations.Api.dll"
            api.touch()

            def dotnet(command, *, cwd, **_):
                if command[1] == "restore":
                    assets = cwd / "obj" / "project.assets.json"
                    assets.parent.mkdir()
                    assets.write_text(json.dumps({"libraries": {
                        f"{package}/{version}": {"type": "package"} for package in packages
                    }}))

            args = ["run.py", "--feed", str(feed), "--version", version,
                    "--apphost", str(apphost), "--fault-api-dll", str(api)]
            gateway = {"environment": {"EventStore__CommandStatus__TtlSeconds": "89999",
                                       "EventStore__Idempotency__TerminalRetentionSeconds": "90000"}}
            with (patch.object(sys, "argv", args), patch("run.subprocess.run", side_effect=dotnet) as build,
                  patch("run.describe_resource", return_value=gateway) as describe,
                  patch("run.interrupted_status") as outage):
                with self.assertRaisesRegex(RuntimeError, "retain command status"):
                    main()
                describe.assert_called_once_with(apphost.resolve(), "eventstore")
                outage.assert_not_called()
                self.assertEqual([call.args[0][1] for call in build.call_args_list], ["restore", "build"])

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
