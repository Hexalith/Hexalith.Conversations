# V9 Execution Overlay — Reality-Checked Decision Review

**Reviewed artifact:** `_bmad-output/planning-artifacts/architecture.md`,
`ARCHITECTURE-EXECUTION-OVERLAY-V9` block at lines 1802–2027  
**Lens:** Verify that committed decisions and named-technology/current-state
claims are grounded in the approved proposal, the repository, or the required
Hexalith baseline; flag training-data assertions and potentially stale claims.  
**Review date:** 2026-08-02  
**Verdict:** **CONDITIONAL PASS — the execution-only decisions are grounded and
no new technology/version claim depends on training data, but current machine
readers do not yet implement the overlay-discovery rule and the preserved v1
view is already drifted. The global implementation hold correctly remains
active.**

## Findings

### F-01 — HIGH — Overlay-marker authority discovery is a future contract, not current repository behavior

The overlay says machine readers determine current authority from the last
complete architecture overlay marker (lines 1835–1839). That is a defensible
new rule for an append-only artifact, and the approved proposal authorizes v9
identity and cross-artifact validator work. It is not, however, a description
of the repository's current behavior:

- `ArchitecturePlanningAuthorityValidationTest.cs:28,191-208` reads the YAML
  frontmatter and requires `conversations-architecture-2026-08-01-v8` plus the
  v1 view.
- `PlanningAuthorityV8ValidationTest.cs:29-32,85-101` binds the v8 identities
  and compares v1 to the whole architecture file.
- `_bmad/scripts/generate_epic_6_current_execution_view.py:15-23,130-142`
  hard-codes v8/v1 and hashes the whole architecture document.
- No other repository file implements or consumes the v9 architecture begin/end
  marker.

This is not a technology-staleness problem; it is a reality boundary between a
normative architecture decision and implemented behavior. Treat the marker rule
as prospective until the approved v9 companion validator exists and passes.
The overlay already prevents misuse through `publication-candidate=UNBOUND`,
the explicit companion-publication requirement, and the implementation hold.
It must not be represented elsewhere as an already-operational current-authority
resolver.

### F-02 — HIGH — The preserved v1 execution-view binding is currently drifted

The overlay correctly anticipates this possibility and requires the future v9
validator to report a historical mismatch as a blocker rather than rewriting
v1 (lines 1850–1856). The mismatch is present now:

- The marker's `v8-prefix-sha256` is
  `7fd33168f34bb7d3326b4abb0eb79999270c11fefc7f50ec3acdd62fb1b86df5`,
  and hashing lines 1–1801 reproduces that value exactly.
- The immutable v1 view records
  `source_architecture_sha256=ced930531c6b0638dbf8253a0c766a146c66748f2f2ee13f64f4259ef9b667eb`.
- `python3 _bmad/scripts/generate_epic_6_current_execution_view.py --check`
  exits `1` with `EPIC_6_CURRENT_VIEW_DRIFT` against the current artifact.

This finding does not invalidate the overlay's reality claims: the overlay says
the mismatch must block, and the live evidence confirms that blocker. It does
mean v9 publication and IR-0 cannot be claimed complete until the new
marker-bounded historical proof and separately candidate-bound v2 validation
exist and pass.

## Decision Grounding

| Overlay commitment | Reality check | Result |
| --- | --- | --- |
| Replace only unfinished v8 execution authority with Epics 7–15 and Gates IR-0/RG-15 | Approved proposal sections 4.2, 4.3, and 4.6 define this exact execution-only correction and mapping. | Grounded |
| Preserve v8 technical invariants and the global hold | Approved proposal lines 281–290 and SC-01/SC-12 require unchanged system design and a candidate-matched readiness gate. The prior v8 architecture contains the inherited rules. | Grounded |
| Keep the publication candidate `UNBOUND` during the architecture-only step | Workspace search finds no v9 epic overlay, v2 execution view, supersession map, successor-story publication, or v9 validator. The approved publication sequence requires those artifacts and digests before IR-0. | Grounded live-state claim |
| Pin the preserved prefix by SHA-256 | Independent hash of architecture lines 1–1801 exactly matches the marker value. | Verified |
| Keep EventStore as the write-side/durable state authority | The existing v8 architecture and `_bmad-output/project-context.md` state this rule; the required Hexalith baseline identifies Hexalith.EventStore as the domain persistence path. | Grounded; no new technology choice |
| Retain the existing Conversations AppHost only as a non-shipping test fixture | `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` exists and declares `IsPackable=false` and `IsPublishable=false`; v8 and the approved v9 proposal explicitly preserve the existing test-harness limit. The repository baseline's general no-domain-AppHost rule remains a separately recorded policy tension, not an invented v9 technology claim or authority to expand the exception. | Grounded exception; no expansion authorized |
| Preserve SM-C2, requirement, and UX denominators | The approved proposal fixes 124/124 FR coverage, 52 UX decisions, 28 UX acceptance IDs, and the unchanged performance authority; the overlay repeats those planning invariants without choosing a new library or measurement technology. | Grounded |
| Use topological predecessor rules, atomic AC IDs, SHA-256 bindings, exact commands, and fail-closed results | Approved proposal sections 4.2, 4.4, and 4.10 specify these contracts directly. | Grounded |
| Publish IR-0 unchanged and outcome-neutral | Inherited v8 readiness rules plus the approved gate model require an independent actual result and keep the gate closed for anything other than candidate-matched `READY`. | Grounded |

## Named Technology / Currency Conclusion

The v9 block introduces no framework, library, package, starter, runtime
version, or external service. Its named technical surfaces—Hexalith.EventStore,
the existing Conversations AppHost, Aspire/ServiceDefaults as prohibited new
module ownership, SHA-256, and gitlinks—are either directly present in the
workspace or fixed by the approved proposal and required baseline. No web claim
or training-data-only assertion was found, and unrelated inherited package
version research is outside this execution-only overlay review.

## Gate Conclusion

The reality check supports the overlay as an architecture-only, deliberately
non-executable projection. It does **not** support treating v9 as mechanically
published or currently machine-resolved. `UNBOUND`, missing companions, the
unimplemented marker reader, and existing v1 drift cumulatively keep the global
implementation hold active exactly as the overlay requires.

## Recheck Addendum — Latest Overlay

**Recheck verdict:** **PASS — both prior high findings are explicitly
fail-closed, assigned to named v9 companion publication work, and no candidate
identity or digest has been fabricated. They remain verified blockers, not
uncontained architecture defects.**

- **F-01 contained:** Current code still has no v9 marker reader, but the latest
  overlay keeps the planning candidate `PC` explicitly `UNBOUND`, calls
  `UNBOUND` a mechanical blocker that cannot be inferred, prohibits IR-0 and
  hold lift, and names the planning validators plus canonical authority bundle
  as required companion artifacts (latest architecture lines 1841–1848 and
  2101–2107). The marker rule is therefore a prospective v9 publication
  contract, not a fabricated claim that current readers already pass.
- **F-02 contained:** The live v1 check still exits `1` with
  `EPIC_6_CURRENT_VIEW_DRIFT`, while the preserved-prefix hash still matches the
  marker exactly. The latest overlay requires the v9 validator to report any
  pre-existing v1 mismatch as a blocker, forbids rewriting v1, and permits v2 to
  become current only after binding the complete v9 candidate and passing parity
  validation (lines 1863–1869). Missing/invalid/mismatched hold state defaults to
  `ACTIVE` (lines 2073–2086).

No committed root candidate, authority-bundle digest, v2 digest, or
supersession-map digest is asserted. Those identities remain deliberately
unbound until the named companion publication exists and validates them.
