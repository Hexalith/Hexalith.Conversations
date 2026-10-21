---
title: 'Epic 11 retro A-1: enforce fixture inventory and solution equality'
type: 'bugfix'
created: '2026-10-21'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epic 11 retrospective F-1 shows that the fixture inventory-only CLI passes an identical-byte external symlink with only 12 evaluated rows. The graph checker also passes a solution missing its AppHost while all four project files remain present.

**Approach:** Complete retrospective action `epic-11-retro-item-45-enforce-fixture-inventory-and-solution-equality`. Reject symlinked, missing, or otherwise unevaluated frozen rows with `MINIMAL_FIXTURE_INVENTORY_DRIFT`; require the physical project list and parsed solution entries to equal the four frozen projects, rejecting omissions, additions, and duplicates with `APPHOST_BOUNDARY_INVALID`. Retain a passing 13-row/four-project control and standalone negative regressions, including the real inventory-only CLI and byte restoration. Preserve the frozen fixture, its fault ledger, accepted publications, and historical evidence. The user selected A-1 first; A-2 through A-4 remain deferred.

</frozen-after-approval>

## Implementation Notes

- Planning: no unresolved intent gaps or irreversible operations. The implementation is limited to `_bmad/scripts/verify_minimal_module_fixture.py` and `_bmad/scripts/tests/test_minimal_module_fixture.py`, with this spec, appended scope deferrals, and closure of only action item 45 in sprint tracking.
- Reuse `FROZEN_RELATIVE`, `EXPECTED_PROJECTS`, existing ElementTree support, and the disposable test repository. Keep new regressions outside `FAULTS` and `FAULT_EXPECTATIONS`, which bind accepted Story 11.2 records.
- Use the current-change validation route and focused Python tests. Historical evidence-boundary verification and story final-record generation are not applicable to this maintenance action; the completed 11.2 story is not reopened.

- Implementation: frozen rows now produce inventory-drift diagnostics when symlinked or nonregular, and the evaluated row list must equal the frozen list. The graph checker parses solution XML and enforces exact sorted project lists, preserving duplicate detection and existing reusable-Aspire diagnostics.
- Added standalone restoration regressions for identical-byte external and dangling symlinks, missing/nonregular/non-UTF-8 rows, each omitted solution project, duplicate/extra/commented/malformed entries, equivalent XML formatting, and physical project omissions/additions. No accepted fault identifiers or fixture sources changed.
- Baseline: the original fixture suite passed all 68 cases. Before the source fix, the real symlink CLI regression and all four omitted-project regressions failed as expected (5 failures; `/tmp/epic11-a1-red.xml`); their `finally` blocks restored original bytes and checked the passing controls.
- Current-tree controls: the real inventory-only CLI returned PASS/0 with its report at `/tmp/epic11-a1-inventory.json`; a direct graph check returned no blockers and exactly the four frozen solution projects.

- Verification: `python3 -m pytest -q -p no:cacheprovider _bmad/scripts/tests/test_minimal_module_fixture.py --junitxml=/tmp/epic11-a1-fixture.xml` exited 0: 85 passed, zero failures/skips. `python3 -m pytest -q -p no:cacheprovider _bmad/scripts/tests/test_generate_story_record.py -k story_112 --junitxml=/tmp/epic11-a1-record-routes.xml` exited 0: 41 passed, 1,364 deselected, zero failures/skips.
- Preservation: `git diff --exit-code -- tests/fixtures/Hexalith.Conversations.MinimalModule docs/release-evidence artifacts/v9 _bmad-output/implementation-artifacts/epic-11-retro-2026-10-21.md` exited 0. The current CLI still reports exactly 13 files and the original `4fa26d6fa11a365339d9ea39c79e38751340847cf963fe71a82c37062f7df15c` inventory digest. Changed Python sources compile; `git diff --check` passes.
- Scoped historical evidence-boundary gate: not-applicable (routine A-1 maintenance without an evidence-republication requirement). Story final-record generation: not-applicable (no numbered story or new story contract). Accepted Story 11.2/11.3 records remain immutable.

- Independent Blind Hunter review returned four reproducible related gaps. Patches reject symlinks in frozen-path ancestors, count only direct Solution/Folder project entries, handle unsupported XML encodings, and retain discovered physical paths even when an unexpected project cannot be parsed. New standalone cases cover each; no public CLI or accepted evidence format changes.

## Review Triage Log

| Finding | Verdict / route | Evidence and resolution |
| --- | --- | --- |
| BH-1: arbitrary XML containers count as projects | medium / patch | The reviewed `solution.iter("Project")` counted ignored descendants. The parent repeated `dotnet sln <disposable-invalid.slnx> list`: exit 0, “No projects found in the solution.” Restrict traversal to direct Solution projects and direct Folder projects; negative cases cover unknown containers at both levels. |
| BH-2: unknown XML encoding crashes verification | medium / patch | ElementTree raises `LookupError`, outside the reviewed exception handler. Catch unsupported encodings and require the actual CLI to replace a prior PASS report with FAIL/1 and `APPHOST_BOUNDARY_INVALID`. |
| BH-3: malformed extra project bypasses equality diagnostics | medium / patch | `parse_project` raised `Blocked` before comparing physical paths. Retain the discovered path inventory separately, skip unparseable unexpected project bodies, and report the exact-set mismatch. Empty and unsupported-encoding additions have dedicated regressions; malformed frozen projects retain their existing blocked behavior. |
| BH-4: a symlinked fixture directory passes containment | medium / patch | Resolving both the file and fixture root accepted the same outside directory. Reject symlinked ancestors within the repository path. New cases move the fixture root, its upstream tests directory, and a nested project directory outside the repository, then restore all bytes and passing controls. |
