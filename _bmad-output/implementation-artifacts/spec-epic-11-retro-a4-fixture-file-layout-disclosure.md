---
title: 'Epic 11 A-4: disclose the frozen fixture file-layout exception'
type: 'chore'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The approved 13-file authoring fixture co-locates several C# types despite the normal one-type-per-file convention. Its accepted SM-2 comparison needs an explicit disclosure of that scoped exception and the file-layout sensitivity.

**Approach:** Publish a separate, discoverable disclosure reconciling the frozen Story 11.2 inventory with the shared convention. Explain the five additional files in a hypothetical 18-file layout, the 77.59% versus 68.97% estimate-qualified file reductions, and the unchanged low comparison confidence. Preserve the measured 13-file artifact, accepted evidence/receipts/spec intent, and SM-1; apply the normal convention to future comparable measurements unless explicitly excepted. Close only A-4's tracking/deferral after verification and review.

</frozen-after-approval>

## Implementation Notes

- Route facts: no human-only intent gap, no irreversible action or remote publication, and a bounded documentation change (new disclosure, README link, action tracking and appended resolution). No runtime API, code, dependency, fixture or accepted-record changes.
- Entry HEAD: `649367e451f64b95aa76e61ae11b839fce05be3d`. Preserve the existing unrelated EventStore gitlink change. Read-only deep investigation confirmed the two combined files, the frozen Story 11.2 inventory constraint, Story 11.3 owner decision, and the accepted template digest binding; no skills were read under `references/`.
- Keep `docs/domain-module-authoring-template.md` and frozen fixture README unchanged; link the separate disclosure from the mutable repository README. Treat 18 as a hypothetical file count, with no measured LOC, compiled fixture, new proof or confidence upgrade attributed to it.
- Focused validation will independently recount the accepted inventory/LOC and type declarations, recompute Decimal percentages/threshold, check documentation links and preservation against `/tmp/epic11-a4-preservation.json`, and run the existing three SM-2 threshold boundary cases. No new tests for this documentation-only change. Scoped historical evidence and numbered final-record gates are `not-applicable` under the current-change policy; do not regenerate historical publications.
- Implemented the separate `docs/minimal-module-file-layout-disclosure.md` and a repository README entry. The disclosure names all five additional types, separates measured and hypothetical counts, retains high measurement/low comparison confidence and the 58-file/1,460-LOC estimated reference, explains the unchanged LOC scope, and gives future measurement conventions with a declared sample boundary.
- Read-only independent checks passed: all 13 accepted inventory rows match their declared source hashes, physical/nonblank totals independently recount as 451/396, the two combined files declare exactly the documented seven types, Decimal arithmetic gives 77.59%/68.97% with both above the inclusive threshold, and all new disclosure links and README local links resolve (including heading fragments).
- Preservation check passed for all 180 tracked protected paths against the entry snapshot and Git blobs. No fixture, guidance template, accepted evidence, receipts, original specs, retrospective verdict or dependency changed. The existing three SM-2 threshold cases passed: `uv run --frozen --no-sync python3 -m pytest -q -p no:cacheprovider _bmad/scripts/tests/test_sm2_evidence.py -k inclusive_threshold_before_rounding --junitxml=/tmp/epic11-a4-thresholds.xml` (3 passed, 57 deselected). `git diff --check` passed.
- Independent one-shot Blind Hunter review completed (same model, context-free). Applied all four findings: link directly to the baseline containing the introductory type rule; state that only the measured layout is normalized and the estimated reference's convention is unverified; explain the four generated locks excluded from the 13 authored files; and record the read-only reproduction procedure below. No review work is deferred.
- Post-review read-only checks passed against the canonical entry revision without relying on the temporary snapshot. Commit message validation passed: `node_modules/.bin/commitlint --config commitlint.config.mjs --edit /tmp/epic11-a4-commit-message.txt --verbose` (0 problems, 0 warnings; `/tmp/epic11-a4-commitlint.log`).

Reproduce the independent recount, percentages, link checks and 180-path preservation check from the repository root using Python 3 and Git; this reads source/evidence only and needs no `/tmp` snapshot:

```bash
python3 - <<'PY'
from pathlib import Path
from decimal import Decimal, ROUND_HALF_UP
import hashlib, json, re, subprocess

root = Path.cwd()
entry = '649367e451f64b95aa76e61ae11b839fce05be3d'
scopes = ['tests/fixtures/Hexalith.Conversations.MinimalModule', 'docs/release-evidence',
          'artifacts/v9/11.1', 'artifacts/v9/11.2', 'artifacts/v9/11.3',
          'docs/domain-module-authoring-template.md',
          '_bmad-output/implementation-artifacts/spec-11-1-correct-and-validate-platform-hosted-thin-module-authoring-guidance.md',
          '_bmad-output/implementation-artifacts/spec-11-2-build-the-reproducible-minimal-module-fixture-against-live-platform-apis.md',
          '_bmad-output/implementation-artifacts/spec-11-3-generate-authoritative-sm-2-v2-evidence-and-decide-oq-2.md',
          '_bmad-output/implementation-artifacts/epic-11-retro-2026-10-21.md']
subprocess.run(['git', 'diff', '--exit-code', entry, '--', *scopes], check=True)
paths = subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', entry, '--', *scopes], text=True).splitlines()
assert len(paths) == 180
for path in paths:
    assert Path(path).read_bytes() == subprocess.check_output(['git', 'show', entry + ':' + path]), path
record = json.loads(Path('docs/release-evidence/minimal-module-authoring-cost-sm2-v2.json').read_text())
rows = record['measurement']['paths']
assert len(rows) == len({row['path'] for row in rows}) == 13
physical = nonblank = 0
for row in rows:
    data = Path(row['path']).read_bytes()
    lines = data.decode('utf-8').splitlines()
    assert hashlib.sha256(data).hexdigest() == row['sha256']
    assert len(lines) == row['physicalLoc']
    assert sum(bool(line.strip()) for line in lines) == row['nonblankLoc']
    physical += len(lines)
    nonblank += sum(bool(line.strip()) for line in lines)
assert (physical, nonblank) == (451, 396)
fixture = Path(record['measurement']['root'])
tracked = subprocess.check_output(['git', 'ls-files', str(fixture)], text=True).splitlines()
assert len(tracked) == 17 and sum(Path(path).name == 'packages.lock.json' for path in tracked) == 4
server = fixture / 'src/Hexalith.Conversations.MinimalModule.Server'
for filename, expected in [
    ('ConversationModule.cs', ['ConversationModule', 'ConversationProjection', 'ConversationTitleQuery', 'ConversationObservations', 'ConversationCreatedObserver']),
    ('ConversationAggregate.cs', ['ConversationAggregate', 'ConversationState'])]:
    assert re.findall(r'^public (?:sealed |static )?class (\w+)', (server / filename).read_text(), re.M) == expected
assert record['comparison']['referenceFileCount'] == 58
for count, expected in [(13, '77.59'), (18, '68.97')]:
    raw = (Decimal(58) - count) / Decimal(58) * 100
    assert str(raw.quantize(Decimal('.01'), rounding=ROUND_HALF_UP)) == expected and raw >= 50
for filename in ['docs/minimal-module-file-layout-disclosure.md', 'README.md']:
    for _, url in re.findall(r'(?<!!)\[([^\]]+)\]\(([^)]+)\)', Path(filename).read_text()):
        if '://' in url or url.startswith(('mailto:', '#')):
            continue
        link, _, anchor = url.partition('#')
        target = (Path(filename).parent / link).resolve()
        assert target.is_relative_to(root) and target.exists(), (filename, url)
        if anchor and target.suffix == '.md':
            headings = re.findall(r'^#{1,6}\s+(.+)$', target.read_text(), re.M)
            assert anchor in [re.sub(r'[^\w\- ]', '', heading.lower()).replace(' ', '-') for heading in headings], url
print('PASS: 180 protected paths; 13 authored + 4 generated files; 451/396 LOC; 5 extra types; 77.59%/68.97%; local links')
PY
```

Expected: exit 0 with `PASS: 180 protected paths; 13 authored + 4 generated files; 451/396 LOC; 5 extra types; 77.59%/68.97%; local links`. The exact existing threshold test command above independently checks 28/29/30 files around the inclusive boundary.

## Review Triage Log

- BH-1 — low, patch: the fragment pointed below the actual one-type rule. Removed the fragment so the baseline's introductory rule is visible at the linked document.
- BH-2 — medium, patch: the hypothetical row could be overread as normalizing both sides. Explicitly state that only the measured layout varies, the 58-file estimate is fixed, and its type-to-file convention cannot be verified from a reconstructed source tree.
- BH-3 — low, patch: the 13 authored files could be mistaken for the complete tracked fixture. State the complete 17-file boundary and exclusion of four generated lock files from authored file/LOC totals.
- BH-4 — low, patch: the original notes relied on an ephemeral preservation snapshot and unspecified checking code. Appended exact read-only Python/Git commands to the agent-owned notes; the frozen intent is unchanged. Reproduction succeeded with no snapshot dependency.
