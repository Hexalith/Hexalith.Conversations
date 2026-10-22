---
title: '5.4 Complete EXT-CONV-AI-1 owner qualification'
type: 'feature'
created: '2026-10-22'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '71fab44b7dd664699cedbbfc17ed0d7822e8bdb9'
context:
  - '/home/administrator/projects/hexalith/conversations/docs/runbooks/current-change-validation.md'
  - '/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/external-dependency-register.md'
---

<frozen-after-approval reason="explicit user instruction to complete delivery on 2026-10-22">

## Intent

Complete the six-seam EXT-CONV-AI-1 delivery for Agents Story 5.4, including the installed Live manifest, dedicated Conversations service Party, current authenticated machine binding, source authority, and persisted C1–C4 qualification. Install the separately authenticated contract-conformance deletion receiver and independent receipt lookup permitted by OQ-5.4-1. Return reviewed changes, qualification evidence and a replacement immutable-target acceptance packet.

## Boundaries & Constraints

**Always:** Follow the complete six-seam contract and OQ-5.4-1. Domain persistence uses EventStore aggregates/events and SDK transports. Verify Branch B identity by immutable tenant/Party id. Preserve source-atomic approval, stable logical signal/source revision, independent receipt lookup, ordered checkpoints, authenticated exact targets, no-skip quarantine, typed denial and current authority. The receiver has separate authentication and no Agents package or runtime path; its evidence is Level 4 only.

**Never:** Enable Agents Story 8.3 or claim Level 5/Available from component evidence. Manufacture enrollment, authority, credentials, protected deletion evidence or target acceptance. Execute a cohort before OQ prerequisites are met. A missing prerequisite leaves delivery in progress with exact refusal evidence; it is not an implementation waiver.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Receipt acceptance | Authenticated exact source, complete stable signal and target | EventStore persists one conformance acceptance; independent lookup proves its positive receipt revision | Gateway admission alone remains pending/unavailable |
| Retry and restart | Same signal, new attempt, receipt stream replay | Same original receipt and stable source binding | Same id with changed fields conflicts; no checkpoint advance |
| Isolation | Foreign tenant, wrong operation, missing authentication, production Agents receiver | No acceptance or information disclosure | Typed denial/unavailable; real Agents path stays gated |
| Qualification | Exact accepted clean commits/configuration and all OQ prerequisites | All C1–C4 persisted lanes recorded once | Missing protection commitment, bindings or anchors refuses before effects |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Conversations.Server/Agents/` provides `IConversationDeletionReceiver`, stable delivery pump, receipt verifier and opt-in runtime registration. Reuse these unchanged unless necessary.
- `eng/verify-ext-conv-ai-1-live.ps1` implements C1–C4 and committed owner-target checks. It lacks explicit OQ prerequisite enforcement and receiver-kind isolation.
- `tools/Hexalith.Conversations.DeletionConformance/` will own the separate qualification-only domain service, using SDK hosting/authentication, ordinary receipt aggregate/state/events, and independent complete-source reads. No custom database, Dapr plumbing, bearer-token verifier or fake production source authority.
- `tests/Hexalith.Conversations.DeletionConformance.Tests/` will prove receipt folding, exact retry/conflict, tenant/target isolation and non-acceptance on unconfirmed persistence.
- `../eventstore/src/Hexalith.EventStore.DomainService/DomainServiceRequestRouter.cs` rejects `CommandStateProof`/`VerifiedEffectiveEvents`. DTO fields are not authenticated command-source proof. Do not turn those fields into authority.
- `../eventstore/src/Hexalith.EventStore.Server/Streams/` contains namespace/index actors but no installed `ISourcePublicationNamespaceAuthority` or `ISourcePublicationOperationAuthority`. Technical owners must supply the missing current providers.
- `docs/implementation/ext-conv-ai-1-runtime-qualification.md` and the original in-progress runtime-delivery spec retain previous work. Correct obsolete receiver/cohort/target statements without rewriting historical evidence.

## Tasks & Acceptance

- [x] Add the separate SDK-hosted EventStore-backed conformance receiver and exact receipt lookup, with focused tests. Supply configuration requirements that fail closed without separately enrolled workload identity; do not claim installation from a source build.
- [ ] Bind and install the actual Party, current worker, approval issuer, complete source/catalogue/publication authority and receiver configuration once independently verified inputs exist.
- [x] Enforce OQ prerequisites before Live operations and pin receiver qualification scope independently of caller expectations; preserve existing C1–C4 assertions and refusal evidence.
- [x] Run focused Local and receiver checks, review the change, and retain commands/results. Record externally blocked qualification separately.
- [ ] Run exactly one approved C1–C4 qualification on the accepted installed target set; prepare the complete replacement packet with full canonical commit, exact command, manifest/configuration/plan/anchor hashes and evidence. Do not invent missing fields or acceptance.

**Acceptance Criteria:**

- Given the installed separate receiver, when delivery/lookup/retry occurs, then only confirmed persisted conformance receipts can acknowledge the source and changed same-id fields conflict.
- Given current accepted installed bindings, when the exact owner command runs, then all C1–C4 lanes pass with persisted evidence.
- Given unmet OQ gates, when qualification is attempted, then no consuming Agents path or cohort effect occurs and no completion/availability claim is made.

## Implementation Notes

Initial discovery: Conversations is clean at the baseline. The accepted target is `87e8b4a6ec3224b20a560b0e83de857805925572`. The installed manifest directory and application credential references are absent. EXT-PROTECTION-1 remains Uncommitted. Other repositories have unrelated changes and must be preserved. The user has been asked for newer commitment and application-binding records while independent source work proceeds.

The user answered that newer commitment/binding paths are unknown. Source work therefore proceeds without creating application identities or inferring installed authority. The separate receipt domain uses SDK workload authentication, native source reads and ordinary event persistence. Its 28 component cases pass; a missing-config process refuses before listening. Its Debug publish succeeds, but no deployment, enrollment, host receiver adapter or Live qualification is established.

The Live runner now proposes a committed `RuntimeQualificationBinding` row for immutable manifest/configuration/plan/profile/anchor hashes, exact POST body inventory, a Level 4 conformance identity query, SDK payload decoding, sidecar workload transport, credential separation and an exclusive retained attempt reservation. It captures consumed manifest/body bytes and disables HTTP redirects. The changed verifier and additional row require replacement acceptance; the old commitment is not silently upgraded.

The original four-project Local command passed 2,001 tests and its repeated 37-case six-seam matrix. The accepted owner command exited 1 at its target comparison without executing a cohort. The isolated Platform startup failed before runtime with `NU1605` downgrades from EventStore.Aspire/ServiceDefaults 3.126.1 to 3.126.0. Detached evidence is retained at `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/delivery-20261022-71fab44/`.

## Spec Change Log

## Review Triage Log

Component review on 2026-10-22 covers the current source diff, not completion of the externally blocked delivery. Historical evidence-boundary scopes are not applicable under the current-change runbook.

| Finding | Verdict / route | Evidence and disposition |
| --- | --- | --- |
| Blind 1: missing host delivery transport | high / pre-existing incomplete task | No production `IConversationDeletionReceiver` adapter exists. The component deliberately supplies only the receiving domain/query protocol; actual host composition, current authority and enrollment remain unchecked delivery tasks and block qualification. |
| Blind 2: movable attempt journal | high / patch | Identical accepted bytes in another directory can reserve the same fingerprint again. Pin an absolute journal directory inside the owner-accepted configuration. |
| Blind 3: POST casing | medium / patch | Helper inventories case-sensitive POST while the runner accepted case variants. Reject noncanonical methods before body inventory and execution. |
| Blind 4: symlink ancestors | medium / patch | File traversal stopped at the manifest directory and missed a linked ancestor. Check the full ancestor chain. |
| Blind 5: bare receiver response | high / patch | Legacy direct-response support also admitted receiver dictionaries without SDK `QueryResult`. Require the SDK envelope/encoded JSON specifically for receiver queries. |
| Blind 6: rollover identity | high / patch | C3 retried before proving replacement receiver scope. Pin the rollover target, authenticate its fixed conformance identity and current source binding before retry. |
| Blind 7: static short-lived assertion | medium / patch | Static environment assertions cannot refresh during a long run. Support rereading an atomically published assertion file from the independently enrolled SDK credential provider; no local minting or JWT verifier. |
| Blind 8: partial Development startup | high / patch | SDK intentionally warns in Development and can listen with incomplete trust. Require non-Development for this qualification host and inherit the SDK's strict startup validation. |
| Blind 9: routine C# execution missing | medium / patch | The explicit CI and Local project lists omit the new receiver suite. Enroll it in both lists. |
| Blind 10: interruption evidence | medium / patch | Request observations are held in memory until lane completion. Persist a pending record before each request and its observation immediately afterward using atomic flushed writes. |
| Edge 1: POST casing | medium / patch | Independently reproduced the same case-sensitive inventory / insensitive execution defect. Same fix as Blind 3. |
| Edge 2: journal relocation | high / patch | Independently reserved an identical fingerprint in two input directories. Same fix as Blind 2. |
| Verification 1: routine receiver tests | medium / patch | Reviewer traced the explicit shared CI project list and Local verifier. Same enrollment fix as Blind 9. |
| Verification 2: runner ordering untested | medium / patch | Existing Python tests call helpers directly; moving guards after a request would evade them. Execute the actual runner with synthetic Git/HTTP boundaries and prove missing prerequisites and repeated attempts make zero requests. |
| Verification 3: retained-target positive case missing | medium / patch | Existing retry test keeps one current target; removing historical target allowance would pass. Add an old-target receipt lookup after current-target rollover. |
| Package API inspection: native reader constructor | high / patch | The pinned 3.126.1 NuGet API has no source-only `(gateway, clock, bool)` overload. Use its four-argument native-proof constructor with explicit null timeout; retain native proof requirements. |
| Runner execution: timestamp parsing | high / patch | PowerShell's default JSON date conversion made valid manifest UTC bounds fail text validation. Keep JSON timestamps as strings with PowerShell 7.5 `DateKind String`; the actual-runner positive case now reaches all lanes. |
| Edge follow-up: equivalent UTC representations | medium / patch | Literal comparisons rejected equivalent `Z` and `+00:00` bounds after effects. Validate increasing explicit UTC input before reservation and compare parsed zero-offset response bounds. Positive, invalid-input zero-effects and changed-response-window cases pass. |

All source patches above are applied. Verification-gap follow-up confirmed CI/Local enrollment, actual-runner guards and retained-target lookup. Edge follow-up confirmed method, journal and UTC-window fixes with no unresolved defect in those fixes. The pre-existing absent host transport remains required delivery work; it is not waived or counted as installed.

Final focused verification: the five-project Local command passed 2,032 tests (including 31 receiver cases), with zero failures/skips. The repeated 37-case six-seam matrix is a subset, not an additional test count. Helper tests passed 34 cases and five subtests; actual-runner synthetic Git/HTTP tests passed 11 cases, including interruption evidence and zero-request refusals. These are local component/control-flow evidence only. All failed development runs are retained alongside passing reruns. No Live cohort attempt was reserved or executed.

The shared repository recorded earlier component work in `6655e38743e57ffb85278278dde216663150bd01` during the run. Later review fixes remain uncommitted; this observed commit is not the complete reviewed immutable target. The committed Agents register is unchanged and still pins the previous accepted target. The detached blocked replacement packet records null installed hashes and target rather than inventing acceptance. This spec remains in progress because installation, current authority, enrollment and C1–C4 persisted qualification are incomplete.

## Verification

Use individual Debug test-project builds with project references and xUnit v3 assembly execution. Run `pwsh -NoProfile -File eng/verify-ext-conv-ai-1.ps1 -Mode Local -ArtifactsPath /tmp/ext-conv-ai-1-local-20261022`. Live invocation may record refusal only until prerequisites and exact successor acceptance exist. Evidence-boundary historical scopes are not applicable to this new owner-delivery work under the current-change runbook.
