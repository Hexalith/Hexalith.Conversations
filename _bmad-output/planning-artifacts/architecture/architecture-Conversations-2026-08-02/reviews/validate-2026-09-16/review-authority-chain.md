# Authority-Chain And Data-Integrity Review

**Target:** `_bmad-output/planning-artifacts/architecture.md`  
**Repository state:** `fd3dd58c3c10b512c79cc425f94d57c4d3400b20` (`main`, equal to `origin/main`)  
**Review date:** 2026-09-16  
**Mode:** validation only; no target or authority artifact was changed

## Verdict

**FAIL — current effective authority is not deterministically resolvable from the architecture entry point, and the current V21 checker restores the Story 7.1 hold to `ACTIVE` while the installed V21 artifact still presents an unqualified `LIFTED` effect.** The append-only architecture bytes and the recorded predecessor digests are internally intact, but the V13 discovery/publication contract was not followed for V15–V21 and the latest point-in-time lift is stale at HEAD.

Severity counts: **1 critical, 2 high, 1 medium**.

Until a successor authority is published and passes the current checker, the safe effective interpretation is **`ACTIVE` for Story 7.1**. Historical V17/V20/V21 `LIFTED` facts remain valid only for the exact candidates they assessed; they are not current authorization at HEAD.

## Findings

### AUTH-01 — Critical — Installed V21 says `LIFTED`, but the current checker says `ACTIVE`

The installed V21 record reports `PASS`, `implementationHold: LIFTED`, and `fullStoryExecutionAllowed: true` at lines 118–125 of `_bmad-output/planning-artifacts/v21-story-7.1-authority-correction-v1.json`. Its lineage and V20 predecessor digest are explicit at lines 4–20, and that predecessor digest equals the current raw SHA-256 of V20 (`7719deb5…aa09`). These are valid publication-time facts.

They are not current facts at HEAD. Running the current authority checker returned exit `1`, blocker `V21_DESCENDANT_GITLINK_DRIFT`, and `effectiveHold: ACTIVE`. The reported drift spans post-V21 commits through HEAD, including EventStore, Folders, and FrontComposer gitlink changes. This is the designed fail-closed behavior, not evidence that V21's historical publication was corrupt.

This produces an unsafe read if a consumer treats the installed artifact's `authorityEffect` as live state without recomputing it. The repository's own protected-branch workflow treats that recomputation as mandatory at `.github/workflows/planning-authority-preflight.yml:233-255`.

**Disposition:** **discuss, then update.** Treat the current effective Story 7.1 hold as `ACTIVE`. Do not use the raw V21 `LIFTED` field as current authorization. If Story 7.1 execution must resume against HEAD, publish a successor authority bound to the current gitlinks and require the V21/successor effective-hold check to pass before lifting.

### AUTH-02 — High — V15–V21 are not discoverable through the architecture's binding pointer

The architecture defines an unambiguous discovery contract:

- `_bmad-output/planning-artifacts/architecture.md:2415-2430` says the last complete overlay marker names the current sidecar head and that every new `v<N>-*-authority` sidecar and its pointer amendment must be published in the same commit; publishing either alone is an authority-publication failure.
- The last complete marker is still V14 and names `v14-current-candidate-authority-v1.json`, digest `e96c34df…da7f`, with `hold=ACTIVE` (`architecture.md:2627` and `architecture.md:2698`).
- V15, V16, V17, V18, V19, V20, and V21 authority files were subsequently committed between 2026-08-22 and 2026-09-15, while `git log -- architecture.md` shows no architecture commit after the V14 publication on 2026-08-19.

Consequently, three plausible readers reach different states:

1. The normative architecture resolver stops at V14 and reads `ACTIVE`.
2. A filename/version scanner selects V21 and reads `LIFTED` (`v21...json:118-125`).
3. The current V21 checker evaluates HEAD and returns `ACTIVE` because of descendant gitlink drift.

The first and third happen to agree today, but not through one authoritative resolution chain. A reader cannot discover V15–V21 or establish which later artifact is effective by following the architecture's declared rule.

**Disposition:** **discuss, then update.** Append a new architecture pointer overlay in the same publication transaction as the next effective authority head. It must bind the predecessor block bytes/digest, exact sidecar path/digest, candidate binding, and effective hold. Do not rewrite V1–V14.

### AUTH-03 — High — Post-V13 point-in-time authorities omit the required `statusAsOf` qualification

The architecture requires every regenerated point-in-time sidecar to carry `statusAsOf` so frozen status cannot be mistaken for current status (`architecture.md:2592-2598`). None of the V15–V21 authority JSON files contains a `statusAsOf` field.

The omission matters because the sequence intentionally changes state:

- V17 validly lifts only `7.1-SCHEMAS` (`v17-implementation-hold-decision-authority-v1.json:121-129`; the companion hold record scopes the lift at `implementation-hold-v1.json:10-18,24-37`).
- V18 and V19 record full-story hold `ACTIVE` and explicitly preserve V17 as a point-in-time scoped lift (`v18-package-environment-authority-v1.json:213-252`; `v19-story-7.1-checkpoint-completion-authority-v1.json:9-29,255-264`).
- V20 then records a release-owner lift for Story 7.1 only (`v20-story-7.1-release-owner-authority-v1.json:4-23,168-177`).
- V21 preserves that lift at its publication candidate (`v21-story-7.1-authority-correction-v1.json:118-130`), but the current checker now invalidates it at HEAD.

The predecessor chain makes those historical transitions auditable, but without `statusAsOf` or a valid current-head pointer, their status-shaped fields remain easy to misread as current.

**Disposition:** **update.** Successor sidecars should carry an explicit point-in-time marker/candidate and distinguish `recordedEffectAtCandidate` from recomputed `effectiveStateAtEvaluatedHead`. Existing immutable records should remain unchanged and be qualified by the successor/pointer rather than rewritten.

### AUTH-04 — Medium — Current tests prove frozen bytes, not current authority discovery

`ArchitecturePlanningAuthorityValidationTest` passed all 18 tests at HEAD, yet it does not detect AUTH-02. Its frontmatter test still deliberately binds the frozen V8 provenance (`ArchitecturePlanningAuthorityValidationTest.cs:193-223`), and `PlanningAuthorityV9ValidationTest` describes V14 as closing the chain and only extracts V9–V14 blocks (`PlanningAuthorityV9ValidationTest.cs:48-99`). No test walks the V13 discovery rule to the latest committed `v<N>-*-authority` file.

The direct `PlanningAuthorityV9ValidationTest` run also failed 1 of 7 tests because `.agents/skills/bmad-dev-auto/SKILL.md` is missing; the failing code reads the obsolete alias at `PlanningAuthorityV9ValidationTest.cs:459-463`. This is distinct from authority corruption, but it means the current direct V9 class is not green. The workflow correctly runs the complete historical V9 publisher in a detached historical checkout (`planning-authority-preflight.yml:299-338`) and uses the V21 checker for live effective-hold resolution (`planning-authority-preflight.yml:233-255`), so a direct HEAD failure from `publish_v9_planning_authority.py --check` is not by itself proof that the immutable V9 publication is invalid.

**Disposition:** **update.** Add a current-head resolver test that (a) enforces the V13 pointer rule, (b) walks predecessor identities/digests to the selected authority, and (c) asserts the recomputed effective hold. Separately update the stale skill-alias fixture so the direct V9 test class is green or explicitly historical.

## Integrity Checks That Passed

### Append-only overlay markers

An independent byte calculation over `architecture.md` confirmed:

| Block | Bytes | SHA-256 | Binding result |
| --- | ---: | --- | --- |
| V9 | 18,270 | `46862123…f3d9` | V8 prefix digest matches |
| V10 | 3,846 | `893315bf…74b` | V9 bytes and digest match |
| V11 | 3,042 | `a97385c1…c4d1` | V10 bytes and digest match |
| V12 | 6,075 | `3050b326…796` | V11 bytes and digest match |
| V13 | 17,857 | `c7d5c867…4605a` | V12 bytes/digest and V14 sidecar digest match |
| V14 | 3,873 | `d33d977f…6dc5` | V13 bytes/digest and V14 sidecar digest match |

The file ends exactly after the V14 END marker plus its trailing LF, consistent with the grammar at `architecture.md:2442-2454`. No evidence of an in-place rewrite was found.

### Predecessor identities and raw digests

The checked-in hashes form a coherent historical chain:

- V16 pins V15 as `bac4dc43…37ea` (`v16-planning-tooling-lifecycle-authority-v1.json:103-106`), matching the current V15 file.
- V17 pins V15 and V16 as `bac4dc43…37ea` and `5b71e6fb…d45a` (`v17-implementation-hold-decision-authority-v1.json:98-104`), both matching.
- V18 pins V15, V16, V17, and the hold record as `bac4dc43…37ea`, `5b71e6fb…d45a`, `1444f76d…6e50`, and `2c594075…0b12` (`v18-package-environment-authority-v1.json:213-233`), all matching.
- V19 pins V18 as `24891d99…1d94` (`v19...json:4-7`), V20 pins V19 as `58b7bc8e…89ce` (`v20...json:4-7`), and V21 pins V20 as `7719deb5…aa09` (`v21...json:4-7`); each matches the current raw file digest.

Thus the defect is discovery/currentness, not a broken recorded predecessor digest.

## Reproduced Commands And Results

All commands were run from `/home/administrator/projects/hexalith/conversations` without modifying the target.

| Command | Exit | Result |
| --- | ---: | --- |
| Independent Python byte/digest walk of V9–V14 marker blocks | 0 | All prefix, predecessor byte-count, predecessor digest, and V13/V14 sidecar-digest checks matched; terminal LF grammar matched. |
| `python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check` | 1 | `CANDIDATE_SOURCE_DRIFT: pyproject.toml`. This is a HEAD/current-candidate result; the workflow validates the immutable V9 publication at historical commit `6400c09…`, so it was not treated as corruption of historical V9. |
| `python3 _bmad/scripts/publish_v13_current_proof_authority.py --repository . --check` | 0 | `V13_CURRENT_PROOF_AUTHORITY_OK` |
| `python3 _bmad/scripts/publish_v14_current_candidate_authority.py --repository . --check` | 0 | `V14_CURRENT_CANDIDATE_AUTHORITY_OK` |
| `python3 _bmad/scripts/publish_v15_planning_tooling_environment.py --repository . --check` | 0 | `V15_PLANNING_TOOLING_AUTHORITY_OK` |
| `python3 _bmad/scripts/publish_v16_planning_tooling_lifecycle.py --repository . --check` | 0 | `V16_PLANNING_TOOLING_LIFECYCLE_OK` |
| `python3 _bmad/scripts/publish_implementation_hold_decision.py --repository . --check` | 0 | `HOLD_DECISION_OK ... STATE=LIFTED UNLOCKS=7.1-SCHEMAS` |
| `python3 _bmad/scripts/publish_v18_package_environment_authority.py --repository . --check` | 0 | `V18_PACKAGE_ENVIRONMENT_AUTHORITY_OK` |
| `python3 _bmad/scripts/publish_story_7_1_successor_authorities.py --repository . inventory --check` | 0 | `PASS`; 10 inventories, all observation/checkpoint ledger entries PASS |
| `python3 _bmad/scripts/publish_story_7_1_successor_authorities.py --repository . v19 --check` | 0 | `V19_STORY_7_1_CHECKPOINT_AUTHORITY_OK` |
| `python3 _bmad/scripts/publish_story_7_1_successor_authorities.py --repository . v20 --check` | 0 | `V20_STORY_7_1_RELEASE_OWNER_AUTHORITY_OK ... DECISION=LIFTED` |
| `python3 _bmad/scripts/publish_story_7_1_successor_authorities.py --repository . v21 --check` | 1 | `V21_DESCENDANT_GITLINK_DRIFT`; returned `effectiveHold: ACTIVE` |
| `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -class Hexalith.Conversations.Conformance.Tests.ArchitecturePlanningAuthorityValidationTest` | 0 | 18 total, 18 passed, 0 skipped |
| `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -class Hexalith.Conversations.Conformance.Tests.PlanningAuthorityV9ValidationTest` | 1 | 7 total, 1 failed: missing `.agents/skills/bmad-dev-auto/SKILL.md` |

## Handoff

The current data is fail-closed, so no unsafe lift should be inferred: **Story 7.1 effective hold is `ACTIVE` at HEAD**. An update should preserve all historical bytes, append a new pointer-bearing overlay and successor authority, add `statusAsOf`/evaluated-head semantics, and make the deterministic current-head resolver the single route used by human readers, conformance tests, and CI.
