# Epic 11 Context: Thin-Module Authoring Proof

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give module authors an executable recipe for building a minimal domain module on the public platform APIs, and give release maintainers a reproducible way to qualify the current package graph and onboarding commands. The original checkpoints cover approved authoring guidance, a proved fixture, and a measured authoring-cost decision. The November sprint supplement extends this work to clean package qualification using the current SDK and Builds catalog.

## Stories

- Story 11.1 — Correct and validate platform-hosted thin-module authoring guidance
- Story 11.2 — Build the reproducible minimal-module fixture against live platform APIs
- Story 11.3 — Generate authoritative SM-2 v2 evidence and decide OQ-2
- Story 11.4 — Restore package qualification and maintainer commands

## Requirements & Constraints

- Modules own contracts, domain behavior, module registration, and tests. The platform owns ServiceDefaults, Dapr wiring, event subscription, projection and query runtime, publication, health, and telemetry. A module may have one repository-only test AppHost. The project is non-packable and non-publishable, and its hand-authored files count toward SM-2.
- The fixture exercises registration, command handling, event production, and platform-hosted subscription, projection, query, and publication using public APIs. Qualification requires a clean Release build and execution of all seven named fixture assertions.
- Check the bytes of all 17 files bound by the frozen fixture proof, including the four original locks. Use four external successor locks for the current package graph. Restore from the published feed in an empty cache, then repeat in locked mode with a second empty cache and require byte-identical successor locks. [Frozen fixture proof](../../docs/release-evidence/thin-module-fixture-proof-v1.md)
- Pack all five Conversations packages and install the candidate artifacts in an independent clean consumer. Separately reproduce the documented mapped restore, build, and focused test from a clean checkout with direct submodules. The root tooling test command must run the maintained suite. Resolve the release-plan check with its owner and remove its public command and references together if the check is retired. [Current sprint supplement](../planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#story-114-restore-package-qualification-and-maintainer-commands)
- Preserve the accepted Story 11.3 decision. SM-2 uses an inclusive 50% threshold for hand-authored module-owned files, with LOC as supporting evidence. The proof measured 13 files against an estimated 58, a 77.59% reduction with high measurement confidence and low comparison confidence. Keep the historical SM-2 v1 baseline and the SM-1 compatibility surface of 13,289 lines intact. [OQ-2 decision](../../docs/release-evidence/story-11.3-final-record-v2.md#measured-sm-2-and-oq-2)

## Technical Decisions

- The artifact version and wire schema have independent compatibility rules. Conversations 2.x targets `net11.0` and retains wire v1. Use the active workspace-tool pin, producer source mapping, and SDK target in maintained examples. [Current architecture](../planning-artifacts/architecture.md#ad-12--package-compatibility-and-qualification-adopted)
- Release and Builds own the fixture package reconciliation. The November audit reproduced a catalog mismatch between the external overlay and the directly pinned Builds catalog. Keep the drift guard and qualify the effective catalog from the direct checkout. Changes to generated CI or shared tooling go through CiCd or Builds ownership. [Sprint change proposal](../planning-artifacts/sprint-change-proposal-2026-11-20.md)
- Published guidance, fixture proof, and final records remain evidence for their measured candidates. Use the current-change runbook for Story 11.4 validation and retain the historical checkpoint records. [Current validation policy](../../docs/runbooks/current-change-validation.md)

## Cross-Story Dependencies

- Story 11.1 used 6.2, 7.4, and 10.4 as hard predecessors. Stories 11.1–11.3 bound the guidance, fixture, and authoring-cost evidence on one compatible candidate chain. Story 11.4 uses the frozen fixture proof as the byte-identity reference for its current qualification.
- Story 9.3 can identify the supported host in parallel. The packaged-client workflow and Story 9.5's compatibility matrix use the qualified artifacts from 11.4. Epic 15's entry also requires 11.4. [Current execution rules](../planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#current-execution-rules--24-november-2026)
