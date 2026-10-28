---
title: 'Qualify producer preparation and refresh held Epic 16 tooling bindings'
type: 'feature'
created: '2026-10-27'
status: 'done'
baseline_commit: 'c71e6ba924f42e7f5da62116f9e3b4fd5d77227e'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-16-tooling-qualification-and-binding-refresh-plan.md'
---

<frozen-after-approval reason="separate prerequisite proposal; user selected planning only">

## Intent

**Problem:** Accepted CLI 4.35.2 cannot execute the implemented CI preparation command. The existing Epic 16 source binding rejects a legitimate committed attributes change before reaching the runtime-entry gate.

**Approach:** Measure the existing producer's bounded preparation capability, retain every unqualified release/owner gate, and introduce an explicitly selected v2 tooling package using shared consumers. This delivers a truthful prerequisite handoff and a working held route; it does not qualify runtime execution.

**Decision:** On 2026-10-27 the user selected preparation of this separate plan. Implementation is not yet approved. The earlier remediation's offline baseline exception does not automatically extend here.

**Baseline decision:** The user selected the normal AppHost baseline. Run and inspect the existing Conversations AppHost before code edits; an unsuccessful baseline blocks those edits. No offline exception is granted. This local baseline grants no runtime-story entry.

## Boundaries & Constraints

**Always:** Preserve accepted v1 carriers, schema, trust pins and evidence. Keep all 18 ACs, 32 requirements, 38 graph nodes, 61 edges, rollback and native measurement obligations. Pass immutable package policy explicitly. Preserve FAIL/BLOCKED distinctions and nonempty ledgers. Retain null entry, empty independent entry trust, ACTIVE hold and false authority flags.

**Never:** Silently repin v1, remove the attributes rule, fall back after v2 failure, nominate owner trust, change tool pins/gitlinks, publish, initialize nested dependencies, retire hosts, alter accepted fixtures, or implement runtime 16.1–16.3. Local compilation remains Debug. Published-package, platform-specific, operational and human acceptance remain separate measured gates.

## I/O & Edge-Case Matrix

| Input | Required behavior |
| --- | --- |
| Existing producer source tests | Report only their executed source scope |
| Missing published package, tag or platform evidence | Retain BLOCKED qualification; no acceptance inference |
| Existing v1 commands | Retain FAIL/SOURCE_DRIFT on `.gitattributes` |
| Explicit v2 contract with null entry | BLOCKED/ENTRY_QUALIFICATION_PENDING; no runtime output |
| Altered pin, inventory, selector or executable | Fail closed; no cross-version fallback |
| Synthetic fixture success | `runtimeQualified=false`; isolated fixture outputs only |

</frozen-after-approval>

## Code Map

[Detailed plan](epic-16-tooling-qualification-and-binding-refresh-plan.md) freezes the inspected revisions, review inventory, exact commands, proposed footprint and publication gates. Builds already owns `BuildPreparationService`, its CLI and action; reuse them. Existing consumers use global package paths, so adding a binding alone cannot select v2.

[Current investigation](story-16-1-entry-investigation-2026-10-28.md) supersedes dated pin, scanner-count and first-failure observations: root CLI is 4.38.1, direct Platform remains 4.35.2; ordinary validation passes, adoption has six passing/twelve failing rows. All six v1 commands fail first on `.config/dotnet-tools.json`; independent recomputation also finds `.gitattributes` drift. Preserve both current files, accepted v1 inputs and all twelve scanner findings. The committed mapped AppHost baseline retry passed readiness and cleaned up its own instance; inspect and bind that source evidence before implementation and retain a fresh baseline if current inputs differ.

## Tasks & Acceptance

**Execution after approval:**

- [x] `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` — run the existing mapped Debug topology, inspect resource readiness and retain the baseline before changing code; use the plan's exact CLI target and preserve other sessions.
- [x] `module/validation/epic-16-tooling-qualification-v2/` — retain fresh bounded producer tests, source mapping, command streams, identities and complete qualification ledger. Keep unavailable artifact/platform/owner gates explicit; upstream corrections require their own scope.
- [x] Planning `epic-16-tooling-v2/` — add README, three contracts and binding; preserve exact predecessor obligations and independent source inventory. Generate the binding last, excluding its own digest.
- [x] `_bmad/schemas/epic-16-tooling-v2.schema.json`, `_bmad/schemas/.gitattributes`, `_bmad/scripts/epic16_trust_v2.py` — add scoped LF policy, closed successor schema and independent contract/schema/predecessor pins; preserve v1 files.
- [x] `_bmad/scripts/epic16_packages.py`, `epic16_tooling.py` — introduce immutable explicit package selection across validation, source/Git binding, measurement and generation; retain shared neutral infrastructure and executable-closure checks.
- [x] `_bmad/scripts/generate_story_record.py`, `verify_story_16_{1,2,3}.py` — allowlist exact v2 dispatch and declared imports while preserving standalone legacy routes and error classification.
- [x] `_bmad/scripts/tests/test_epic16_tooling_v2.py` and existing affected suites — exercise the matrix, all fifteen native fixtures, three deterministic final records, independent fault guards and version isolation.
- [x] `module/README.md`, `module/platform-handoff.md`, runtime draft — publish measured dispositions and pending release/source/catalog/owner requirements without lifecycle advancement.

**Acceptance Criteria:**

- Given exact producer inputs, when focused tests and source preparation execute, then their actual results and limits are retained, and no source or synthetic package result becomes published/Platform acceptance.
- Given preserved v1 and explicit v2 contracts, when all six Story 16.1 commands run for each, then v1 retains its measured FAIL and v2 reaches entry BLOCKED, with zero measured runtime tests and all seven runtime output paths absent.
- Given mutated selected-package inputs, when each named fault runs, then its guard rejects with a nonempty ledger; unchanged fixtures retain all native/final-record obligations and legacy dispatch behavior.

## Implementation Notes

2026-10-27: The user approved this prerequisite spec and chose to stop before execution. Resume with the required AppHost baseline. No implementation task has started; Story 16.1 remains held.

2026-10-27 execution attempt: [Baseline evidence](../../module/validation/epic-16-tooling-qualification-v2/attempt-20261027-apphost-baseline-01/README.md) records the isolated AppHost launch exiting 2 with seven missing-source-mapping build errors. Source preparation was BLOCKED because Builds and Platform checkouts differ from the recorded root gitlinks. No instance started, and no tooling or runtime code was changed. Required implementation/tests remain incomplete; the normal-baseline gate stops further edits. Stream/log hashes and preservation checks passed. Story 16.1 remains draft/backlog and held.

2026-10-28: The user selected option 1, resuming this approved prerequisite implementation. Preserve the existing runtime-draft/context edits and fresh entry observations. The [successful baseline retry](../../module/validation/epic-16-tooling-qualification-v2/attempt-20261028-apphost-baseline-02/README.md) clears the prior launch blocker within its readiness-only scope. No tool-pin alignment, scanner exemption, owner-entry authority or runtime implementation is authorized.

2026-10-28 implementation: [Fresh source evidence](../../module/validation/epic-16-tooling-qualification-v2/attempt-20261028-implementation-03/README.md) binds a new successful mapped Debug readiness/cleanup baseline, 525 passing producer native source tests and nine action tests. The original 180-second adoption-class timeout is retained; its bounded retry passed 396 tests. Explicit tooling-v2 selection and shared immutable policy are implemented. Focused v2/native/final-record/fault checks pass, including independent v1 pin/attributes faults. All six actual v1 commands retain FAIL/SOURCE_DRIFT on the current tool manifest; all six v2 commands reach BLOCKED/ENTRY_QUALIFICATION_PENDING with zero runtime tests and seven absent outputs. Required current regressions now pass: 2,885 tooling, 1,516 generator, 131 adoption v1 and 162 adoption v2 cases, plus 60 focused v2 cases. The existing directory-selection policy retires exactly 17 live Story 11.3 generator cases; current selection exactly matches the 1,516 passing JUnit identities. Earlier explicit-file failure and interrupted attempts remain retained. Runtime remains draft/backlog, entry null, independent trust empty, ACTIVE hold and all authority flags false. Published artifact, Platform/native-host, operational and human-owner gates remain separately BLOCKED.


2026-10-28 validation disposition: Neutral helper failures were reproduced as missing producer mapping, then resolved with all seven returned source/Debug environment values; bridge/helper/carrier bytes remain unchanged. Adoption fixtures now load additive committed-history root blobs verified against unchanged accepted source/artifact pins, preserving live files and existing fixtures. Both exact Story 7.3 guards passed in an isolated copy. All source/schema/selector/native/final-record/fault checks pass; full evidence and the six-row matrix audit are in the attempt directory. All 194 protected file hashes, direct dependency tuples, initial HEAD and exact staged mode/object/path inventory remain unchanged. Raw index bytes differ, and no saved initial bytes exist to identify the changed fields; no byte-identical-index or cache-only claim is made and no index was restored. Runtime entry and separate release/Platform/owner qualifications remain BLOCKED.

2026-10-28 review closeout: Three independent review layers produced thirteen findings. Four fixes are verified; five findings sharing four pre-existing causes are recorded in deferred work, and four findings were rejected with individual reasons below. [Fresh review-fix evidence](../../module/validation/epic-16-tooling-qualification-v2/parent-review-20261028/fixes/README.md) binds 525 passing producer cases to unchanged before/after identities for 92 execution-directory files. Parent verification passed the complete updated 64-case focused suite, confirmed all 81 selected source hashes, exact 1,516 generator/JUnit identities, all 326 original-attempt files and the seven absent runtime outputs. The 4,694 current compatibility cases and nine producer-action cases remain valid; no bound executable source changed during review. Only this separately approved prerequisite is complete. Story 16.1 remains draft/backlog, runtime entry BLOCKED and publication not-applicable. This prerequisite has no dotted v9 story-completion contract; no runtime final pair is generated.

## Spec Change Log

## Review Triage Log

- BH-01 — **false / reject**. `unknown-contract.json` without the tooling dialect selects no successor package. The generic generator retains legacy dispatch; guessing that an unknown path denotes v2 would violate exact selection. Exact v2 paths and the explicit tooling dialect already produce rejection ledgers.
- BH-02 — **low / reject**. The verifier uses argparse's last-value behavior for repeated options, while the generic generator rejects duplicate contract options. The final verifier value still passes the exact allowlist and cannot bypass entry. This uncommon malformed invocation does not justify another parser guard or changing legacy option semantics.
- BH-03 — **false / reject**. Invalid `--scope` is an argparse usage error outside the accepted command grammar, as on v1. The successor's nonempty evidence ledger applies to evaluated package/scenario faults, not help or malformed required-option syntax; no evaluated outcome or authority is accepted.
- BH-04 — **medium / defer**. An unexpected mapped configuration can empty the gateway provenance loop. The method and its callers are unchanged from implementation-entry HEAD `ef01d578a00efd527bae9226dca302f54cd012f0`; record the pre-existing non-vacuity gap without modifying frozen host consumers.
- BH-05 — **medium / defer**. Current CI runs an explicit unittest list and does not collect the new pytest suite, or the existing Epic 16 pytest family. This pipeline gap predates the implementation. A CI lane must respect the package/Release environment and the separately pending supported helper strategy; the bounded source qualification does not establish that lane.
- BH-06 — **medium / defer**. The existing Story 6.4 v4 observation records source/XML hashes and a mapping hash without retained binary/mapping identity. Those committed evidence bytes predate this implementation and remain unchanged. Any stronger proof needs an additive, separately owned successor rather than editing accepted evidence.
- BH-07 — **low / patch**. The new native summary binds 525 outcomes to TRX and the producer source/build commands, but its assembly inventory omits the test assembly actually invoked. Add fresh execution-bound test-assembly/source observations with before/after hashes and distinct reports; preserve the completed attempt.
- BH-08 — **low / patch**. The module README's no-change statement is correct for the implementation slice, but the inherited baseline diff also contains antecedent host/tool/dependency commits. Name the implementation-entry revision directly in that statement.
- BH-09 — **low / reject**. The frozen dated proposal and later approval notes require chronological reading, while current frontmatter already says in-review. The requested correction edits this build's spec and is rejected under the review workflow's explicit rule; no additional approval is required or inferred.
- BH-10 — **low / patch**. Held-runtime coverage directly exercises only Story 16.1 with absent outputs. Add isolated actual verifier/generator tests for all three selected contracts, including pre-existing evidence, to demonstrate byte preservation as well as zero measurements and false authority.
- EC-01 — **medium / defer**. The retained AppHost disables discovery and supplies no tenant subscription through its sidecar resources. On a clean local run without external subscriptions, tenant delivery is unqualified. This topology is unchanged from implementation entry; readiness-only evidence and the handoff already leave live delivery pending. Record the operational gap for the owning runtime scope.
- EC-02 — **medium / defer**. Independently confirms BH-04's empty-configuration loop and skipped provenance assertions. Same pre-existing root cause; retain this finding individually and group its deferred action with BH-04.
- VG-01 — **medium / patch**. The pre-verified gap demonstrates that removing selected-policy propagation from insertion rederivation rejects a valid v2 record while existing tests stay green. Add actual generator insertion verification with v2 contract/schema assertions, a passing control, and a corrupted-region rejection. Production propagation is already correct; the missing guard is a regression test.

Patch verification: BH-07 is closed by the fresh four-class rerun and before/after execution inventories; BH-08 by the explicit implementation-entry comparison; BH-10 and VG-01 by actual CLI regressions. All four new cases passed, followed by the parent's complete 64-case focused run with zero failures/errors/skips. The original qualification attempt and every bound executable remained byte-identical throughout these fixes.

## Design Notes

Shared executable changes are explicitly proposed; accepted v1 data/trust/evidence bytes remain fixed. V1 therefore continues to expose drift. Finalize contracts/schema, then independent pins, then executable sources, then the v2 binding to avoid digest cycles. Reuse existing final-record destinations and native rules; package v2 does not redefine their wire contract.

The matrix's v1 attributes-drift guard remains required and needs its own passing fault regression. Current exact v1 commands encounter the earlier tool-pin mismatch first; retain that measured FAIL rather than changing either accepted digest or the legitimate current pin. Tests must independently demonstrate rejection of both source mutations. Runtime qualification and publication remain held.

The parent audit found two additional boundary gaps: selected-policy capture could raise an unstructured contained-reader failure, and the new independently pinned schema lacked LF protection. Lazy selection now preserves reader FAIL/BLOCKED codes through the shared rejection ledger. The scoped `_bmad/schemas/.gitattributes` protects only itself and the v2 schema, is in the frozen v2 source inventory, and passes a controlled autocrlf checkout. Six actual verifier/generator capture-fault cases were added with positive controls; interrupted pre-correction test runs remain explicitly incomplete. Root attributes and v1 inventories are unchanged.

## Verification

First capture the AppHost run and resource-state baseline. Then run the plan's bounded individual Debug/native and action commands, focused v2 plus affected v1/legacy Python suites, both Story 7.3 guards, and twelve actual held commands. Compare exact obligations, protected bytes, raw gitlinks/checkouts, output absence and whitespace. This slice cannot complete on failed required implementation tests; qualification exclusions remain separately BLOCKED rather than silently waived. Current historical boundary replay is not required.
