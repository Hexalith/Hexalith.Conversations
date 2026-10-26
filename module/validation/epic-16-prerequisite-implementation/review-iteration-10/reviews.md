# Independent review reports

Reviewed diff: `/tmp/epic16-review-next-l52ia6_4/complete.diff`, SHA-256
`cdc4ec432804cae101ff86f1dcd92dbaf9d48e91913f6e5148fd35fe3f6cc6ac`.
All three context-free reviewers were launched before any result was handled.

## Blind hunter

- Finding floor: 28,290,852 bytes ÷ 1,000 = 28,290.852 kB; N = min(floor(√28,290.852 + 1), 10) = 10.
- In `_bmad/scripts/epic16_products.py:74`, bind each candidate API-comparison DLL to the corresponding `loadedProducts` DLL. Currently, two different binaries stamped with the same revision can independently satisfy these checks, so preservation can be established against a binary the native test did not exercise.
- In `_bmad/scripts/epic16_tooling.py:551`, compare product identities across the five scenarios before generating one final record. Only the candidate commit is compared; mutually different product binaries carrying that revision can collectively produce a passing record.
- In `_bmad/scripts/epic16_native_helper/SignatureNames.cs:48`, retain the referenced assembly’s full identity, particularly its public key/token and culture. Recording only its simple name conflates distinct external types and can conceal changes to public parameter, return, field, and property contracts.
- In `_bmad/scripts/epic16_native_helper/Program.cs:221`, support the SDK informational-version convention already handled by `ProductMetadata.cs`: existing `+metadata` followed by `.<revision>`. The test inspector currently rejects that valid stamp because it requires `+<revision>` directly; the rejection was reproduced.
- In `_bmad/scripts/epic16_tooling.py:335`, require exactly one `TestMethod` and one `Execution` element per definition. A TRX containing additional conflicting method and execution identities currently passes `parse_native()`; this was reproduced with an incorrect second class/method and execution ID.
- In `_bmad/scripts/epic16_native_helper/Program.cs:237`, validate supported constructor dependencies instead of merely counting public constructors. A genuine `[Fact]` class whose sole constructor requires an unprovided `string` passes inspection, despite lacking a runnable xUnit construction path; this acceptance was reproduced.
- In `_bmad/scripts/epic16_tooling.py:395`, supplement Conversations contract-source preservation with compiled public-surface or wire-contract evidence. Unchanged files under the Contracts directory do not establish unchanged public shape when external build properties, generated sources, or dependency types change.
- In `_bmad/scripts/epic16_measurements.py`, extend the AC-16.2-03 query observations to retain the actual public response content. Checking `Forbidden/forbidden` and zero index reads cannot establish the preserved requirement of a non-disclosing response; the current protocol cannot detect leaked cached content accompanying that status.
- In `_bmad/scripts/generate_story_record.py:11110`, narrow the new `except ImportError` to genuinely missing declared dependencies. Unlike the verifier wrappers, it also converts internal import defects into an unavailable-schema result, disguising broken successor code as a prerequisite blocker.
- In `_bmad-output/planning-artifacts/epic-16-tooling-v1/README.md`, correct the claim that entry/trust changes can be committed after the implementation candidate. `git_facts()` requires the current binding and tooling bytes to exist unchanged in that candidate, and normal evaluation also requires the candidate to equal HEAD. Document a workable commit sequence and cover it with an end-to-end test.

## Edge-case hunter

```json
[{"location":"_bmad/scripts/epic16_native_helper/ProductMetadata.cs:109-112","trigger_condition":"A public generic constraint tightens from IFoo? to IFoo.","guard_snippet":"Include Attributes(reader, constraint.GetCustomAttributes()) alongside Names.Name(reader, constraint.Type).","potential_consequence":"API preservation passes although previously valid nullable callers fail compilation with CS8631."}]
```

## Verification gap reviewer

No verification gaps found.
