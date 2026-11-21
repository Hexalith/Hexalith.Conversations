# Conversations architecture validation

21 November 2026 · Pilot architecture and inherited decisions

## Verdict

**Changes needed before teams use the spine as their sole implementation handoff.** The November update accurately records the pilot’s open work. Six high-priority findings still allow materially different outcomes across the architecture, its companion documents and the implementation. The most consequential are how long an existing access grant can survive a stalled revocation and whether the same event history can be accepted by the domain and rejected by its projection.

## High-priority findings

### 1. Tenant access can outlive a delayed revocation

The spine says lagging tenant state denies access and AD-11 relies on that guarantee. The Story 16.1 owner decision defines stale as a missing or unproven record. A previously healthy checkpoint has no time-based expiry. When a revocation has yet to be applied, the prior grant can continue to authorize. The retrospective records this exact limitation.

**Closure.** Conversations and Platform owners should state what a healthy checkpoint proves, who observes delivery lag, and what request behavior applies during a lag window. Bind the handoff and a delayed-revocation acceptance case to the same decision. A local recovery test cannot establish production delivery freshness. [Spine, lines 838 and 3353](../../architecture.md) · [16.1 decision 3, line 33](../../../implementation-artifacts/spec-16-1-persist-tenant-access-projection-state-and-prove-convergence.md) · [Checkpoint health, line 54](../../../../src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantSnapshotHealth.cs) · [Retrospective F-6](../../../implementation-artifacts/epic-16-retro-2026-11-16.md) · [Tracker item 54](../../../implementation-artifacts/sprint-status.yaml)

### 2. Domain replay and projection replay disagree on duplicate identity

For participant-added and project-changed events, the domain verifier accepts an EventId repeated at a later position as a no-op. AD-8 includes sequence in the identity binding, and the projection treats the changed binding as contradictory. The same supplied history can therefore pass domain replay and fail projection. Production reachability of that history remains unverified.

**Closure.** Domain and Projection owners should settle tracker item 52, including the namespaces for EventId and MessageId and the visible result if a prior generation already exists. Feed the same history to both paths and assert the agreed state and read result. [AD-8, line 3051](../../architecture.md) · [Domain verifier, line 89](../../../../src/Hexalith.Conversations/Replay/ConversationReplayVerifier.cs) · [Projection admission, line 928](../../../../src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs) · [ADR 0005, line 76](../../../../docs/adrs/0005-conversation-derived-key-lifecycle.md) · [Tracker item 52](../../../implementation-artifacts/sprint-status.yaml)

### 3. Current planning can resolve to three different entry points

The canonical SPEC still directs readers to V15. The spine’s inherited AD-3 directs marker-based readers to V22. The November acceptance decisions sit after V22, and the current-change runbook archives the earlier execution machinery. A consumer following the SPEC can block completed Epic 16 work or miss AD-11 and AD-12.

**Closure.** Publish a current planning entry point outside the frozen bytes and reference it from the SPEC. It should identify the November decisions and current-change runbook while keeping the archived hold and its recorded verdict intact. Check that a reader starting at either file reaches the same authority. [Canonical SPEC, lines 24–35](../../../specs/spec-Conversations/SPEC.md) · [AD-3, line 2818, and November status, line 3343](../../architecture.md) · [Historical boundary](../../../../docs/runbooks/current-change-validation.md) · [Epic 16 delivery, lines 143–149](../../../implementation-artifacts/sprint-status.yaml)

### 4. Two paths can publish a false Current-empty result

Story 16.4 covers event redelivery after a lost index. A delayed first delivery also matters. A receipt can prove the initializer has never seen TenantCreated even when the tenant already has a conversation. During rollback, the handoff says an older reader treats a missing index as Current and empty, and an older writer can rewrite a v2 index as v1. Both cases break the same promise to the reader.

**Closure.** Require proof that creation precedes all applicable conversation history through the publication cutoff, or equivalent causal ordering. Until that proof exists, preserve the degraded result. Platform and Projection should also define traffic and writer fencing for a rollback, or demonstrate equivalent containment. Test delayed first delivery and a missing index through rollback and roll-forward, including version overlap where supported. [AD-7 clarification, line 3361](../../architecture.md) · [Initializer limitation, lines 205–208](../../../implementation-artifacts/spec-16-2-make-replay-time-deterministic-and-missing-index-state-truthful.md) · [Rollback proposal, line 155](../../../../docs/adrs/0005-conversation-derived-key-lifecycle.md) · [Rollback behavior, lines 597–602](../../../../module/platform-handoff.md) · [Story 16.4 acceptance, lines 4524–4526](../../prds/prd-Conversations-2026-06-02/epics.md)

### 5. HTTP 503 carries two valid contracts

AD-11 requires one read of a forward-only body and preservation of typed errors. A 503 response can also contain a valid unavailable list or detail result. The existing client test preserves that result, its freshness and its HTTP status. A client that maps all 503 responses directly to a service error loses the read contract.

**Closure.** Specify how to recognize a valid operation-specific read and a service-error envelope after one body read. Preserve code, category and correlation for the error and the typed freshness for the read. Exercise both 503 bodies on a forward-only stream and verify the fallback for empty or malformed bodies. [AD-11, line 3353](../../architecture.md) · [HTTP mapping, lines 279–304](../../../../src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs) · [Client tests, lines 203–233](../../../../tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs) · [Story 9.4, line 4532](../../prds/prd-Conversations-2026-06-02/epics.md)

### 6. The Platform revision determines who provisions Dapr Components

At the working Platform checkout abb62da8 the environment owns Dapr Components. At d80bdc94, the gitlink recorded by Conversations 08953f5e, the application chart owns them. The November statement accurately names the newer revision. A deployment following the clean root and a deployment following the updated checkout will nevertheless provision different resources.

**Closure.** Platform and Release owners should bind host qualification to an exact root and Platform gitlink, record the single writer for that tuple, and exercise the ownership handoff before promoting the newer configuration. [November ownership, line 3347](../../architecture.md) · [Refreshed Platform deployment contract, line 45](../../../../references/Hexalith.Platform/eng/helm-qualification/README.md) · [Root-pinned Platform contract, line 45](https://github.com/Hexalith/Hexalith.Platform/blob/d80bdc94baffe1fa398337fbb463a11c60e2c715/eng/helm-qualification/README.md)

## Other findings

- **Hidden read responses.** Specify that header mismatch and multiple values return the same denial within each endpoint, before query-store access. The existing hidden detail returns 404 and the hidden list returns 200. A uniform status across both would change wire behavior. [AD-11, line 3353](../../architecture.md) · [Hidden detail and list, lines 346 and 384](../../../../src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs)
- **Overlapping freshness conditions.** The AD-7 table includes mixed-generation records under Unavailable, while in-flight partial progress can return Rebuilding/mixed_generation. A valid Rebuilding index may also show a watermark gap. Give public state, reason, data visibility and precedence for each condition. The current reader gives explicit Rebuilding precedence over a gap on that index. [AD-7, lines 3031–3043](../../architecture.md) · [Read-store classification, lines 312–341](../../../../src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs) · [Query mapping, lines 422–455](../../../../src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs)
- **Frozen inventory and formal approval.** The recorded 13,289-line denominator can be used for measurement. The PRD still requires named ratification of the exact FR-1 inventory hash. Qualify the inherited “accepted” label so readers can distinguish the frozen value from approval of the inventory. [Spine, line 327](../../architecture.md) · [FR-1, lines 133–145](../../prds/prd-Conversations-2026-06-02/prd.md)
- **Replay serialization boundary.** AD-2 calls for public platform seams. The module currently reaches EventStore’s internal canonicalizer through friend-assembly access. The replay hash input labels its own format version 1 even though the index lifecycle schema is version 2. Document these version domains and bind the public replacement to cross-module golden vectors under tracker item 56. [AD-2 and AD-8](../../architecture.md) · [Replay hash input, lines 33 and 68](../../../../src/Hexalith.Conversations.Server/Projections/ConversationReplayState.cs) · [Canonicalization call](../../../../src/Hexalith.Conversations.Server/Projections/ConversationReplayStateCanonicalizer.cs) · [Tracker item 56](../../../implementation-artifacts/sprint-status.yaml)
- **Erase and generation fencing.** Story 16.2 explicitly defers tenant erase, quarantine and generation fencing until a producer exists. Story 16.4 covers tenant-wide recovery. Carry the separate erase deferral and its activation condition into the current handoff so a recovery implementation cannot imply that deletion is qualified. [AD-7](../../architecture.md) · [16.2 decision 3, line 24](../../../implementation-artifacts/spec-16-2-make-replay-time-deterministic-and-missing-index-state-truthful.md) · [16.4 acceptance](../../prds/prd-Conversations-2026-06-02/epics.md)
- **CI instructions.** The current-change runbook is the correct source for retiring the historical V23–V29 gates. Its routine steps still describe a repository CI job that verifies story tooling. The active workflow delegates to Builds v6.4.4, and the repository instructions require the shared checker before push. Correct the operational instructions and preserve the historical disposition. [Runbook, lines 9–25](../../../../docs/runbooks/current-change-validation.md) · [Repository policy, lines 83–95](../../../../AGENTS.md) · [Workflow, line 14](../../../../.github/workflows/ci.yml) · [Product hook](../../../../.github/actions/after-unit-tests/action.yml)
- **Public timestamps and replay metadata.** When generation precedes the true anchor, the implementation preserves the internal anchor and publishes an epoch sentinel for the last-applied timestamp with unavailable freshness. Record that exception and disposition the legacy projection response before treating replay metadata as private on every route. [AD-8, line 3067](../../architecture.md) · [Story 16.2 implementation note, line 152](../../../implementation-artifacts/spec-16-2-make-replay-time-deterministic-and-missing-index-state-truthful.md) · [Freshness mapping, line 250](../../../../src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs) · [Tracker item 53](../../../implementation-artifacts/sprint-status.yaml)

- **Links from the run-folder alias.** The twenty-eight local November links resolve from the canonical architecture.md. A renderer that keeps the directory of the ARCHITECTURE-SPINE.md symlink instead resolves zero of those targets. Publish or link the canonical path for readers and tools. The two commit-pinned upstream links have identical target bytes in this checkout. [Canonical architecture](../../architecture.md)

## Confirmed evidence

| Check | Result |
| --- | --- |
| BMAD spine lint | Passed with zero findings. Decisions AD-1 through AD-12 occur once in order. |
| Historical integrity | All 217,070 bytes of the committed predecessor match. Nine historical overlay blocks are paired. The frozen V8 prefix, eight predecessor pins, and three distinct referenced sidecar digests checked out. |
| November evidence links | All 30 resolved using the canonical spine directory. The two upstream source links matched the local checkout bytes at their cited Git objects. |
| Direct submodules | The repository’s root declaration/index checker passed. Eleven working-tree gitlinks differ from the reviewed root commit. Builds is at its pinned release v6.4.4. The November upstream observations use the refreshed working checkouts. |
| Minimal-module qualification | Ten of the 17 frozen fixture files differ, including all four original locks. A fresh MSBuild evaluation exited 1 at RegisteredCommand.targets line 28. The overlay selects EventStore 5.3.1 and the checked-out Builds catalog selects 5.4.0. This confirms the open Story 11.4 gate. |

## Decisions already assigned

The deployed owner and address for ordinary client routes (9.3), supported package ranges including 2.1.0/wire v1 (9.5), authoritative tenant-wide enumeration and cutover (16.4), and production exporter verification (16.5) have owners and explicit proof obligations. They remain open. The fixture qualification work (11.4) and per-run browser outputs (8.3) likewise have acceptance criteria. [Sprint supplement](../../prds/prd-Conversations-2026-06-02/epics.md) · [Current stories](../../../implementation-artifacts/sprint-status.yaml)

The archived hold remains ACTIVE. Current pilot acceptance still requires the exact preservation approval and effective cross-repository grants. Under the universal four-path rule the recorded append, list and open P95s exceed the five-percent limit. This validation did not rerun the benchmark, a hosted client journey, a production rollback or the application test suite. [PRD readiness and metrics](../../prds/prd-Conversations-2026-06-02/prd.md) · [SM-C2 method](../../../../docs/release-evidence/sm-c2-measurement-method-v2.md)

## Review record

Standalone BMAD validation on 21 November 2026. The three reviewers examined the whole effective architecture, including inherited decisions and their current owners. The previous update review covered the November amendment. This validation also tested the guidance a reader reaches from the older spec, ADRs and runbooks. The six high-priority findings above combine overlapping reviewer reports by the decision needed. Supplemental findings describe narrower wording, evidence or navigation gaps.

[Good-spine rubric](reviews/validate-2026-11-21/review-rubric.md) · [Runtime and dependency reality check](reviews/validate-2026-11-21/review-reality-checked.md) · [Adversarial review](reviews/validate-2026-11-21/review-adversarial.md)

| Reviewed identity | Value |
| --- | --- |
| Conversations root | `08953f5eed404228a2b8bd41c7d2bf5eaaab543a` |
| Working-tree spine | `6279e0dee85caf6d2a2752d7cc09c1c719328ae1d53a86dd2288a14cc1d88888` |
| Scope | 228,743 bytes, 3,377 lines |
| Entry point | [Canonical architecture](../../architecture.md) |
