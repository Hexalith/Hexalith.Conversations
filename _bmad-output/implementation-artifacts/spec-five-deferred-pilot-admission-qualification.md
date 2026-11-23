---
title: 'Qualify pilot command admission and replay'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Resolve five open Story 9.3 findings about pilot admission and replay. Return the ordinary tenant denial for a verified pre-processing authority failure, and verify the owner's private HTTP response for that failure. Configure the qualification host to retain command status throughout the replay window. Launch the candidate source declaration, and prove that the reader has an enrolled Party before testing the tenant-role denial.

</frozen-after-approval>

## Implementation Notes

- Entry revision `7c35922c34cc787b6874c5ba46c091fa2c8ae6b5`. The pinned EventStore gateway marks a verified owner deferral as recoverable and returns HTTP 503 with reason `owner_admission_deferred`. Translate that exact pre-receipt response in `PilotCommands.SubmitAsync`. Exercise it beside ordinary denial and ambiguous failures.
- Exercise the Server entry point's pre-processing HTTP response with an exact Party binding and a temporarily unavailable tenant-access adapter. Check the status, empty body, and marker.
- In the qualification AppHost, select the manifest through the verified source entry and require the composed pilot API. Configure the EventStore gateway status TTL with room beyond the 24-hour mutation receipt. Check the effective domain Party assignment against the reader identity used by the installed client.
- Update the pilot handoff and append five dispositions to the deferred work ledger. Use routine current-change validation.

## Review Triage Log

- Medium, patched. Resolve the API URL and sidecar port from the same AppHost whose gateway retention and Party configuration the full runner inspects.
- Medium, patched. Require the requested resource to be running before drawing conclusions from its environment.
- Medium, patched. Match all immediate Party binding children, including named ones, and refuse duplicate bindings as the domain does.
- Medium, patched. Check the durable healthy `TenantReader` membership before the package consumer sends the denied create.
- Low, patched. Document the mapping and source-root inputs before giving the launch command.
- Low, patched. Record five dispositions in the deferred work ledger.

## Verification

- The Release Server build reported zero warnings and errors. All 1,765 Server tests passed with zero skips, including the authenticated `/process` test and the public denial and uncertainty cases.
- The qualification AppHost compiled against the pinned Platform and Builds sources. Four runner tests passed, and the standalone consumer installed the candidate packages into an empty cache and built without warnings.
- The indexed root-submodule check, both Story 7.3 workflow scenarios and the whitespace check passed. This freeform follow-up uses routine current-change validation.
- The Aspire baseline built the AppHost and then stopped on its read-only `/home/agent/.aspire/cli/bch` cache. The full live journey remains unverified. The shared checker on entry `main` failed while cloning the anonymous CiCd policy branch in its isolated Git environment.
