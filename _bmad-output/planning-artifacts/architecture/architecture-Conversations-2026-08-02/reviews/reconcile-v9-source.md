# V9 Architecture Overlay — Approved-Source Reconciliation

**Reviewed artifact:** `_bmad-output/planning-artifacts/architecture.md`, appended block at lines 1802–1991  
**Load-bearing source:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md`  
**Review date:** 2026-08-02  
**Verdict:** **NOT SOURCE-COMPLETE — two blockers and two material contract gaps remain.**

The appended block correctly preserves the v8 technical design, maps every Story 6.x disposition to the approved successor epic, reproduces the Epic 7–15 outcome graph, and keeps the implementation hold fail-closed through IR-0 plus the release-owner decision. It is not yet mechanically usable as the current v9 Architecture authority because the enclosing artifact still advertises v8/v1 metadata and the overlay does not bind a concrete publication candidate or digest.

## Findings

### F-01 — BLOCKER: the enclosing artifact still declares v8 and the v1 execution view as current

**Evidence**

- Architecture frontmatter still declares `authorityVersion: conversations-architecture-2026-08-01-v8` and `currentExecutionView: ...epic-6-current-execution-view-v1.md` at lines 7 and 31.
- The appended block declares `conversations-architecture-2026-08-02-v9` and treats generated view v2 as the current companion at lines 1802–1811 and 1984–1988.
- The approved proposal requires v1 to remain provenance, v2 to be the current view, and all canonical/generated artifacts to reference one v9 identity (proposal lines 312–320 and SC-10 at line 402).

**Impact**

Frontmatter consumers and validators will continue to resolve Architecture as v8 and v1 while block-aware consumers resolve v9 and v2. That is the exact cross-artifact identity split the approved validator is required to reject.

**Required correction**

Make the artifact-level current-authority metadata resolve unambiguously to v9, include v8 in the superseded execution-authority chain, register the approved 2026-08-02 proposal, and make the current-view metadata resolve to generated v2 while preserving v1 as immutable provenance. This metadata correction must not rewrite the v8 technical body or completed-history bytes.

### F-02 — BLOCKER: no concrete v9 publication candidate or digest is bound

**Evidence**

- The overlay names Architecture and Epic authority strings, but contains no root commit, worktree policy, gitlink set, frozen-publication manifest, or concrete candidate/authority digest.
- It nevertheless requires IR-0 to bind “the root candidate” and canonical artifact digests and requires companions to match “the canonical v9 identity and candidate digest mechanically” at lines 1958–1962 and 1984–1989.
- The approved sequence freezes the v9 candidate and inventories and records their digests before appending the overlays (proposal lines 380–383); SC-10 requires all canonical and generated artifacts to reference the same v9 identity and candidate (line 402).

**Impact**

“Same candidate” cannot be evaluated mechanically from this Architecture artifact, and no verifier can prove which digest the companion files must match. IR-0 therefore lacks a resolvable candidate boundary.

**Required correction**

Bind a concrete candidate identity and canonical publication digest in the overlay or reference a single canonical candidate manifest by exact path, schema identity, and digest. The same binding must be used by the Epic overlay, supersession map, story specifications, UX map, view v2, sprint projection, validator output, and IR-0 report.

### F-03 — MAJOR: the acceptance-scenario identity rule is weaker than the approved rule

**Evidence**

- The approved contract requires `AC-<epic>.<story>-<two-digit-sequence>` (proposal line 224).
- The Architecture overlay states `AC-<epic>.<story>-<sequence>` (line 1943).

**Impact**

The overlay permits noncanonical IDs such as `AC-7.1-1`; separately built validators can disagree on identity normalization and ordering.

**Required correction**

Restore the exact two-digit sequence constraint in the Architecture consistency rule and validator.

### F-04 — MAJOR: exact UX and FR denominator validation is not bound by the overlay

**Evidence**

- The overlay preserves 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs, 52 UX decisions, and “every explicit UX acceptance criterion” at lines 1845–1848.
- The approved source fixes exactly 124/124 FR coverage and exactly 52 UX decisions plus 28 UX acceptance IDs, with missing, duplicate, and orphan bindings failing validation (proposal lines 44–45, 302–306, 355–357, and SC-02/SC-09 at lines 394 and 401).

**Impact**

“Every explicit” is self-referential and does not preserve the approved 28-ID denominator if an ID is silently removed from the referenced map. Likewise, the overlay does not state the 124/124 zero-orphan/zero-duplicate validation condition. The broad v8 inheritance clause protects existing semantics, but it does not create these newly approved numeric v9 validator assertions.

**Required correction**

Bind the exact `124/124`, `52`, and `28` denominators and require zero missing, duplicate, and orphan paths/bindings in the v9 planning validator. Keep all 52 decision identities and 28 acceptance identities byte-stable.

## Correctly Landed Requirements

| Source requirement | Reconciliation |
| --- | --- |
| Replace only unfinished v8 execution authority | Correct. Lines 1816–1821 preserve completed history and treat partial/prepared work as unaccepted input. |
| Preserve v8 technical invariants | Correct. Lines 1832–1866 make all v8 technical invariants binding/read-only and explicitly retain ownership, EventStore authority, tenant fail-closed behavior, UX non-activation, AppHost limits, SM-C2, completed history, and the remaining v8 rules. The inherited v8 body retains the immutable 13,289 LOC SM-1 baseline. |
| Old-to-new Story 6.x mapping | Correct. Lines 1870–1887 match every approved disposition, including provenance/salvage treatment for 6.3, 6.8, and 6.12 and separation of 6.6 attestation from RG-15. |
| Epic 7–15 outcome boundaries | Correct. Lines 1889–1930 reproduce the approved entry/exit graph without a conflicting edge. Exact story predecessor sets are properly delegated to canonical Epic authority. |
| Global implementation hold | Correct and conservatively strengthened. Lines 1823–1830 and 1958–1969 require complete publication, mechanical validation, candidate/authority-matched independent IR-0 `READY`, and an explicit release-owner hold-lift; status, partial work, blockers, and drift cannot bypass it. |
| Gate semantics | Correct. IR-0 is independent and pre-implementation; RG-15 follows Story 15.2, is not a developer story, does not predetermine closure, and cannot authorize earlier implementation (lines 1958–1974). |
| Runtime/publication scope boundary | Substantively correct. Lines 1976–1989 authorize planning artifacts only and prohibit product/runtime/API/persistence/infrastructure/deployment/package/baseline/evidence/completed-record/submodule changes. The proposal’s separate approval record remains the governing prohibition on commits, pushes, evidence rewriting, hold bypass, and release closure. |
| v1 provenance and v2 projection | Correct inside the appended block (lines 1984–1989), but contradicted by stale frontmatter as recorded in F-01. |

## Reconciliation Conclusion

No approved old-to-new mapping, technical invariant, implementation-hold rule, or IR-0/RG-15 semantic landed incorrectly in the appended block. Publication cannot be treated as mechanically complete until F-01 and F-02 are resolved. F-03 and F-04 must also be corrected before the v9 atomic-contract and denominator validators can conform to the approved proposal.
