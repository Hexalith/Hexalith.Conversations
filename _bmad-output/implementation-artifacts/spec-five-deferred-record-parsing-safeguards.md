---
title: 'Five deferred record parsing safeguards'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete five open July 30 pass-8 parsing scopes from the deferred-work ledger so valid story inputs remain usable and prose or unsupported YAML cannot become completion authority: quoted frontmatter revisions with trailing comments, UTF-8 BOM frontmatter, root-level sprint output paths, block-scalar skip reasons, and record markers quoted in prose. Verify the changes, append the five resolutions, and push to main as requested.

</frozen-after-approval>

## Implementation Notes

- Entry working tree is clean on main at `92d6c86bf1730a56ddf8eb91506acf0afb091e6b`. The request explicitly selects five deferred items and authorizes the final commit and push. This is one small parsing correction across the generator, its existing tests, this spec and the append-only ledger; no public API, dependencies, data migration or deployment changes are needed.
- Independent investigation reproduced all five defects. Reuse `_bmad/scripts/generate_story_record.py` functions `parse_frontmatter`, `parse_yaml_scalar`, `record_anchor`, historical classification and live output-target calculation. Keep the narrow scalar-only policy: reject unsupported unquoted block indicators, preserve quoted literals and embedded comment characters, and report malformed quoted values with INVALID_SCOPE.
- `_bmad/scripts/tests/test_generate_story_record.py` provides disposable repositories, real CLI invocation and generated-block insertion helpers. Cover positive and negative parsing shapes, actual baseline binding, skipped-test rejection, root/nested sprint allowance and unrelated dirt, and structural versus quoted/fenced markers without rewriting accepted records.
- Use current-change validation and focused Python checks, both Story 7.3 operational guards, whitespace and pinned commitlint. No dotted epic story or effective final-record contract applies; historical evidence-boundary and final-record publication are not-applicable. Preserve all fixture locks, accepted evidence, historical ledger entries and submodule pointers.
- Before the fix, the new parsing selection reported 25 failures and five passing controls (`/tmp/conversations-five-parsing-before.xml`). The initial post-fix historical-positive test omitted its required file_list_commit; reusing the existing generated-record helper corrected that fixture. All 31 focused cases then passed (`/tmp/conversations-five-parsing-focused.xml`). The complete affected v1 selection passes 105 cases (`/tmp/conversations-five-parsing-v1.xml`); exact function selection is retained in `/tmp/conversations-five-parsing-v1-nodes.txt`.
- Both Story 7.3 operational scenarios pass with nonempty ledgers of 33 and 22 assertions. Initial attempts to redirect their outputs were rejected with ARGUMENT_INVALID; the corrected commands use the contract's declared, untracked operational receipt paths. The current indexed root-submodule check and whitespace pass; original ledger bytes remain an unchanged prefix.
- Baseline CI run `39994190851` at `92d6c86bf1730a56ddf8eb91506acf0afb091e6b` fails only `ci / minimal-module platform compatibility`, at `Locked restore and build the platform-backed fixture`; all other executed CI jobs pass. The separate frozen fixture lock-graph failure remains deferred. The raw failed-log request returned no content, so this fresh observation records the failed step rather than claiming a new captured native diagnostic.
- The independent Blind Hunter review found four reachable edge cases; each receives a small patch and regression below. The first post-review parsing run passes all 43 cases (`/tmp/conversations-five-parsing-postreview.xml`). The existing npm tooling lane passes all 92 tests. Final affected selections are rerun after these fixes; earlier counts describe the pre-review version.

## Review Triage Log

- R1 — medium, patch: exact control matching could demote a tampered generated record after whitespace changes. Recognize whitespace-altered markers/header as generated controls and reject noncanonical marker spans. Historical CLI negatives cover leading spaces, trailing spaces and tabs with tampered counts.
- R2 — medium, patch: Markdown fence state consumed YAML block content. Reuse one frontmatter-region reader for parsing and Markdown offsets, preserving BOM and CRLF support; fenced and marker literals in a valid description block cannot hide real anchors.
- R3 — medium, patch: multiline inline-code spans and HTML preformatted examples could still hijack anchors. Exclude matched code spans and HTML literal blocks while retaining offsets and actual generated-record checks. Disposable insertion/historical regressions preserve both example forms.
- R4 — medium, patch: comment-only skip reasons became a nonempty hash-prefixed string. Parse unquoted leading comments as empty and reject blank quoted reasons; retain quoted hash literals. CLI negatives confirm skipped tests cannot use either empty reason shape.
- Self-check — medium, patch: the first heading scanner ignored trailing-space duplicate headings. Normalize structural heading suffix whitespace outside examples and retain FILE_LIST_DRIFT for a duplicate; the added CLI regression passes.
- R3 recheck — medium, patch: the new code-span search could cross an ATX heading and consume a later file-list bullet. Bound it by Markdown paragraph/block boundaries; a CLI regression keeps an unmatched prose tick from hiding the real File List.
- R3 recheck — medium, patch: a negative lookbehind treated even backslash runs as escaping the opening tick. Count the consecutive backslashes modulo two; direct controls cover one through four, and the even-run example also passes live generation, insertion and historical verification.
- The post-recheck parsing selection passes all 49 cases (`/tmp/conversations-five-parsing-review-recheck.xml`). All 60 shared v2 checks pass (`/tmp/conversations-five-parsing-final-v2.xml`), including exact skip-policy positives/negatives and deterministic/malformed/fault routes. The last two patches affect only the v1 Markdown scanner; the final complete v1 selection is rerun below.

## Final Verification

- The independent recheck confirms every review finding closed. It also exercised 16 additional delimiter/backslash combinations with no remaining defect in the two follow-up fixes. No new deferred scope remains from this batch.
- Final affected v1 suite: 123 passed, zero failures/errors/skips, `/tmp/conversations-five-parsing-final-v1.xml`; exact function inventory `/tmp/conversations-five-parsing-final-v1-nodes.txt`. Shared v2 suite: 60 passed, zero failures/errors/skips, `/tmp/conversations-five-parsing-final-v2.xml`. Existing npm tooling lane: 92 passed. Total completed coverage is 275 passing tests, without counting repeated focused runs.
- Both Story 7.3 operational guards and the indexed root-submodule check pass. Whitespace and the append-only ledger check pass. Only the generator, its test module, this spec and the additive ledger resolution are changed; accepted evidence, frozen fixture/lock bytes, lifecycle tracking and gitlinks remain unchanged.
- Pinned commitlint 21.2.3 accepts the exact full commit message with zero problems or warnings; evidence `/tmp/conversations-five-parsing-commitlint.log`. Main has no divergence from origin/main at the final pre-commit fetch. Push to main is explicitly authorized. Historical evidence-boundary and generated final-record publication are not-applicable; the existing baseline fixture locked-restore CI failure remains separately deferred.
