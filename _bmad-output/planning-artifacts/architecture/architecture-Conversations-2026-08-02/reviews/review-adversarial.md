# Adversarial Review — V9 Execution Overlay

**Target:** `ARCHITECTURE-SPINE.md`

**Reviewed snapshot SHA-256:** `96b162877efc801a7009b8acc84e5d48432f31caaf5fca89a8644ff96990df6d`

**Lens:** Construct two downstream units that obey the overlay literally but make incompatible choices about shared authority, ownership, mutation, supersession, candidates, and gates.

**Verdict:** **PUBLICATION BLOCKED; HOLD-SAFE.** `UNBOUND` and the global hold prevent the current draft from authorizing implementation, but the cross-unit execution contract is not yet convergent. Five critical/high holes must be closed in the overlay or explicitly assigned to one canonical companion schema and validator before v9 can bind a candidate.

## Two Literally Compliant Downstream Units

| Dimension | Unit A — completion/evidence workflow (Epics 7 and 10) | Unit B — projection/release assurance workflow (Epics 13 and 15) | Incompatibility |
| --- | --- | --- | --- |
| Authority bundle | Binds both named v9 identities, hashes companion files as raw bytes, and includes gitlinks changed or declared by the current story. | Binds the same two identities, hashes schema-canonicalized records, and includes every root-declared gitlink considered release-relevant. | Both cite the required identities and “relevant gitlinks,” but compute different candidate/bundle digests. |
| Candidate lifecycle | Treats the IR-0 publication commit as immutable planning provenance and gives each story a descendant implementation candidate. Source changes do not alter the bound planning candidate. | Treats every required “root candidate” as the current root commit. The first successor implementation commit is candidate drift and restores the hold until validator and IR-0 rerun. | One workflow continues after a valid lift; the other blocks after every implementation commit. |
| Hold state | Consumes a release-owner decision recorded in `sprint-status.yaml`; IR-0 plus that record is the execution guard. | Consumes a separately signed hold-decision artifact and ignores sprint-status comments as projections. | The same repository can be executable for A and held for B. |
| Supersession | Maps each unfinished v8 story once to its successor epic and lets successor stories decide which partial files are salvageable. | Uses the same story-level mapping but assigns salvage at acceptance-scenario/digest granularity. | Both meet the stated one-row-per-story rule while accepting different inherited bytes and potentially losing different non-FR obligations. |
| Graph | Treats the canonical story predecessor table as the executable DAG and applies IR-0/hold lift as an external guard. | Inserts IR-0 and hold-lift records as graph predecessors and derives epic entry edges from story edges. | Both are acyclic and topological, but their graph digests and schedulable-node sets differ. |

## Critical / High Findings

### F-1 — Critical — “Candidate” collapses three lifecycles and makes hold restoration interpretation-dependent

The overlay defines a v9 **publication candidate** (`1841-1848`), requires every story scenario to bind a **root candidate** (`1971-1976`), and invalidates readiness after any change to the **bound candidate** (`2002-2005`). It also requires IR-0 to assess the exact same candidate used for hold lift (`1827-1831`, `1991-1998`). No rule distinguishes:

1. the planning-publication candidate assessed by IR-0;
2. each successor story's implementation/evidence candidate; and
3. the eventual release candidate assessed by Story 15.2/RG-15.

Unit A and Unit B above therefore both obey the words but disagree on whether the first authorized implementation commit automatically restores the global hold. This is a direct execution deadlock/safety divergence, not story-local detail.

**Required disposition:** define versioned candidate kinds and transitions. Bind which bytes and gitlinks identify each kind, the permitted descendant relationship, which mutations invalidate only story evidence versus IR-0, and which candidate RG-15 consumes. If every implementation commit is intentionally meant to require a fresh IR-0, say so explicitly; otherwise constrain IR-0 invalidation to drift in the planning-authority bundle rather than an ambiguous “candidate” change.

### F-2 — Critical — Hold lift/restoration has no single authoritative state record or mutation owner

The release owner must “explicitly record” hold lift (`1827-1831`), no projection may bypass it (`1999-2001`), and drift “keeps or restores” the hold (`1830-1831`, `2002-2005`). The overlay does not name the authoritative record, schema, stable decision identity, signer/owner proof, state vocabulary, single write path, or atomic restoration transition. Sprint status, the IR-0 report, a release-owner memo, and a future gate artifact can consequently disagree while each consumer claims to fail closed.

The same gap recurs at RG-15: an independent review and owner decision are required (`2006-2010`), but “decided,” “closed,” and “reopened” do not have a canonical machine state or candidate binding. A stale positive record can coexist with a later blocker because no precedence or supersession rule is fixed.

**Required disposition:** bind one versioned gate/authorization ledger (or one explicitly named record per gate), one mutation owner, stable gate-decision IDs, exact states and transition rules, candidate/authority/digest bindings, and fail-closed consumer semantics. Drift must create or select one mechanically authoritative restored-hold state; it must not rely on every consumer independently inferring restoration.

### F-3 — High — Shared authority bundle, digest shape, and mutable-artifact ownership remain undefined

The publication must record “relevant gitlinks” and “canonical artifact digests” (`1841-1848`); scenarios and IR-0 repeat “relevant gitlinks” (`1974-1976`, `1991-1995`); companions must match the v9 identity and candidate digest (`2020-2025`). The inherited v8 rules use different context-specific gitlink scopes for promotion and projection evidence, so “relevant” does not select one existing set. The overlay also does not fix:

- the canonical authority-manifest path/schema and schema owner;
- the complete artifact membership set;
- path normalization, byte versus semantic canonicalization, sort order, or digest domain;
- the single writer for the supersession map, v2 view, sprint projection, gate records, and any shared current-head pointers; or
- rejection of overlapping story mutation scopes for those shared artifacts.

Unit A and Unit B can bind the same identity strings while producing mutually unverifiable bundle hashes or overwriting a shared projection under different ownership assumptions.

**Required disposition:** make one machine-readable authority bundle the sole source for the identity pair, candidate kind, exact gitlink set, artifact inventory, schemas, and digests; specify canonicalization and hashing; assign one owner/writer and allowed mutation phase per shared artifact; require story file-baseline inventories to reject overlapping mutable ownership unless a named handoff orders the writes.

### F-4 — High — The supersession map proves old-story row coverage, not obligation preservation

The map is called “zero-gap,” but its enforceable cardinality is exactly one disposition for each **unfinished v8 Story 6.x** (`1898-1902`). The table maps whole stories to whole successor epics (`1904-1915`). That does not prove that each v8 acceptance criterion, checkpoint, prohibition, evidence dependency, failure contract, preserved artifact, or partial-work path is retained exactly once. The general 124-FR and UX counts (`1984-1987`) cannot detect loss of non-FR release-governance and evidence obligations. The approved source's success criterion is stronger: every unfinished v8 **obligation** must land (`sprint-change-proposal-2026-08-02.md:393-403`).

Two successor packages can therefore carry the same legal story-level map while salvaging different bytes, duplicating a shared obligation, or silently dropping a v8 failure contract.

**Required disposition:** give every unfinished v8 obligation a stable source identifier and require exactly one machine disposition: preserved immutable, superseded by named successor AC(s), or rejected with explicit salvage policy. Bind source path/digest, destination story/AC, owner, and strength digest. Validate zero missing, duplicate, split-without-aggregation, and orphaned obligations—not just old story IDs.

## Additional High Finding

### F-5 — High — The canonical graph and its gate nodes are not defined as one hashable structure

The overlay presents epic hard entries (`1917-1929`), a Mermaid projection (`1931-1959`), delegates exact story predecessors to the epic authority (`1961-1968`), and requires companions only to match identity/candidate mechanically (`2020-2025`). Acyclic and lower-numbered is necessary but insufficient: no rule states that the machine story DAG is the sole authority, that epic hard entries are the exact aggregation of it, that the v2 view and sprint projection are derived from its digest, or whether IR-0 and the owner hold-lift are graph nodes versus external guards. RG-15 likewise lacks an edge binding its decision record to the exact accepted Story 15.2 candidate.

**Required disposition:** publish one canonical machine DAG containing story and non-story gate nodes, exact edges, candidate/authority bindings, and node-result semantics. Derive every table, diagram, v2 view, and sprint projection from its digest and fail on edge or node-set drift. If gates remain external guards, state that explicitly and require every executable node to validate the same canonical guard record before transition.

## Gate Conclusion

No finding justifies weakening the hold. The fail-closed disposition is to keep `publication-candidate=UNBOUND`, publish the missing canonical schemas/ownership rules, and require the v9 validator to prove the resolved candidate, authority bundle, obligation map, gate ledger, and DAG agree before IR-0 may run.

## Recheck Addendum — 2026-08-02

**Rechecked snapshot SHA-256:** `d1cea0dbafd4b0bc6030ade66f559309a56a2f0b2c2d38652480a42616852059`

**Verdict:** **ONE CRITICAL CLOSURE DEFECT REMAINS; HOLD-SAFE.** The latest overlay closes the semantic divergence in four of the five prior findings, but the new canonical bundle and hold-state rules form a self-invalidating identity cycle. Keep `PC` unbound and the hold active until this is removed.

| Prior finding | Recheck result |
| --- | --- |
| F-1 — candidate scopes | **Closed.** `PC`, `SC-<story>`, and `RC` are non-interchangeable and descendant story commits no longer redefine `PC` (`1850-1861`, `2081-2086`). |
| F-2 — hold owner/state | **Semantics and ownership closed; digest lifecycle still blocked.** One fail-closed record and one release-owner writer are fixed (`2073-2080`), but that mutable record is also placed inside the immutable planning-bundle parity domain (`1873-1889`, `2101-2104`). |
| F-3 — bundle/digest/owners | **Mostly closed; self-reference remains.** The path, byte hashing, manifest digest, canonical projections, and writers are fixed (`1871-1903`), but `PC` is recorded by the bundle whose containing commit defines `PC`. |
| F-4 — obligation map | **Closed.** The map inventories and maps every v8 criterion, checkpoint, prohibition, dependency, evidence obligation, rollback condition, and completion gate exactly once (`1953-1962`). |
| F-5 — graph/gates | **Closed.** One deterministic graph projection carries explicit IR-0/RG-15 gate nodes and is parity-checked against all projections (`1882-1889`, `1991-2019`). |

### Remaining incompatible downstream units

- **Unit A — detached binding:** generates the bundle in commit `B`, records `PC.rootCommit` as predecessor `P`, and excludes `implementation-hold-v1.json` from the immutable `PC` digest so the release owner can later record `LIFTED`. It can complete IR-0 and lift the hold, but `PC` does not identify the commit containing its declared bundle and Unit A omits a companion the text says participates in bundle parity.
- **Unit B — literal binding:** requires `PC.rootCommit` to equal the commit containing `v9-authority-bundle-v1.json` and includes the hold-state record because the bundle indexes every canonical/generated planning artifact and validates hold-state parity. It can never finalize the bundle's self-referential Git commit hash; even if that were externally patched, changing `ACTIVE` to `LIFTED` changes a bundled artifact, changes the bundle digest/`PC`, invalidates IR-0, and restores `ACTIVE` under `2081-2084`.

Both choices follow part of the literal contract, but one weakens identity and the other makes hold lift unreachable.

**Required closure:** make candidate binding and decisions one-way. Define `PC` as an immutable content/tree identity that excludes its own binding record, or place the signed bundle record in a later detached envelope that binds the immutable planning content without claiming to be inside that content. Publish an exact closed membership list. Exclude mutable IR-0, hold-lift/revocation, and RG-15 decision records from the `PC` digest; require each immutable decision record to reference `PC`, the authority-bundle digest, its predecessor decision, and its assessed candidate. No decision mutation may feed back into the identity it attests.

## Digest-Cycle Closure Recheck — 2026-08-02

**Rechecked snapshot SHA-256:** `de1566463724324f6afdf2e0c4b0e44a7cc8308319ae80d1fd5ecff3ec7d524d`

**Verdict:** **CLOSED; STABLE HOLD LIFT IS REACHABLE.** This supersedes the remaining-defect verdict in the preceding addendum.

The latest contract fixes both feedback loops:

- `PC` is the committed planning-authority **source** candidate and is frozen before record generation; record generation and descendant story commits do not redefine it (`1852-1857`).
- The authority bundle resolves exact canonical blobs from `PC`, while the bundle file itself is outside its digest (`1875-1889`). It therefore attests `PC` without requiring a Git commit to contain its own final hash.
- Validator results, IR-0, hold decisions, story records, RG-15 decisions, and release attestations are also outside the immutable bundle digest. Each references `PC`, the immutable bundle digest, and predecessor record digests in one direction (`1885-1901`).
- The single hold record can consequently transition to release-owner `LIFTED` after matching validator `PASS` and IR-0 `READY` without changing `PC` or the bundle digest (`2085-2097`).

The reachable sequence is now: freeze `PC` → generate and validate the detached immutable bundle → publish matching validator and IR-0 records → write the release-owner hold decision referencing those immutable identities → compute effective state `LIFTED`. A later hold revocation adds/updates only an excluded decision record; canonical-source drift still correctly invalidates IR-0 and restores `ACTIVE`.

The former Unit A/Unit B divergence can no longer be constructed while obeying the text: including mutable decisions in the bundle digest or redefining `PC` after record generation now directly violates the explicit two-phase boundary.
