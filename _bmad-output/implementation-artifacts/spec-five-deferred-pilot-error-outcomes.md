---
title: 'Preserve accurate pilot API and client error outcomes'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Close five outstanding findings from the packaged-client review. Preserve typed mutation failures in the client. Return safe correlation IDs on rejected pilot requests, give expired receipts terminal reconciliation guidance, preserve pre-admission size refusals, and report malformed owner list data as unavailable.

</frozen-after-approval>

## Implementation Notes

- Entry revision is `ae91ab5d8712bec1c76ecdc344c586ebce93c0f8`. The five findings share the pilot's public error boundary. One was recorded in the deferred ledger for Story 9.4 and four remain open in the Story 9.3 review. This batch addresses those findings without completing either story.
- In `ConversationClient.SendAsync`, read each HTTP response body once. Cover exact status, error code, wire correlation, retryability, and action on the object returned to the consumer, including the existing typed read outcomes on non-success statuses.
- In `PilotCommands`, use the shared error safety rules to select the error correlation from command metadata, a single header, or the request trace in that order. An expired receipt keeps its existing wire status and error code and directs reconciliation. A gateway 413 returns a nonretryable validation outcome before any status poll.
- In `PilotQueries.ListAsync`, handle invalid input at filter and page parsing. Cover malformed downstream data as unavailable and confirm input validation stops before owner dispatch.
- Use `PilotApiTests`, `ConversationClientTest`, and the packaged-client journey for focused verification. Append only the five dispositions to the deferred ledger and follow the routine current-change validation runbook.

- The client streams successful responses and command errors into their expected type. A failed read can carry either a read result or an error envelope, so the client parses that body once and tries both supported types. An unreadable HTTP 413 body still yields nonretryable validation guidance.
- An expired receipt preserves the existing HTTP 409 code. Its safe message and developer guidance direct reconciliation, and the optional action is absent. The retryable uncertainty response still carries `retry-same-request`.
- The public error correlation and EventStore submission correlation serve different purposes. The fallback is only used in error construction. The owner list must explicitly supply a row array and a matching returned count before the pilot publishes the page.

## Review Triage Log

- Medium, patched. Missing or null owner row arrays normalized to empty before validation. The pilot now checks the wire shape first.
- Medium, patched. A returned count could contradict the owner row count. The pilot now returns unavailable for that outcome.
- Medium, patched. Safe error correlation initially changed gateway submissions. The gateway retains the admitted command correlation, with a separate public fallback verified by a regression.
- Medium, patched. A read failure after HTTP 413 headers lost the definitive refusal. The client now retains the status and its resize guidance.
- Medium, patched. A bare HTTP 413 with unsafe metadata could throw while constructing a fallback. The client and server share the contract's correlation safety rules.
- Medium, patched. The integration guide now uses retryability and the retry action to distinguish retry from reconciliation.
- Medium, verified. The installed-package journey asserts the typed missing-key response. A disposable package consumer exercises both wire failures, including the expired receipt, with a fresh NuGet cache.
- Low, patched. The deferred ledger records all five resolutions.

## Verification

- Release builds passed with zero warnings. Client reported 68 passing tests and Server reported 1,754, with zero failures or skips. The package-only journey restored and compiled in a fresh adopter cache. A second isolated adopter ran both typed mutation cases against controlled HTTP responses.
- The indexed root submodule check, both Story 7.3 workflow scenarios, and whitespace validation passed. This freeform batch uses routine current-change validation.
- Aspire built the AppHost, then `aspire run --apphost src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` exited 2 because `/home/agent/.aspire/cli/bch` is read-only. The live packaged journey was not run.
- `npm test` exits 1 on scripts absent from the entry revision's tracked tooling tree. The shared pre-push checker on the clean entry revision exited 1 when its anonymous clone of `delivered/Hexalith.Conversations` could not connect to GitHub. Its isolated Git environment omits the workspace's network proxy.
