# Observation-attribute placement correction — 2026-10-27

The raw-byte rule now lives in the observation directory's own
[`../.gitattributes`](../.gitattributes), with the same effective protection for
root-level and nested raw observations. Only our previously added root block was
removed. Root `.gitattributes` is byte-identical to its committed and accepted
binding input, SHA-256 `d743202ab8c0bdf3dcd000a4a101acde352b6e6cf305a49b708b23c6e7029d7d`. No accepted binding,
validator, pin, package or scanner scope was changed.

All **six actual Story 16.1 contract commands** were rerun from the real checkout:
each returned **BLOCKED / exit 2 / ENTRY_QUALIFICATION_PENDING**. Their exact
commands, working directory, timing and raw-output hashes are in
[story-commands.json](story-commands.json), with separate same-stem raw stdout and
stderr files. No C# compilation, C# test or broad verification lane ran in this
correction pass.

[effective-raw-attributes.json](effective-raw-attributes.json) verifies `text:
unset`, `diff: set`, and `whitespace: unset` for observation-root JSON/text,
review-fix-1 nested XML, parent-final raw stdout, and this correction's nested
JSON. The local rule preserves the raw bytes without modifying the bound root.

[correction.json](correction.json) records the final checks:

- All **270** prior observation files remain byte-identical, including
  `review-fix-1` and `parent-final`. The latter's FAIL/exit 1/SOURCE_DRIFT
  observation remains a truthful record of the earlier root-rule state.
- All **755** C# source files captured at correction entry are unchanged.
- All accepted binding/package files captured at entry are unchanged.
- The fresh [implementation-source snapshot](implementation-source-digests.json)
  covers **802** implementation and policy files, including the three
  untracked C# additions and the local attributes file. It is separate from the
  native tracked-only scanner inventory and grants no tracked-candidate approval.
- Entry remains null, trust empty, hold ACTIVE, and no runtime receipt/final pair
  was written.

The review-fix-1 pending-root-binding-refresh note is retained byte-for-byte as a
historical observation. This correction resolves that specific root drift; an
attribute-rule binding refresh is no longer required. Current module guidance
now reflects this disposition. CI mapping, native excluded findings, owner/live
qualification and runtime-entry gates remain unchanged.
