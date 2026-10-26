from pathlib import Path
import json
root=Path.cwd();review=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12';v=json.loads((review/'verification.json').read_bytes());commands=json.loads((review/'implementation/test-commands.json').read_bytes())
def elapsed(name):
 import re
 return float(re.search(r'\d+ passed.* in ([0-9.]+)s', (review/'implementation'/name/'stdout').read_text()).group(1))
text=f'''
### Re-derived implementation verification — review iteration 12

All authorized prerequisite changes are implemented and verified; parent review and
completion remain separate. Exactly 42 owned files were hash-verified before
restoration. All five protected inputs, all 7,954 preexisting evidence files seen
at restoration, and the 2,854 prior indexed implementation artifacts remain
unchanged. The original combined run retained an 86-file source snapshot with 71
bound paths, including the Commons 2025 MIT notice and copied-method attribution.
A concurrent user/Cursor /pushall commit changed the evidence whitespace attribute
during that run; its sourceUnchanged=false result is preserved. Current packaging
and legacy checks use a separate stable 86-file snapshot, described below.

- Each of the four reverse-save experiments now records executed and completed
  saves against one durable state: complete native replay proposals, original CAS
  expectation, newer commit, rejected stale write, and complete preserved contents.
  Both distinct replica processes perform correlated final reads and derive the
  decision from that same committed snapshot. Existing horizon, pending and
  recovery experiments remain independent.
- Both six-state tenant-authorization captures retain actual request/read/response
  operations, native projection/checkpoint and source fingerprints, persisted
  freshness, gap/conflict/provider causes, and honest absent metadata. Responses
  derive from native role/status/permission inputs. The 100-tick freshness limit
  is an independently pinned test control, not a production SLO. Native Unknown
  role yields InsufficientRole. The fixed .NET helper applies the retained Commons
  tenant/principal input methods before admitting a read. Compiled actual Commons
  parity controls cover invalid zero-read inputs and legitimate Unicode; unrelated
  opaque identifiers, nullable descriptions and open native maps remain supported.
- Provider-failure and retry-exhaustion captures record one or three actual failed
  attempts, source-derived proposed state, correlated provider fault/restoration,
  terminal processing and the observed empty acknowledgement inventory. The
  complete durable contents before and after restoration must remain equal. All
  successful durable outcomes and duplicate zero-write semantics are preserved.
- Both test and product source stamps require genuine approved framework ownership,
  exact constructor signatures and complete blobs. Task/ValueTask and ParamArray
  recognition use the same real identity boundary. Genuine SDK stamps and supported
  constructors still pass. The unsigned custom Task passes the actual pinned runner
  but is rejected as an unsupported framework async return; the foreign ParamArray
  is rejected by both inspector and runner. The original local product-stamp probe
  remains a recorded rejection; the parent's distinct external product-stamp
  counterexample is corrected. No general compatibility claim is introduced.
- Catch-up enqueue transactions cannot reuse lifecycle stage commit identities;
  legitimate work/drain sharing remains valid. SDK/build prerequisite failures
  retain bounded stdout/stderr tails and the real exit code while preserving
  HelperUnavailable and structured BLOCKED results.
- Original combined verification: **{v['combined']['counts']['tests']:,} passed** in
  **{elapsed('combined'):.2f}s**, preserving all 2,416 prior cases and adding
  {v['combined']['newSuccessorTests']} cases ({v['combined']['successorTests']:,}
  successor and 162 historical adoption-v2). Current packaging-epoch legacy verification:
  **72 passed** in **{elapsed('legacy'):.2f}s**. These combined, packaging and legacy suites have zero
  assertion failures/errors/skips. The original wrapper exited 1 because the
  post-run freeze check detected .gitattributes drift; it did not start legacy.
  Exact original commands, streams, JUnit, source archive and sourceUnchanged=false
  remain intact. The full combined run was not repeated or relabelled as current.
- Concurrent user commit `46ca7bf39b25318418b7145b7ce75bb306471cd0` preserved current
  work, added only `-whitespace` to the prerequisite evidence attribute rule, and
  advanced Builds, EventStore, Folders, FrontComposer, McpCli, Memories, Parties,
  Platform, Projects and Tenants gitlinks. The worker preserved those bytes and
  commits. The original baseline remains unchanged. `-text` and `diff` remain
  intact; the new flag suppresses whitespace diagnostics for retained evidence
  without changing text conversion, checkout bytes or executed code. Only that
  attribute's bound SHA was refreshed. Exactly two frozen paths differ from the
  original archive: .gitattributes and its binding hash; all code/tests and the
  other 84 paths are unchanged.
- Current packaging verification: **{v['packaging']['counts']['tests']} passed** in
  **{elapsed('packaging'):.2f}s**, plus the **72 legacy passes** above. The exact
  selector in packaging/command.json covers binding inventory/pins, executing-source
  rejection, atomic publication, genuine fresh-checkout native retention, binary
  Git diff behavior and autocrlf checkout preservation. Both current runs report
  sourceUnchanged=true against packaging-source.json. The original final-source.json
  remains separate. Current HEAD/gitlinks and exact old/new binding bytes are
  retained in packaging-epoch/. Original inventory final-source.json has SHA-256
  `ed4e8a100abdd4ca22f2cc5df225d69da6275ccde4b4e9e4e865c4a1a6c27573`;
  current inventory packaging-source.json has SHA-256
  `68ad9ad781fd6aebffe1a7c7042ea054a0d3b533cbd9ad1b8bd19b4ebe5ad22d`.
  No full combined run on this packaging epoch is claimed.
- **39 distinct guards across 42 mutation runs** produce **199 targeted assertion
  failures with zero setup errors/skips**. Three Program.cs guards were repeated
  after the notice-only correction, giving every guard a mutation against final
  exact target bytes. Every failed mutation case passes in the combined run; its
  code/test bytes are identical in the current packaging epoch. All patches, original/mutated bytes, command records and source epochs are retained.
- Exploratory evidence remains honest: the first run had 428 passes and two old
  nullable-description positives whose new full reverse snapshots needed matching
  refreshes. Subsequent selected runs passed 100, 21, 12, 34 and six cases. The
  21-pass epoch had the subsequently corrected Unknown-role expectation; it is not
  presented as final native parity. The wrong converter source-path lookup was a
  preparation error, not a test failure. The combined assertion results supersede
  those exploratory selections; the separate current packaging checks do not
  erase the observed source drift.
- Genuine pinned xUnit execution retains untouched raw TRX and seven synthetic
  product stand-ins. Fresh-checkout regeneration/insertion passes after original
  build/measurement/payload locations disappear. Each original/current packaging
  experiment retains its own 31 exact committed artifacts, including 26 publication paths, nine PE binaries and five captures
  with the same seven-product inventory. Helper provenance confirms SDK 10.0.402
  and .NET 10.0.13. These fixtures confer no runtime or owner authority.
- All eighteen actual runtime/final-record commands remain **BLOCKED / exit 2 /
  ENTRY_QUALIFICATION_PENDING**. All 54 runtime paths remain absent; both exact
  Story 7.3 guards and git diff --check pass, with operational bytes restored.
  The 18 scenarios, 32 requirements, 38 graph nodes, 61 edges, rollback obligations
  and predecessor pins are preserved. Entry remains null, trust empty, accepted
  CLI 4.35.2, Story 16.1 draft/backlog and hold ACTIVE.
- Evidence is indexed by
  `module/validation/epic-16-prerequisite-implementation/review-iteration-12/verification.json`.
  Exact disposable commit messages passed pinned commitlint before use. The first
  batched read included bmad-build/SKILL.md before reaching the spec-only handoff
  instruction; this is explicitly recorded. No skill was rendered/invoked, no
  secondary workflow, child agent or completion orchestration ran. This worker
  performed no actual repository staging/commit/push, dependency update, runtime application start or runtime publication. Concurrent user Git
  activity is attributed separately above.
- The prohibited broad lane was not rerun. REFRESH_UV_DRIFT and Story 10.4 guidance
  binding remain unresolved and unweakened. Actual Platform integration
  qualification, seventeen adoption findings, provider/environment parity, recovery
  drills, operator/deputy coverage, capacity/waiver decisions, human SM-2 and separate
  owner entry remain outside this prerequisite implementation.
'''
for path in ('_bmad-output/implementation-artifacts/spec-epic-16-prerequisite-implementation.md','_bmad-output/implementation-artifacts/epic-16-prerequisite-implementation-handoff-2026-10-25.md'):
 p=root/path;assert '### Re-derived implementation verification — review iteration 12' not in p.read_text();p.write_text(p.read_text().rstrip()+'\n'+text)
print('Appended observed iteration-12 verification to spec and handoff')
