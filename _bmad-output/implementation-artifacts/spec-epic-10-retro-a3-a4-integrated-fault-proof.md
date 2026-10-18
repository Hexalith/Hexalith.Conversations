---
title: 'Epic 10 A3/A4: repair integrated hardlink and authority-chain proof'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context:
  - docs/runbooks/current-change-validation.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Integrated source lost the seven real authority-chain fault tests and its hardlink mutation harness no longer disables both composed protections. Epic 10 is rejected pending A-3/A-4. Latest CI also repeats Windows output-alias rejection, missing pytest imports in release-tooling, and a frozen Story 10.4 replay against later main.

**Approach:** Repair the existing mutation checker and restore all seven mutations against the actual authority document through the compiled `authorityChains` route. Given the current CI selection, when each fault executes, then require the exact blocker, evaluated guard ledger, original verifier result/exit and byte-identical restoration. Given an isolated disabled compiled authority guard, when these tests execute, then all seven must fail assertions. Given the hardlink regression, when both writer protections are disabled in memory, then the baseline must pass and the mutant must fail.

Fix the diagnosed current CI causes while retaining their assertions: normalize Windows output-directory aliases, provide the locked Python environment to release-tooling, and keep historical boundary replays explicitly available outside routine conformance. Use focused current-change validation. Preserve accepted records, receipts, bindings, schemas, original story specs and frozen authority bytes. Record fresh measurements as working-tree results, independently review, close only completed actions, and rerun the Epic 10 retrospective before any Epic 11 work. No publication, dependency update, gitlink change, commit, push, release or epic-lifecycle transition is requested.

</frozen-after-approval>

## Implementation Notes

- No intent gaps, irreversible actions or public API changes were found. This is a bounded tooling repair using the small oneshot route. It has no dotted story ID or v9 completion contract; historical publication and scoped boundary gates are not applicable.
- Inspected clean main at `e233a7140cd73a69459b09144efb3361ed8d836b`, latest CI run `39125796786` and previous run `39121960132`. Windows fails the existing trailing-dot/space assertions; release-tooling imports pytest modules without pytest; five native adoption tests intentionally enforce the historical full boundary at current HEAD.
- Reuse `tests/tooling/test_evidence_boundary_adoption.py::observed_fault`, current CI importer, and native `BoundaryAssertions.AuthorityChain` with Runner's independent frozen identities. The fixture bridge request must use `authorityChains` directly: the full adoption wrapper also imposes historical gitlink pins unsuitable for a disposable Git fixture.
- Restore the seven actual-document mutation constructions from branch source `c48ff6493a15bc6843dfaf7a9c722028b56d6676`, asserting mutation occurred, positive baseline, specific evaluated guard rows and exact restoration. Keep synthetic native ChainTable tests as separate coverage.
- Repair `check_adoption_hardlink_mutation.py` to disable collision and link-count rejection together, fail closed on changed targets or unexpected pytest outcomes, and restore the original function. Add an isolated compiled authority-guard mutation checker without editing workspace source or accepted evidence.
- Keep all existing stdlib tooling checks in `npm test`; supply CI's existing locked uv environment so pytest imports succeed. Exclude only the historical `EvidenceBoundaryAdoptionValidationTest` class from routine conformance, document the policy classification, and retain live helper/adoption faults in current Python CI.
- Verify native TestSupport, the exact routine focused lane, all adoption faults, promotion spans, receipt/parser compatibility, mutation commands, stdlib release tooling and current conformance. Store operational receipts under `TestResults/epic-10-retro-a3-a4/`; bind them to a source-file digest inventory and identify the dirty tree rather than claiming an immutable source candidate. Preserve historical evidence by comparing bytes against HEAD.
- Windows output segments now trim Windows trailing-dot/space aliases before forbidden-directory comparison. Existing Windows-specific assertions remain intact; local execution is Linux, so Windows-host confirmation remains for the next CI run.
- CI release-tooling now installs the existing locked Python environment and runs the unchanged 52-test stdlib suite within it. Routine conformance excludes only the five full frozen-boundary adoption ACs. `docs/runbooks/epic-10-fault-regressions.md` documents that classification, retained coverage and required mutation proofs without changing hash-bound historical guidance.
- The seven chain receipts now name the actual authority SHA-256 `7098e7355522f503e136dc8c5e84ce5c873cb5bad08b92aa54495811c54853b6`, with exact restoration. Tests additionally exercise the production wrapper's dispatch and merged ledger through disposable Git history. Its fixture removes only the independently asserted historical gitlink amendment before invoking the real bridge.
- Mutation success requires exact selected/call/failure counts, assertion type, the named guard-rejection assertion and an admissible verifier outcome. Separate assertions reject infrastructure states and unrelated blockers. Four new negatives prove unrelated chain/hardlink outcomes cannot supply that named failure.
- Native TestSupport: 44 passed; both current conformance tiers: 326 portable and 90 internal passed. Conformance uses Debug with existing project-reference outputs (`--no-restore -m:1 -p:BuildProjectReferences=false`) and the exact CI class/method exclusions. Both projects build successfully. No broad product AppHost rerun is claimed.
- The original broad generator selection was interrupted deliberately after 21 passing cases, exit 2, while rederiving historical publication/lifecycle fixtures. It is incomplete. The completed focused receipt/parser/compatibility selection passes 99 cases. Four accepted record pairs independently pass digest/schema checks; 49 retained evidence, contract, spec, authority, schema and guidance files compare byte-identically against the base revision.
- Final locked Python 3.11 verification passes 66 routine CI cases, 99 parser cases and both mutation commands. All 28 adoption receipts pass independent ledger/result/exit/restoration checks. Seven chain ledgers contain seven or eight rows and hash the actual authority bytes. Source-list digest: `2fa3a51410ef5274c0efffec1cf3204d5cdb67527540aa14fac0e6d46cda9f8d`. The retrospective is rerun as `accepted-with-open-items`; A-3/A-4 completion is authorized by the invocation, while Epic 11 remains untouched. Changes remain uncommitted as requested by the repository's task-scope safeguard.

## Review Triage Log

- **medium, patched (blind):** Generic assertion failures could count as killed mutants. Exact guard-rejection messages now qualify failures, and an independent unrelated-assertion probe is rejected with zero qualifying failures.
- **low, patched (blind):** Direct compiled-route tests omitted wrapper dispatch/merge coverage. Added a positive/negative wrapper regression using the real compiled guard and disposable history. The unchanged production verifier's call remains present; this does not replay its historical full boundary.
- **low, patched (blind):** Mutation commands were manual without a recurring rule. The new runbook requires both commands for guard, bridge or harness changes, preserving the focused CI lane.
- **low, patched (blind):** Historical class reclassification lacked operational documentation. The new runbook records the excluded frozen AC class, live coverage and retained explicit historical invocation.
- **low, patched (blind):** Locked-environment reproduction commands were missing. The new runbook records setup, both exact commands, exit semantics and baseline/mutant expectations.
- **medium, patched (verification/edge):** An intermediate smoke test omitted a required keyword. It was corrected before execution and subsequently replaced by the focused wrapper regression.
- **medium, patched (adversarial):** The intermediate smoke test invoked full historical verification in the routine lane. The final test invokes only the wrapper with disposable history; reviewer reinspection confirms the replay dependency is removed.
- **medium, patched (adversarial):** Named assertions still admitted `BLOCKED` or unrelated verifier failures as kills. Admissibility checks now require exact original rejection or clean `PASS` survival; four negative cases pass, and reviewer reinspection confirms the false-proof path is closed.
- Final edge-case and verification-gap lenses report no remaining findings. The adversarial reviewer rechecked both concrete findings as resolved. No new deferred work was introduced.
