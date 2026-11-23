---
title: 'Resolve five deferred admission and read consistency findings'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Address five deferred findings about trustworthy conversation reads and writes. Verify the authenticated HTTP behavior for legacy detail requests and bound global administrators, compare stored summaries by value, bind newly recorded Agent removals to their authorized event identity, and verify the exact native tenant permission proof returned by a successful resolution metadata query.

</frozen-after-approval>

## Implementation Notes

- Entry revision `21c102202ce5ebe553d418522973deffea73a6a1`. Use routine current-change validation.
- Use `GetConversationDomainQueryHandler` and the existing authenticated SDK HTTP fixture to settle BH-05 and BH-06. Keep delegated and bounded-purpose checks and enforce ordinary tenant Read for a bound global administrator.
- Replace the JSON round trip in `ConversationProjectionReadStore.SameSummary` with value comparisons, including ordered Party IDs and provider extension keys and values. Exercise the detail and page paths.
- Check newly recorded `RemoveAgentParticipant` event identities against the existing client allocation and verified source prefix in `ConversationAgentAdmissionStage`. Preserve historical no-op behavior.
- Extend the native resolution query assertions to compare the complete permission proof with the tenant evidence supplied through the production adapter. Append five dispositions to the deferred ledger.

## Review Triage Log

- Medium, patched. The detail read service maps a summary mismatch to `Rebuilding`. The revised test verifies that response and independently checks that the page rejects the row.
- Medium, resolved. The ledger and Platform handoff record the gateway caller binding required by the supported pilot. The authenticated receiver test covers the authority-free denial.
- Medium, patched. The selector check preserves the existing direct owner call. A direct SDK dispatch test covers that path.
- Medium, patched. The HTTP cases cover verified administrators with and without tenant Read, a refused administrator assertion, both bounded purposes, and delegated Projects detail.
- Medium, patched. The Client transport test pins the removal command and event IDs and compares the original public command on retry.
- Medium, patched. The native fixture advances the tenant proof between successful queries and compares each complete proof with the returned permission version.

## Verification

- The Release solution built with zero warnings and errors. All 1,782 Server, 69 Client, 638 Contracts, and 189 domain tests passed with zero skips.
- The indexed root-submodule checker, both Story 7.3 workflow scenarios, the staged whitespace check, and the Builds-pinned commitlint check passed.
- The Aspire CLI built the baseline AppHost and stopped on the read-only user cache before resource inspection. The shared local checker on clean entry `main` could not clone the CI policy branch in its isolated Git environment. Its anonymous Git transport could not reach GitHub directly from this workspace.
