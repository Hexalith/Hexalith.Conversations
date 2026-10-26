# Independent review 12

Input: `/tmp/epic16-review-next-5pl7ogai/complete.diff`, 69,035,776 bytes,
SHA-256 `b4274da824041f26c761e66bfff6992220f9dfbbcf8487624738e06e65002e2a`.
All three context-free reviewers were launched before any result was handled.

## Blind hunter — raw final report

- Finding floor: 69,035,776 bytes ÷ 1,000 = 69,035.776 kB; N = min(⌊√69,035.776 + 1⌋, 10) = 10.
- `_bmad/scripts/epic16_native_helper/Program.cs:226` recognizes source-stamp attributes solely by namespace/name. A locally defined `AssemblyInformationalVersionAttribute` was accepted without any genuine framework version attribute. Validate attribute ownership and constructor identity; the successful counterexample is `/tmp/epic16-review12-native-pu9vua9v`.
- `_bmad/scripts/epic16_native_helper/Program.cs:198` validates Task/ValueTask ownership using only the assembly’s simple name. An unsigned `System.Threading.Tasks, Version=1.0.0.0` containing a custom Task class passes inspection. Require approved framework identities so the documented async-return boundary cannot admit unrelated types.
- `_bmad/scripts/epic16_native_helper/TestConstruction.cs:116` accepts any external attribute ending in `]System.ParamArrayAttribute`. A metadata fixture with a foreign attribute passes inspection, while the pinned xUnit runner fails with “constructor parameters did not have matching fixture data: String[] values.” Validate framework ownership; retained reproduction: `/tmp/epic16-review12-params-557y69en/probe/run.stdout`.
- `_bmad/scripts/epic16_measurements.py:145` does not establish that reverse saves actually executed against shared persisted state. It compares declared save order, `staleAccepted`, and decision dictionaries, without CAS expectations/results, stored snapshots, or distinct replica read identities. Add those observations for the disable, membership, role, and configuration schedules.
- `_bmad/scripts/epic16_measurements.py:73` accepts the authorization matrix as standalone `{state, allowed, lastContiguousSequence}` assertions. In particular, Unknown and Stale denial have no underlying snapshot, freshness calculation, or executed consumer request. Bind those rows to persisted inputs and observed authorization responses.
- `_bmad/scripts/epic16_measurements.py:108` skips execution evidence for provider failure and retry exhaustion after checking booleans, an attempt count, and null fields. Unlike successful outcomes, these cases cannot demonstrate that delivery processing ran, three attempts occurred, or acknowledgement was withheld after failure. Retain ordered attempt/completion observations and unchanged persisted state.
- `tests/tooling/test_ext_conv_ai_1_installed_delivery_packet.py:9` makes default pytest discovery depend on an administrator’s absolute external evidence directory. A fresh checkout fails during `setUp()` before assertions. Supply portable fixtures or select this test only through an explicit external-evidence lane.
- The new `_bmad/scripts/tests/test_epic16_tooling.py` suite is absent from default pytest selection (`pyproject.toml:13`), the npm test list, and CI selection. Its regression protection currently depends on manually running an additional command; connect it to a maintained validation lane.
- `_bmad/scripts/epic16_native.py:94` discards build stderr, and the preceding SDK-selection failure discards both diagnostic streams. Failures reported exclusively on stderr become an empty “fixed helper build failed” message. Preserve bounded stdout/stderr and exit-code details in prerequisite diagnostics.
- `_bmad/schemas/epic-16-tooling-v1.schema.json` expands shared shapes repeatedly across 32,638 lines—for example, twelve identical 5,439-byte snapshot schemas. Centralize these definitions or provide a deterministic generator for the expanded schema; otherwise one protocol correction requires synchronized edits across many indistinguishable copies.

## Edge case hunter — raw final report

```json
[{"location":"_bmad/scripts/epic16_lifecycle.py:170-202","trigger_condition":"An enqueue reuses an entry, work, or completion transaction ID.","guard_snippet":"transactions = {stage['cas']['transactionId'] for stage in row['atomicity']['stages']}","potential_consequence":"Contradictory durable transaction identities pass AC-16.2-04 schema and semantic validation."}]
```

## Verification gap reviewer — raw final report

No verification gaps found.

## Reviewer probe qualification

The blind reviewer additionally reported that the custom Task-returning fixture
also passes the actual xUnit runner. Its claim is type ownership, not failed
discovery. The ParamArray fixture instead produces a genuine runner failure.
The edge review's guard snippet is a suggested correction; current code initializes
the enqueue transaction inventory as an empty set.
