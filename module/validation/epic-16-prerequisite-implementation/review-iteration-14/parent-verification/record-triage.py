from pathlib import Path
import hashlib,json
root=Path.cwd();p=root/'_bmad-output/implementation-artifacts/spec-epic-16-prerequisite-implementation.md'
s=p.read_text();start=s.index('<frozen-after-approval');end=s.index('</frozen-after-approval>')+len('</frozen-after-approval>');frozen=hashlib.sha256(s[start:end].encode()).hexdigest()
block='''### Review iteration 14 triage — patches to iteration 13

All three final reports were received before individual classification. Raw reports
and independent parent reproductions are retained under
`module/validation/epic-16-prerequisite-implementation/review-iteration-14/`.
The verification reviewer filed an Other finding, so it was independently verified.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R14B1 | high | patch | Parent inspected the exact compiled varargs Fact successfully, then ran the same bytes with pinned xUnit: exit 1 with NotSupportedException. IsTestSignature omits the default calling-convention check; one comparison rejects this demonstrated shape without changing supported ordinary methods. |
| R14B2 | high | patch | Parent inspection accepts the source-authored optional ref-struct constructor, while the pinned runner exits 1 because it cannot box a byref-like value. Reuse NonNullableValueType in the existing direct value-type default path. |
| R14B3 | medium | patch | A one-byte NestedClass parent mutation makes a valid nested type its own parent. Parent's current fixed helper accepts the original but aborts with stack overflow on the mutant; Python classifies that exact malformed evidence as HelperUnavailable. Track visited definition handles in the existing recursive name walk and reject the demonstrated cycle as invalid evidence. |
| R14B4 | false | reject | On clarification the reviewer explicitly withdrew this correctness claim: no mismatched binary was demonstrated through the qualified runtime path and no independent-build-authentication promise was identified. products.loaded correlates retained native bytes, MVIDs, owners and revision metadata; deliberately synthetic test stand-ins do not demonstrate a breach of that contract. Independent build authentication is a separate optional enhancement, not a reproduced admission defect. |
| R14B5 | medium | patch | Parent changes a valid authorization observation: the direct predicate explains that authorization summaries must derive from actual read/access inputs, but the full evaluator returns only MEASUREMENT_INVALID and the scenario. All 676 require-call explanations are source literals. Preserve those controlled predicate explanations through a dedicated internal ValueError subtype; leave unrelated/parser exceptions generic so untrusted values are not exposed. |
| R14B6 | medium | patch | On the same passing disposable fixture, the real generator CLI succeeds with the exact explicit pair but returns OUTPUT_FORBIDDEN with both optional flags omitted. Pass None when both are absent so generate uses its existing exact destinations; retain the existing rejection of a partial pair. |
| R14B7 | low | reject | The scoped exception does expose build/cache paths under this evidence-only directory, but current archives deliberately retain those exact native compilation bytes and their complete inventories were audited. No unrelated staged file or incorrect admission is demonstrated. Future accidental files in this dedicated tree are an occasional hygiene risk; an allowlist/archive migration adds maintenance beyond a direct correction and would disturb immutable evidence retention. No staging occurs in this task. |
| R14B8 | false | reject (carried) | carried R13B9/R12B10: same expanded schema representation and hypothetical future divergence, with no new inconsistent definition or incorrect caller admission. Preserve the prior verdict; no re-verification or schema refactor. |
| R14B9 | low | reject | The 4,957-line test file makes occasional navigation harder, but the suggested split identifies no independent failing assertion or specific uncovered behavior beyond the separately verified B1/B2 defects. Reorganizing fixture modules and all historical source bindings is more than a direct correction for this cosmetic maintenance cost. Existing assertions and identities remain intact. |
| R14B10 | medium | patch | The earlier producer paragraph still says runtime capture version 1 is required, while the effective parser/policy and later paragraph require version 2. Correct that primary paragraph and label its older fixture facts historical; link the current version-2 producer requirements rather than requiring readers to reconcile contradictory instructions. |
| R14E1 | high | patch | Parent independently repeats the source-authored optional local ref-struct case: metadata inspection succeeds and pinned xUnit fails before executing a test. The direct default path lacks the existing byref-like exclusion. |
| R14V1 | high | patch | Parent independently compiles/inspects the reviewer's optional and explicit-default ref-struct fixtures and reruns both exact binaries. Both are admitted by the current helper and both runners exit 1 with NotSupportedException; the direct default path needs the same value-type eligibility check already used for nullable arguments. |

Only after those individual verdicts, R14B2/R14E1/R14V1 are grouped as the
same missing direct byref-like exclusion. B1, B3, B5, B6 and B10 remain separate
roots. Every surviving fix is local, adds no public contract surface, and guards
only a demonstrated state or corrects the existing producer documentation.
They route to patch, with no intent/spec loopback, no counter increment and no
new deferred entry. The frozen authorization, prior evidence, user files,
actual entry/trust state and runtime hold remain unchanged.

'''
assert '### Review iteration 14 triage' not in s
s=s.replace('## Verification\n',block+'## Verification\n',1);p.write_text(s)
assert hashlib.sha256(s[start:end].encode()).hexdigest()==frozen
r=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-14/reviews.md'
with r.open('a') as f:f.write('''
## Blind reviewer clarification — B4

B4 was not demonstrated through the qualified runtime path. I observed that `products.loaded()` correlates retained bytes, names, MVIDs, source-revision metadata and required types; I did not establish that the intended contract promises independent build authentication.

The synthetic fixtures are not evidence of a defect in that contract. Treat B4 as an optional provenance enhancement, not a verified finding or reason to continue the implementation loop. I withdraw it as a correctness finding.
''')
print('Recorded 12 individual verdicts; 6 patch roots; no loopback or deferred entry.')
