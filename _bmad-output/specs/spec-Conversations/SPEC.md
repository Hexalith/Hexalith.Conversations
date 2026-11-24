---
id: SPEC-Conversations
companions:
  - ../../planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md
  - ../../planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md
  - ../../planning-artifacts/ux-design-specification.md
  - ../../planning-artifacts/ux-requirement-map.md
  - ../../planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md
  - ../../planning-artifacts/architecture.md
  - ../../project-context.md
  - ../../../docs/runbooks/current-change-validation.md
sources: []
---

> **Canonical contract.** This SPEC and the files in `companions:` are the complete, preservation-validated contract. The companions remain owned by their originating workflows and are not rewritten by this package.

# Conversations Current Planning Authority And Execution Rules

## Why

Current Conversations planning authority sits in the 2026-11-21 **Current planning entry point** of `architecture.md`, and on 2026-11-24 the canonical `epics.md` replaced the V9 execution machinery for Stories 12.3–15.2 and Gate RG-15 with the routine current-change path. A SPEC that still names V15 as current authority sends remaining stories back to retired ceremony and contradicts its own architecture companion. Consumers need one contract that routes them to those two sections, keeps the accepted product scope and completed history intact, and reads the V15 hold and later verdicts as historical record rather than live gates.

## Capabilities

- **CAP-1**
  - **intent:** Planning and execution consumers can determine the current Conversations planning authority, the execution rules for the remaining stories, and which earlier hold states and verdicts are historical.
  - **success:** A consumer opens the `architecture.md` companion at **Current planning entry point**, takes acceptance from the November 20 PRD, work from the 20 November sprint supplement and delivery from the sprint tracker, applies **Current execution rules · 24 November 2026** in the canonical `epics.md` to Stories 12.3–12.4, 13.1–13.3, 14.1–14.3 and 15.1–15.2 and Gate RG-15, and treats the V15 hold, the AR-15 status, the V14 Epic 16 rejection, the V22 marker and the V29 `FAIL` as historical records that gate neither entry nor completion.

## Constraints

- The 2026-11-21 **Current planning entry point** in `architecture.md`, with its clarifications of the inherited decisions, is the current planning authority together with every earlier invariant it does not clarify. `architecture.md` is controlling; this kernel narrows none of its decisions or inherited security, privacy, authority and fail-closed rules.
- The November 20 PRD sets acceptance, the 20 November sprint supplement in the canonical `epics.md` names the work, and `_bmad-output/implementation-artifacts/sprint-status.yaml` records delivery.
- AD-3 for current work: routine changes follow current policy, and every push requires a passing `python3 references/Hexalith.Builds/check.py` from the module root.
- **Current execution rules · 24 November 2026** governs Stories 12.3–12.4, 13.1–13.3, 14.1–14.3 and 15.1–15.2 and Gate RG-15. A story enters when the hard-entry predecessors named in its epic are `done` in the tracker. Its candidate is the committed `HEAD` with every root-declared gitlink, checked by `python3 scripts/check-root-submodules.py --repository .`. It completes through the routine path of the current change validation runbook.
- For that work the `PC` binding, the fixed ten-path gitlink inventory, `PC_UNBOUND`, `AUTHORITY_MISMATCH`, `v9/story-contracts` files, the v9 schema identity and `v9-authority-bundle-v1.json` are not prerequisites; an existing contract such as `12.3.json` may still supply scenarios. Each story spec states every acceptance scenario with its command and expected result, builds a named generator or verifier only when that scenario's evidence requires it, runs test assemblies in the producer-selected configuration and the current target framework, records each replacement of a `bin/Release/net10.0` path, and runs the v2 final record and `--verify-inserted-record` only when it requires them. A waived scenario is recorded as waived, never as `PASS`.
- Scenario intents, fault cases and evidence deliverables stand: the projection proof v3 chain in Epic 13, the v3 preservation manifest in Epic 14, and the release revalidation and attestation v2 in Epic 15. Story 15.1 also waits for Stories 8.3, 9.3–9.5, 11.4, 16.4 and 16.5.
- RG-15 remains the only route to release closure: an independent review followed by an explicit release-owner decision. The universal SM-C2 rule, the pending preservation approval and the other release gates keep their requirements; the execution rules remove ceremony only.
- The tracker marks Stories 16.1–16.3 `done` under story-scoped waivers whose native evidence covers those stories only; production provider parity, the operating envelope and exporter verification need their own evidence. Stories 16.4 and 16.5 take acceptance from the 20 November supplement and the entry point's AD-7 and AD-5 clarifications.
- Historical record, read through the runbook's **Historical boundary**: the V15 AD-3 recomputation that kept the execution hold `ACTIVE` on `V21_DESCENDANT_GITLINK_DRIFT`; V15's AR-15 status `BLOCKED_BOOTSTRAP_AUTHORITY`, superseded by V16 and V22; V15's rejection of the V14 Epic 16 carriers as execution-sufficient, with its per-story AD-5 through AD-8 entry gates; the V22 marker; and V29's `FAIL` with hold `ACTIVE` and all four authority flags false. Earlier blocks, records and verdicts keep their bytes and meaning; none gates entry or completion of current work, and no consumer or story claims any hold lifted.
- Preserve the canonical PRD and addendum; exactly 124/124 functional requirements comprising 20 initiative FRs and 104 `Feature-FR`s; all 77 `Feature-NFR`s; FR-16 as the sole deferred and non-activated initiative requirement; all 52 UX decisions; and all 28 UX acceptance IDs.
- Preserve Epics 1–5 and completed Stories 6.1, 6.2, and 6.7, including their completed records, accepted baselines, signed evidence, and submodule bindings, as immutable history.
- This update changes only the bmad-spec package. Architecture, PRD, epics, UX artifacts, tracker, story status, product code, dependencies, submodules, gitlinks, completed records, signed evidence, and public value sets remain unchanged.
- Every file in `companions:` is adopted and read-only to bmad-spec; its original wording and authority remain intact.

## Non-goals

- Claiming any hold lifted, completing AR-15, or reviving V9–V29 authority, publication or validator machinery as a gate.
- Changing product, technical, requirement, UX, execution-rule, evidence or completed-history semantics, or rewriting earlier blocks, records or verdicts.
- Making the RG-15 release decision, changing story status, starting implementation, accepting a story, releasing, or pushing.

## Success signal

A developer starting Story 12.3, 13.1, 14.1 or 15.1 from this package reaches the entry point and the 24 November execution rules and needs no IR-0, `LIFTED` decision, v9 bundle binding or entry waiver. The package still preserves the accepted scope and completed history, records the V15 hold and verdicts as historical, and passes coherence and preservation validation with only the spec workspace changed.

## Assumptions

- `docs/runbooks/current-change-validation.md` is an adopted companion because both cited sections route completion and historical interpretation through it.
