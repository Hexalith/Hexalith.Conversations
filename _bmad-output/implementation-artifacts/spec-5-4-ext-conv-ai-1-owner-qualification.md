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

### Delivery continuation — 2026-10-25

The previously reviewed component implementation is now committed in canonical source candidate `ed53473ea4c67aa10ffc46c9b951d9864fe263d4`. Before this documentation update, all 31 prior inventory paths matched its committed bytes; 29 matched the historical review snapshot, with only the CI workflow and unrelated deferred-work ledger changed. No further runtime source change is justified without the independently verified inputs required by the unchecked installation task. Pre-existing Builds working-tree changes were preserved.

Fresh five-project Local verification passed 2,247 tests, including 31 receiver cases, with zero failures/skips; its repeated 37-case matrix is included in the total. The helper and synthetic actual-runner suites passed 45 cases. The receiver's fresh 52-library restore graph contains no Agents library. Exact commands, native receipts, source hashes and a refreshed blocked packet are retained at `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/delivery-20261025-ed53473/`. The historical 2026-10-22 evidence remains unchanged.

The Live manifest and installed authority, enrollment, host transport, receiver and hash inputs remain unverified; the committed dependency register still pins the prior Conversations target without its proposed `RuntimeQualificationBinding`, and protection remains Uncommitted. The packet records the full source candidate while retaining null installed fields. A complete replacement `Committed` acceptance must pin the target, command and hashes before Live execution; successful persisted C1–C4 evidence is required for `Available`, not for proposing or accepting `Committed`. No Live/refusal command or real cohort reservation occurred. The installation and qualification tasks remain unchecked and this spec remains in progress.

## Spec Change Log

- 2026-10-25: Recorded the committed source checkpoint, fresh component verification and remaining installed-input blockers; clarified acceptance-before-qualification ordering without changing the frozen scope.
- 2026-10-29: Rechecked current committed dependency gates, repaired Local verification to inherit the producer mapping, and retained fresh component and refusal evidence. Installation and persisted C1–C4 qualification remain incomplete.

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

### Delivery continuation — 2026-10-29

Both frontmatter context files were loaded before this continuation. The current
committed Agents register retains the older accepted Conversations target and
omits its proposed `RuntimeQualificationBinding`; `EXT-PROTECTION-1` remains
Uncommitted. Its 2026-10-28 custody amendment preserves these gates. The
installed Live manifest remains absent and the retained attempt journal has no
reservation. No independently verified replacement installation inputs were
supplied.

EventStore now has `SourcePublicationAuthority` and SDK registration for the
namespace, operation and complete-command authority interfaces. This corrects
the earlier source-absence observation without changing historical evidence.
Its independent current installation-evidence port, role enrollment, workload
credentials and deployed bindings remain required. The router still rejects
caller-supplied command-proof DTO fields. Source code is not installed current
authority.

The original Local command failed before tests because its sibling-root and
version selection bypassed the current producer mapping requirement in
`Directory.Build.props`. The repaired runner inherits the complete producer
environment, validates source/Debug selection and exact mapping hash before
building, records that hash, and no longer selects dependency roots, versions
or reference mode. Preparation and invocation commands are recorded in
`docs/implementation/ext-conv-ai-1-runtime-qualification.md` and the detached
command receipts. The existing Debug source producer emits retained release-tag
and version-range warnings; that use establishes no accepted published-tool or
runtime qualification.

Fresh five-project Local verification passed 2,279 tests, including 31 receiver
cases, with zero failures/skips. Its separate 37-case matrix reruns cases
already included in the Server suite. The receiver source restore graph has
52 libraries and no Agents library; it proves no installed receiver identity.
The tooling suite passed 52 cases: the existing helper/Live-runner and retained
packet checks plus four Local-runner mapping tests. Those new tests exercise
all seven missing environment values, invalid input files/hash/configuration,
evaluated selection mismatches before builds, and unchanged environment
inheritance through the complete five-project path. They use synthetic process
boundaries; the native Local receipts provide separate real build/test evidence.
The isolated AppHost baseline failed before runtime on EventStore
`CopyRefAssembly` file-copy errors, and `aspire describe` found no running
AppHost for that project. The accepted exact owner command exited 1 at its
immutable-target comparison, before the Live script.

Fresh evidence and the blocked replacement packet are retained at
`/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/delivery-20261029-acd8b0b/`.
The packet keeps missing installed bindings, frozen hashes, reviewed clean
successor and replacement acceptance null. No enrollment, manifest,
installation, acceptance or protected deletion receipt was manufactured. No
cohort attempt was reserved or executed. The two unchecked tasks and this
spec's in-progress status remain unchanged; Story 8.3 and Level 5 remain gated.

### Deployment-configuration check — 2026-10-29

A read-only inventory of the only configured deployment target
(`~/.kube/hexalith-production`) found no Conversations, Parties, conformance
receiver or Agents workload; only the `ext-secrets-1-isolated` stack exists.
The canonical Platform owner form records `installationAuthorized: false`
(offline preparation only) and an uninstalled independent decision issuer.
Source still has no `ISourcePublicationAuthorityInstallationEvidence`
implementation and no host `IConversationDeletionReceiver` adapter.
`EXT-PROTECTION-1` remains `Uncommitted` (latest audit `BlockedNotSubmitted`).
None of the five OQ prerequisites could be supplied without inventing values.
Nothing was installed, and no manifest, attempt or acceptance was created. The
prerequisite bundle and ordered owner actions are at
`/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/owner-prerequisites-20261029-180858/`.

Source delivery (owner choice "Source delivery first") is committed as
`ebc65f6da42d8c165ed70f94d2fe5231a6402830` under
`spec-5-4-ext-conv-ai-1-host-composition.md`. It adds the opt-in host composition,
complete-source verifier, conformance receiver adapter, runtime identity endpoint
and container settings. An enabled host still refuses to start without the
independent authority, approval and publication providers. Nothing was installed.
Both unchecked tasks remain open.


### Installation prerequisite continuation — 2026-10-30

Both frontmatter context files were fully loaded before this continuation. The
Platform owner form already contains `extConvAi1InstallationAuthorization` for
additive isolated Conversations, Parties and target/rollover conformance receiver
installation on `https://192.168.1.30:6443`, including workload enrollment and
Party provisioning. Its broader top-level `installationAuthorized: false`
applies to the coupled lifecycle scope and is not a request to reapprove this
narrow installation. Independent approvals, register changes, replacement target
acceptance and C1–C4 remain outside that installation authorization.

Fresh read-only metadata from that configured cluster found 24 namespaces and
57 workloads, with no named Conversations, Parties, conformance receiver or
Agents workload. This is a scoped cluster observation, not a universal absence
claim. The canonical source candidate is `ebc65f6da42d8c165ed70f94d2fe5231a6402830`.
Its opt-in host, complete-source verifier, conformance receiver adapter,
authenticated identity endpoint and container settings supersede the historical
source-absence observations. Source availability establishes no installation.

The inspected production source still supplies only Unavailable agent-authority
and approval-verification defaults; no concrete independent source-publication
installation-evidence provider or acknowledgement observer was found in the
searched Conversations/EventStore/Platform source roots. The executable gateway
does not compose publication authority. The embedded bound-assertion issuer is
explicitly a nonconforming Development fixture, whereas the conformance receiver
requires separately enrolled Production-capable SDK trust and exact bound
principals. Authentic tenant/AgentId/Branch-B Party, current worker, independent
approval issuer/public trust, source inventory/journal/epoch enrollment and
receiver/storage bindings remain unsupplied. An enabled host refuses before
listening without mandatory providers; deploying disabled/nonstarting placeholders
cannot close these tasks. No installation or enrollment was invoked.

The committed Agents register still pins the prior Conversations target, omits
its proposed `RuntimeQualificationBinding`, and leaves `EXT-PROTECTION-1`
Uncommitted. The exact accepted command was captured verbatim and exited 1 at
its immutable-target comparison before invoking Live. The manifest remains
absent, the retained attempt journal is empty, and no C1–C4 attempt was reserved
or executed. No runtime source changed, so the passing component suites were
not repeated.

Fresh receipts, committed/working register snapshots, narrow authorization,
preexisting changed-file snapshots, source hashes, all five prerequisite
statuses, exact ordered owner actions and a blocked NotSubmitted replacement
packet are retained at `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/owner-prerequisites-20261030-060525/`.
The proposed command pins the observed full canonical source candidate but is
neither accepted nor installed; missing installed hashes, successor acceptance
and integration date remain null. The bundle's `SHA256SUMS` is verified by
`sha256sum --check SHA256SUMS`; its native receipt is
`checksum-verification.json`. Historical evidence and preexisting edits are
preserved. Both installation and persisted C1–C4 tasks remain unchecked, delivery
remains in progress, and Story 8.3/Level 5/availability remain gated.

The kubeconfig supplies deployment-target/access configuration and the owner
form supplies narrow authorization; neither supplies installed application
configuration. No authoritative installed Conversations/Parties/receiver
application deployment configuration was located in the inspected Platform
`deploy/src/eng` or Conversations `src/tools` JSON/YAML/environment files, or at
the known Live-manifest path. `application-deployment-input-map.json` in the
bundle records the scoped no-match search, exact configuration keys and
owner-return JSON pointers separately from missing compiled production
providers; no placeholder application configuration was manufactured.
