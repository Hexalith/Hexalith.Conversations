---
title: 'Strengthen packaged client qualification for pilot mutations'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Resolve five open Story 9.3 findings that weaken qualification of the packaged client’s pilot mutations. Require the outage scenario for a full run, match the interrupted status lookup to the accepted gateway receipt, assert the exact create and append results on first acceptance and replay, prove denied append IDs are absent from durable state, and verify the pilot rejects completed statuses for a different command.

</frozen-after-approval>

## Implementation Notes

- Entry commit `35053bbef6c8ea30c56be124e278e14e0f3aacb4`. Scope comes from the five matching findings in the Story 9.3 review. This batch changes qualification coverage and operator commands.
- `tests/packaged-client-journey/run.py` distinguishes full, partial and build-only invocations. Document the full command in `docs/integration-guide.md` and align both commands in `module/platform-handoff.md`.
- `tests/packaged-client-journey/Program.cs` checks typed success status, operation, key and identity on both submissions. Give each denied append a fresh ID and read the durable detail key in both relevant tenant scopes.
- `tests/packaged-client-journey/status_outage.py` verifies the real receipt's message ID and records only a GET for that escaped ID. Exercise the relay through its HTTP surface with a controlled local upstream.
- `tests/Hexalith.Conversations.Server.Tests/Api/PilotApiTests.cs` tests receipt, tenant, domain and aggregate mismatches independently against the existing positive confirmation path. Record the five dispositions in the deferred ledger.

## Review Triage Log

- Medium, patched. A dedicated outage conversation leaves the ordinary append fresh. Both scenarios check the installed client’s typed receipts and the outage checks for one durable message after replay.
- Medium, patched. A durable projection alone cannot establish that a denied append never reached the owner. The journey sends the denials before the successful write in the owning conversation, and the two server regressions verify zero gateway calls on denial.
- Medium, patched. The status relay now requires the EventStore target and the escaped receipt path. Loopback requests with wrong and absent targets leave the interruption unset.
- Low, patched. The consumer checks the expected schema and visibility caveat on every successful mutation response.
- Low, patched. The integration guide gives the focused Python command and the runner suite covers six invalid mode combinations.
- Low, patched. The ledger records the five dispositions and links this verification. The source review remains the record of the original findings.

## Verification

- The Server Release build succeeded with zero warnings. The Server suite reported 1,760 passed, zero failed, and zero skipped, including the four unrelated-status and two pre-dispatch denial cases.
- The runner installed client and contracts packages `2.1.1-qualification.1` in an empty adopter cache and compiled the standalone consumer without warnings. The focused Python suite passed both tests, including the six argument cases. The root submodule check, both Story 7.3 workflow scenarios, and whitespace validation passed.
- The Aspire baseline command built the AppHost, then exited 2 while opening the read-only `/home/agent/.aspire/cli/bch`. The full live Platform qualification remains unverified.
- The shared pre-push checker on clean entry `main` exited 1 because it could not anonymously clone `delivered/Hexalith.Conversations` from Hexalith.CiCd. A direct anonymous Git probe also failed to connect to GitHub on port 443 outside the command sandbox.
