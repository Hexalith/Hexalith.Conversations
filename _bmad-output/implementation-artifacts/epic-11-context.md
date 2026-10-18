# Epic 11 Context: Thin-Module Authoring Proof

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Module authors get a corrected, platform-owned thin-module authoring recipe, a reproducible minimal fixture built against live public platform APIs, and decision-grade SM-2 (new-module authoring cost) evidence that replaces the earlier estimate-qualified, low-confidence historical claim. The epic ends with a reproducible decision on OQ-2 / SM-2 attainment. It supersedes the old Story 6.5. Hard entries: Story 6.2 and Epics 7 and 10 (concretely Stories 7.4 and 10.4). Exit: Stories 11.1-11.3 done on one compatible candidate chain, with Checkpoints A-C independently evidenced.

## Stories

- Story 11.1: Correct and validate platform-hosted thin-module authoring guidance (Checkpoint A)
- Story 11.2: Build the reproducible minimal-module fixture against live platform APIs (Checkpoint B)
- Story 11.3: Generate authoritative SM-2 v2 evidence and decide OQ-2 (Checkpoint C)

## Requirements & Constraints

- Module-owned: contracts, domain behavior, module registration, module tests. Platform-owned: ServiceDefaults, Dapr implementation/wiring, event subscription, projection/query runtime, publication, health, telemetry. A module must never reimplement or reassign any platform-owned capability.
- A module may contain exactly one repository-only test AppHost (non-packable, non-publishable) and it counts toward authoring cost. A reusable module Aspire library/ServiceDefaults project is prohibited.
- The fixture must restore (locked mode), build warnings-as-errors clean, and run non-vacuous tests (zero failed/skipped/not-run) using only public platform APIs; internal/private API use is a failure. Tests must prove registration, command handling, event production, and platform-hosted subscription/projection/query/publication.
- SM-2: at least 50.00% fewer hand-authored module-owned files than the preserved reference, inclusive; file count decides, LOC is supporting only. All 13 frozen fixture paths (including the test AppHost) are counted when hand-authored; generated/build outputs are excluded. Confidence is `high` only if every path and line is reproducibly counted. `oq2Decision` is `accepted` only at >=50.00% file reduction, otherwise `rejected` (result never hidden). The artifact should also state which single-file decisions would flip attainment (prior figure sat exactly at 50.00%).
- Immutable history: SM-1 compatibility surface stays exactly `13,289`; the v1 SM-2 baseline JSON/Markdown (`minimal-module-authoring-cost-sm2-baseline-v1.*`) must remain byte-identical. Counts must be computed from sources, never trusted from declarations.
- Evidence is generated, deterministic (JSON + Markdown parity), schema-validated, and bound to the story contract, predecessor final-record digests, and the canonical candidate rule. Reviewer decision (approved, named, UTC instant, bound to source/evidence digests) is required for guidance.
- Every story has a mandatory fault-injection matrix: each mutation yields its exact blocker code and is restored byte-identically; undetected/unrestored mutation fails.

## Technical Decisions

- Guidance source: `docs/domain-module-authoring-template.md`. Generated evidence lives under `docs/release-evidence/`: `thin-authoring-guidance-v2.*` (schema id `hexalith.conversations.thin-authoring-guidance.v2`), `thin-module-fixture-proof-v1.*` (`...thin-module-fixture-proof.v1`), `minimal-module-authoring-cost-sm2-v2.*` (`...minimal-module-authoring-cost-sm2.v2`), plus `story-11.N-final-record-v2.{json,md}`.
- Fixture root: `tests/fixtures/Hexalith.Conversations.MinimalModule/`, a frozen 13-path inventory (hash-pinned): Directory.Build.props, slnx, README, Contracts (csproj, CreateConversation, ConversationCreated), Server (csproj, ConversationAggregate, ConversationModule), Tests (csproj, MinimalModuleFixtureTests), AppHost (csproj, AppHost.cs). Paths must be source-controlled, hand-authored, LF-normalized.
- Tooling is Python scripts in `_bmad/scripts/` (generate_thin_authoring_guidance, verify_thin_authoring_guidance, verify_minimal_module_fixture, generate_minimal_module_fixture_proof, generate_sm2_evidence, verify_sm2_evidence, verify_story_candidate, generate_story_record) with pytest fault matrices in `_bmad/scripts/tests/`. Story contracts are `_bmad-output/planning-artifacts/v9/story-contracts/11.N.json` bound via `v9-authority-bundle-v1.json`. Validation tests are xUnit v3 in `Hexalith.Conversations.Contracts.Tests` (`DomainModuleAuthoringTemplateValidationTest`, `MinimalModuleAuthoringCostBaselineValidationTest`), run with `-automated sync -failSkips -trx`.
- Rollback is per story: remove only that story's generator, validator, results, generated bundle, and record; preserve predecessors and v1 history.

## Cross-Story Dependencies

- Strict chain: 11.1 -> 11.2 -> 11.3. 11.2 binds 11.1 guidance/record digests; 11.3 binds the 11.2 fixture proof and the preserved v1 baseline. All three checkpoints must resolve to one compatible candidate chain (mismatch is `CHECKPOINT_CANDIDATE_MISMATCH`).
- 11.1 additionally requires final records of 6.2, 7.4, and 10.4.
- 11.1 does not create the fixture or compute SM-2; 11.2 does not compute or decide SM-2; 11.3 does not alter the fixture or history.
- Any C# change after a candidate is recorded may invalidate bound digests and require rebinding.
