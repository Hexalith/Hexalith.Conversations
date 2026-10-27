# Review correction observations — 2026-10-27

These additive observations retain the preceding implementation's reports and
raw bytes. The correction pass ran only the edited mapping class and the two
edited projection classes; full parent verification remains pending.

- Mapping tests: **10/10 PASS**, including missing/partial producer imports in source and CI/package modes,
  exact source/package selections, real isolated `../Hexalith.*` siblings,
  copied `global.json`, and matching repository SDK selection.
- Projection tests: **41/41 PASS**, with zero errors, failures, skips or not-run cases. They cover
  summaryless pending siblings after a configured SDK rebuild batch commits,
  fail-closed reads before configured recovery with another component already
  holding interrupted SDK state, both public writer invalidation paths, safe
  terminal compensation/refusal, and unchanged populated alternate components.
- Data Protection creates and persists actual SDK key-ring material through the
  SDK fake Dapr client with a two-second operation timeout, encodes/decodes a
  real cursor, checks its declared purpose and wrong-purpose rejection, and
  reloads that key ring in a second isolated provider. Configured component
  contents are asserted; default/alternate components are untouched. This is
  source-fake behavior, not live delivery or process/provider restart qualification.

The first projection correction run's one failed test expectation is retained
in `xunit-projections.xml`: a completed same-generation dispatch marker correctly
stays completed. The corrected test uses a bounded injected index-CAS failure
after `PersistAsync` durably advances detail, leaving actual pending state for
terminal-refusal assertions. Production algorithms were not changed.

The final command records and raw outputs are
`../xunit-review1-mapping.*` and `../xunit-review1-projections-verified.*`;
individual mapped Debug build records are `../build-review1-contracts.*` and
`../build-review1-terminal.*`. The complete implementation-source digest snapshot
is [implementation-source-digests.json](implementation-source-digests.json):
**801 files**, including all three untracked C# additions. It is separate
from the retained tracked-only native scanner observation. No file was staged,
and scanner scope was not changed.

The `.gitattributes` rule scopes exact raw bytes to this observation directory;
[raw-byte-attributes.txt](raw-byte-attributes.txt) confirms `text: unset` for raw
XML and JSON. That additive rule changes the accepted tooling-source input hash:
[pending-tooling-source-binding-refresh.json](pending-tooling-source-binding-refresh.json)
records the mismatch and pending owner refresh. Accepted historical packages and
bindings, pins, gitlinks, entry/trust and the runtime hold remain untouched.
