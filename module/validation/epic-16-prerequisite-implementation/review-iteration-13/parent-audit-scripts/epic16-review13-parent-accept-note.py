from pathlib import Path
import json,hashlib
root=Path.cwd();base=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-13';spec=root/'_bmad-output/implementation-artifacts/spec-epic-16-prerequisite-implementation.md';handoff=root/'_bmad-output/implementation-artifacts/epic-16-prerequisite-implementation-handoff-2026-10-25.md';b=spec.read_bytes();frozen=b[b.index(b'<frozen-after-approval'):b.index(b'</frozen-after-approval>')+len(b'</frozen-after-approval>')];assert hashlib.sha256(frozen).hexdigest()=='2502c2f713128efff4d97e0dd3b91c18b076ebff1fc4c6be0fde6de5874a685e'
for i in range(2,6):
 old=(str(i)+'. [ ] ').encode();assert b.count(old)==1;b=b.replace(old,(str(i)+'. [x] ').encode())
note='''

### Parent acceptance audit — iteration 13

The parent accepts all six authorized prerequisite tasks after reviewing the
original-baseline diff, its complete file inventory, the 1,847-line incremental
source/document delta and prior reviewed history. The step-03 diff contains
24,441 sections, including 14,190 untracked files; 11,857 sections are unchanged
from the preceding review. Normal Git output is valid UTF-8 without a binary
rerendering workaround. The original baseline and real index are unchanged.

- Independent audits confirm **3,032 combined**, **72 legacy** and **131 focused**
  passes, with no failures/errors/skips and the same 86-file source snapshot.
  All 2,879 prior combined cases, 72 legacy cases and 19 packaging controls remain.
  There are 153 new combined cases, including 127 iteration-13 cases. All eight
  frozen matrix rows have executed passing coverage. All 71 source bindings and
  three test hashes match current bytes.
- All **12,048 implementation artifacts** and **12,081 nested references** match
  their hashes; the inventory is exact and trackable. All 3,746 prior indexed
  artifacts remain unchanged. Thirty independent parent native/caller controls
  pass. The 23 exact final guard-removal probes produce 53 targeted test failures
  with zero setup errors/skips; every selected case passes in the final combined
  control. Earlier errors, interrupted runs and the one-line expected Git ordering
  correction retain their separate source epochs.
- The final native experiment retains 33 committed files, 28 publication paths,
  11 PEs and five raw TRX/capture pairs with the same nine-product inventory.
  All 43 commit/tree objects and 33 mapped blobs match actual disposable Git
  objects; raw hashes and Git IDs were recomputed. Publication/lifecycle changes
  were derived from retained tree bytes. These fixtures grant no runtime authority.
- Both completion guards and whitespace checks pass. All eighteen actual runtime
  commands remain **BLOCKED / exit2 / ENTRY_QUALIFICATION_PENDING**, and all 54
  runtime paths remain absent. Five protected files and the 18 scenarios, 32
  requirements, 38 nodes and 61 edges are unchanged. The 38 exact disposable
  messages have 390 retained successful pinned-commitlint validations. Runtime
  publication is not-applicable; accepted CLI4.35.2, empty trust, null entry,
  Story16.1 draft/backlog and the ACTIVE hold remain in force.

Evidence: [verification index](../../module/validation/epic-16-prerequisite-implementation/review-iteration-13/verification.json),
[acceptance audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-13/parent-acceptance-audit.json),
[artifact audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-13/parent-artifact-audit.json),
[mutation audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-13/parent-mutation-audit-order.json),
[native audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-13/parent-native-retention-audit-order.json),
[state audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-13/parent-state-audit-order.json),
and [exact audit commands](../../module/validation/epic-16-prerequisite-implementation/review-iteration-13/parent-audit-commands.json).

Independent review and workflow completion follow this acceptance audit. Existing
REFRESH_UV_DRIFT and Story10.4 guidance-binding failures were not rerun or weakened.
Actual Platform qualification and seventeen adoption findings, parity/recovery/
operator/capacity evidence, human SM-2 and owner entry remain outside this scope.
'''
assert '### Parent acceptance audit — iteration 13'.encode() not in b;b+=note.encode();spec.write_bytes(b)
h=handoff.read_text();assert '**Current disposition: iteration-10' in h;h=h.replace('**Current disposition: iteration-10','**Historical disposition: iteration-10',1)
intro='''**Current disposition: iteration-13 prerequisite implementation and parent
acceptance are complete; independent review is pending. Runtime Story16.1 remains
on hold.**

Final verification passed **3,032 combined tests**, **72 legacy tests** and
**131 focused tests** on unchanged source. The parent verified 12,048 retained
artifacts, all eight acceptance-matrix rows and 30 independent controls. The latest
worker and parent audit sections below record the evidence and remaining external
qualification requirements. Earlier observations remain historical.

'''
first=h.index('\n\n')+2;h=h[:first]+intro+h[first:];h+=note;handoff.write_text(h)
record=json.loads((base/'parent-staged-diff-read.json').read_bytes());record.update(allSixTasksAccepted=True,allEightMatrixRowsCovered=True,incrementalSemanticDeltaRead=True,incrementalSemanticDeltaLines=1847,priorReviewedSectionsCarried=11857,finalStep04RewriteWillIncludeNewAuditNotes=True,runtimePublication='not-applicable',runtimeHold='ACTIVE');(base/'parent-diff-acceptance.json').write_text(json.dumps(record,indent=2)+'\n');print('Accepted six prerequisite tasks and recorded parent audits. Frozen intent unchanged.')
