# Independent review 14 — raw reports

All three reports returned before classification or implementation changes. Reviewed diff: `/tmp/epic16-review-next-1ry5kka_/complete.diff`, 315,005,183 bytes, SHA-256 `bb314c90b02c1f577f0616ff95000446fc39636db3ec50d9ffe907419ca2598b`.

## Blind hunter

- Floor: 315,005,183 bytes ÷ 1,000 = 315,005.183 kB; N = min(floor(√315,005.183 + 1), 10) = 10.

- [Program.cs:249](/home/administrator/projects/hexalith/conversations/_bmad/scripts/epic16_native_helper/Program.cs:249) accepts varargs test methods. `[Fact] public void Scenario01(__arglist) {}` passes inspection, but the pinned xUnit runner fails with `NotSupportedException`. Require the supported calling convention explicitly. Exact source, binaries, commands, runner streams and inspector results are retained in [/tmp/epic16-blind-vararg-6rskk2b5](/tmp/epic16-blind-vararg-6rskk2b5/manifest.json).

- [TestConstruction.cs:298](/home/administrator/projects/hexalith/conversations/_bmad/scripts/epic16_native_helper/TestConstruction.cs:298) admits optional local `ref struct` constructor parameters. Inspection passes, but xUnit reports “Cannot create boxed ByRef-like values” and executes zero tests. Apply the existing byref-like exclusion to ordinary default/optional arguments as well as nullable arguments. Reproduction: [/tmp/epic16-blind-reflike-gv_lmeot](/tmp/epic16-blind-reflike-gv_lmeot/manifest.json).

- [SignatureNames.cs:34](/home/administrator/projects/hexalith/conversations/_bmad/scripts/epic16_native_helper/SignatureNames.cs:34) recursively follows nested-type relationships without detecting cycles. Changing one `NestedClass` parent to itself crashes the helper with a stack overflow; the Python wrapper then classifies malformed evidence as an unavailable prerequisite. Add cycle/depth guards that return invalid evidence. Original and mutated binaries, mutation details, helper inputs and streams are retained in [/tmp/epic16-blind-cycle-g134dbww](/tmp/epic16-blind-cycle-g134dbww/manifest.json).

- [epic16_products.py:61](/home/administrator/projects/hexalith/conversations/_bmad/scripts/epic16_products.py:61) establishes product/source association through assembly names, declared types and self-authored revision stamps. It does not verify that retained DLLs were built from the owning committed sources. Add independently verified build provenance or reproducible artifact comparison before treating these bindings as proof of the actual implementation.

- [epic16_tooling.py:506](/home/administrator/projects/hexalith/conversations/_bmad/scripts/epic16_tooling.py:506) discards every semantic validation explanation and reports only `MEASUREMENT_INVALID` plus the scenario. Preserve a safe assertion identifier or controlled explanation so operators can identify the failed relationship without debugging hundreds of predicates.

- [epic16_tooling.py:666](/home/administrator/projects/hexalith/conversations/_bmad/scripts/epic16_tooling.py:666) declares generator output options optional, then always passes `[None, None]` when omitted, producing `OUTPUT_FORBIDDEN` instead of using `generate()`’s default destinations. Either require both options for generation or pass `paths=None` when both are absent; reject partially specified pairs explicitly.

- [.gitignore:467](/home/administrator/projects/hexalith/conversations/.gitignore:467) unignores every descendant of the validation directory, including otherwise ignored build products and caches. The diff already contains 1,886 `bin`/`obj` paths. Narrow retention to an explicit evidence inventory or controlled archives so subsequent staging does not silently absorb unrelated generated files.

- [_bmad/schemas/epic-16-tooling-v1.schema.json](/home/administrator/projects/hexalith/conversations/_bmad/schemas/epic-16-tooling-v1.schema.json) expands shared capture structures into a 36,023-line schema. Extract repeated snapshot, identity, operation and transaction shapes into shared `$defs`; maintaining separate copies makes it easy for equivalent evidence paths to acquire inconsistent validation.

- [test_epic16_tooling.py](/home/administrator/projects/hexalith/conversations/_bmad/scripts/tests/test_epic16_tooling.py) mixes nearly 5,000 lines of protocol tests, compiler fixtures, metadata mutation utilities and historical review rounds. Separate shared native-fixture utilities and organize tests by supported behavior, retaining existing assertions. This would make gaps such as calling conventions and constructor argument constraints easier to locate.

- [README.md:367](/home/administrator/projects/hexalith/conversations/_bmad-output/planning-artifacts/epic-16-tooling-v1/README.md:367) presents version-1 captures as a current requirement before explaining much later that version 2 supersedes them. Consolidate the current protocol into one normative section and clearly separate historical amendments; implementers should not need to reconcile 751 lines of review history to construct valid evidence.

## Edge-case hunter

```json
[
  {
    "location": "_bmad/scripts/epic16_native_helper/TestConstruction.cs:306-313",
    "trigger_condition": "An optional constructor parameter is a local ref struct.",
    "guard_snippet": "if (value && !NonNullableValueType(reader, type)) return false;",
    "potential_consequence": "Unconstructible test assemblies pass metadata admission; the actual xUnit runner exits before executing tests."
  }
]
```

## Verification-gap reviewer

### Other findings

- `_bmad/scripts/epic16_native_helper/TestConstruction.cs:313` admits direct `ref struct` constructor defaults that the pinned xUnit runner cannot supply. `SupportedDefaultType` accepts any local nongeneric value type without checking `IsByRefLikeAttribute`; that check currently exists only in the nullable-argument path. I compiled two fixtures declaring `public ref struct Restricted {}` and constructors accepting either `[Optional] Restricted value` or `Restricted value = default`. `inspect_assembly` accepted both, but xUnit 4.0.2 exited **1** with `System.NotSupportedException: Cannot create boxed ByRef-like values.` Thus the native admission check accepts an unsupported constructor. The default cases at `_bmad/scripts/tests/test_epic16_tooling.py:4588` omit direct byref-like parameters, while the controls at line 4924 cover only `Nullable<ref struct>`. Reject byref-like types on both direct default paths and extend the existing runner-comparison tests. Compiled fixtures, exact commands, runner streams and TRX results are retained in `/tmp/review14-refstruct-h7_v5woq/{optional,null}/`; each `summary.json` records successful inspection alongside runner exit 1.

## Blind reviewer clarification — B4

B4 was not demonstrated through the qualified runtime path. I observed that `products.loaded()` correlates retained bytes, names, MVIDs, source-revision metadata and required types; I did not establish that the intended contract promises independent build authentication.

The synthetic fixtures are not evidence of a defect in that contract. Treat B4 as an optional provenance enhancement, not a verified finding or reason to continue the implementation loop. I withdraw it as a correctness finding.
