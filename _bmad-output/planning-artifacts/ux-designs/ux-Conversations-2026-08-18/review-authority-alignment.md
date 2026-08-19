# Authority Alignment Review — Conversations UX preservation set

Reviewed at HEAD `29c56fa` (2026-08-18), worktree carrying the modified A3 proposal, the untracked
readiness-gate proposal, and the disclosed `references/Hexalith.FrontComposer` gitlink drift.
No validated file was changed by this review.

## Overall verdict

**CURRENT and coherently pinned.** The UX requirement map is a managed companion that the V9
publisher regenerates at every candidate rebind, and at HEAD it binds exactly the candidate,
epic authority, and architecture authority that the newest sidecar (V14) binds — it was itself
rebound in commit `29c56fa`. The spec's older `ux-preservation-planning-2026-08-01-v1` stamp
against the map's `...-2026-08-04-v3` is a designed asymmetry (frozen source vs advancing
disposition authority), not drift. Nothing in the 2026-08-18 proposals or execution views
contradicts the preserved-not-activated disposition; the one live staleness vector is that
approved-but-uncommitted AC amendments in the worktree guarantee a further candidate rebind soon,
after which `planningCandidate: 151f965...` must be re-read, not assumed.

## Binding-by-binding assessment

| Frontmatter field (file) | Pinned value | Current reality | Verdict |
| --- | --- | --- | --- |
| `authorityVersion` (ux-requirement-map.md:2) | `ux-preservation-planning-2026-08-04-v3` | Hardcoded by the live publisher (`publish_v9_planning_authority.py:1401`); lineage v1 (`a1f907d`) → v2 (`dd5b91a`) → v3 (current) | **CURRENT** |
| `planningCandidate` (ux-requirement-map.md:5) | `151f96519a30f1b16530851e73e51ac5ad74b355` | Identical to V14 `authority.planningCandidate` (v14-current-candidate-authority-v1.json:7) and to execution view v2 `planning_candidate` (epic-6-current-execution-view-v2.md:6). `151f965` exists (`cat-file -t` = commit) and **is an ancestor of HEAD** (HEAD~1; `29c56fa` is its companion-rebind commit N+1). Map worktree bytes are identical to HEAD. | **CURRENT** — but see finding 2 on imminent rebind |
| `epicAuthority` (ux-requirement-map.md:6) | `epic-6-authority-2026-08-04-v12` | Matches V14:5, V13:5, V12:5 and execution view v2:7 | **CURRENT** |
| `architectureAuthority` (ux-requirement-map.md:7) | `conversations-architecture-2026-08-04-v12` | Matches V14:6, V13:6, V12:6 and execution view v2:8 | **CURRENT** |
| `currentOwner` (ux-requirement-map.md:8) | `Stories 8.1-8.2 preservation contract` | Epic 8 "Preserved UX Governance" still owns it: epics.md:2542-2637 ("Bounded exit: Stories 8.1-8.2 are `done`...", "V8 source owner: superseded Story 6.4"), execution view v2 rows 8.1/8.2 (lines 32-33), and the live C# validator asserts every UX-DR row carries this exact owner string (PlanningAuthorityV8ValidationTest.cs:215, :238). Publisher rewrites legacy "Story 6.4 disposition contract" → this owner (publish_v9_planning_authority.py:1420). | **CURRENT** — see finding 4 on residual V8-era prose |
| `currentDisposition` / `activationAuthority` (map:4,9; spec:33-34) | `preserved-not-activated` / `separate-approved-release-authority-required` | Reaffirmed everywhere: V14 prohibitions ("implement or start successors", hold ACTIVE), A3 proposal §13, readiness proposal §6 and checklist 3.3 "UI/UX conflicts [N/A]"; Stories 8.1-8.2 remain behind 7.4 ← IR-0 ← ACTIVE hold; AC-14.1-03 enforces `UX_SCOPE_ACTIVATED` as a fault (epics.md:3556) | **CURRENT, uncontradicted** |
| `preservationAuthorityVersion` (ux-design-specification.md:32) | `ux-preservation-planning-2026-08-01-v1` | Spec last touched at `a1f907d`; not a managed companion, not rewritten by the publisher. Its own text delegates: "The current disposition ... is maintained in `ux-requirement-map.md`" (spec:50-54). Recorded at v1 in implementation-readiness-report-2026-08-02.md:603. | **COHERENTLY STALE-BY-DESIGN** (frozen preservation source; map advances independently) |

Cross-checks: V13 pins a *different* candidate (`08d38fc0`, v13-current-proof-authority-v1.json:7)
— this is explicitly point-in-time (V14 `pointInTimePredecessor.supersedesEvidence: false`,
v14:16-22; A3 proposal §4.4 "V13 byte-frozen"), so it does not contradict the map's pin. The
52-decision / 28-acceptance denominators in the map (52 UX-DR rows, 28 AC rows counted) match
Story 8.2's "52-decision/28-acceptance zero-gap validator" (epics.md:2599, execution view v2:33)
and AC-14.1-03's `52/52` + `28/28` expectation. Neither 2026-08-18 proposal supersedes or rebinds
the UX artifacts: the A3 proposal's artifact-conflict table records "UX specification / UX map:
None" (line 138) and lists `ux-requirement-map.md` among the 42 atomically regenerated companions
(§5); the readiness proposal records "PRD / epics.md / architecture / UX spec: None ... Unchanged"
(§2). Leaving the map bound to `151f965`/v12 authorities is therefore consistent by design, not an
omission — the map rides the candidate chain mechanically.

## Findings

- **medium** The map's currency is time-limited by pending approved amendments. The worktree A3
  proposal carries the applied CP-1..CP-3 amendments (+23/−3 vs HEAD, uncommitted; readiness
  proposal §5 task 2 "APPLIED 2026-08-18 ... uncommitted"), and that file is in `CANONICAL_PATHS`
  (publish_v9_planning_authority.py:266) — so the candidate check against `151f965` already fails
  on it, and landing the amendment forces a new candidate commit N plus rebind commit N+1, which
  will regenerate the map with a new `planningCandidate`. An agent that caches
  `planningCandidate: 151f965...` (or V14's identical pin) as durable current authority will be
  consuming a stale binding the moment that rebind lands; the untracked readiness proposal is
  itself not yet candidate-bound anywhere. *Fix:* consumers must re-read the map/V14 pin at use
  time (the concurrent-session hazard §6 of the A3 proposal already mandates this for HEAD); when
  the amendment commits, add the readiness proposal to `CANONICAL_PATHS` alongside the A3 proposal
  so both 2026-08-18 authority records are candidate-bound.
- **low** Internal inconsistency in the A3 proposal about the map's regeneration: §2 says "UX map
  regenerates byte-identically" (line 138) while §5 lists `ux-requirement-map.md` under
  "Regenerated atomically (42)" — and the actual regeneration at `29c56fa` changed one frontmatter
  line (`planningCandidate: 08d38fc0...` → `151f965...`; verified via
  `git diff 55e3fd0 29c56fa -- ux-requirement-map.md`). An agent auditing against the
  "byte-identically" claim would report phantom drift. *Fix:* in the forthcoming amendment commit
  (or the next proposal), restate as "regenerates with only the designed `planningCandidate`
  rebind; all 52/28 rows and dispositions byte-identical" — do not rewrite the committed text.
- **low** The map's `source` (ux-design-specification.md) is not byte-protected by the candidate
  gate: it appears nowhere in the publisher's `CANONICAL_PATHS`/protected set (only `UX_MAP_PATH`,
  line 35), and the promised source-hash binding is deferred to Story 8.1 AC-8.1-02
  (`UX_SOURCE_UNBOUND`/`UX_SOURCE_DRIFT`, epics.md:2587), which is non-executable behind IR-0 and
  the ACTIVE hold. The C# validator does derive AC identifiers from the spec and requires exact
  ordered parity with the map (map:84-87; PlanningAuthorityV8ValidationTest.cs), so identifier
  drift is caught — but non-identifier spec edits would pass ungated until 8.1 executes. The map
  discloses this gap ("will bind source hashes ... during story execution", map:21-23), so it is
  disclosed, not silent. *Fix:* none required now; optionally add the spec path to the protected
  candidate set at the next rebind for interim byte-freeze.
- **low** Mixed-era ownership prose in epics.md: the "UX Preservation Planning Contract" section
  (epics.md:2116-2121) still states "Story 6.4 owns the versioned disposition deliverables and
  zero-gap validator" (V8-era text), while Epic 8's header (epics.md:2548) records "V8 source
  owner: superseded Story 6.4" and the map's `currentOwner` is Stories 8.1-8.2. An agent reading
  only the earlier section could assign ownership to a superseded story. epics.md is byte-frozen
  under A3 §5, so it cannot be edited to fix this. *Fix:* rely on the map's provenance note
  ("planned paths are rebound by Epic 8 under v12", map:22-23) as the navigational correction; if
  epics.md is ever legitimately reissued under a new epic authority version, retire the V8-era
  sentence then.

No critical or high findings. Nothing contradicts `preserved-not-activated` or the
separate-release-authority activation requirement.

## Evidence trail

- Commits: `29c56fa` (rebind V14 candidate; touched ux-requirement-map.md — its file history:
  `29c56fa`, `55e3fd0`, `144c380`, `52240cf`, `7416583`, `a459f54`, `2c47423`, ...,
  `a1f907d`); `151f965` (candidate commit N, "restore context safeguards and rebind A3
  candidate"); `a232614` (baseline); spec history ends at `a1f907d`.
  `git merge-base --is-ancestor 151f965 HEAD` → true; `git cat-file -t 151f965` → commit.
- Sidecars: v14-current-candidate-authority-v1.json (:5-9 authority block, :16-22
  pointInTimePredecessor with `supersedesEvidence: false`, :39-52 prohibitions, :54-58
  completionEffect all-false); v13-current-proof-authority-v1.json (:7 candidate `08d38fc0`,
  :64-70 completionEffect); v12-pre-ir0-remediation-authority-v1.json (:7 candidate `151f965`,
  :32-39 action A3).
- Proposals: sprint-change-proposal-2026-08-18-e6-remediation-a3.md — §2 artifact-conflict row
  line 138, §4.3-§4.4, §5 regenerated/byte-frozen inventories, §13 non-authorizations; worktree
  diff vs HEAD = +23/−3 (applied CP-1..CP-3). sprint-change-proposal-2026-08-18-readiness-gate-
  ac10-ac11.md — §2 artifact-conflict table (UX row "None"), §5 sequence task 2 (applied,
  uncommitted), §6 non-authorizations, §7 checklist item 3.3.
- Epic chain: epics.md:2116-2121 (UX Preservation Planning Contract), :2220/:2542-2637 (Epic 8,
  Stories 8.1/8.2, AC-8.1-02, AC-8.2-01), :3556 (AC-14.1-03 52/28 + `UX_SCOPE_ACTIVATED`);
  epic-6-current-execution-view-v2.md (frontmatter :6-9; rows 8.1/8.2 at :32-33; Gate State).
- Tooling: publish_v9_planning_authority.py:35 (`UX_MAP_PATH`), :266 (A3 proposal in
  `CANONICAL_PATHS`), :1395-1420 (`render_ux_map` — hardcodes authorityVersion v3, injects
  candidate/epic/architecture, rewrites Story 6.4 owner);
  PlanningAuthorityV8ValidationTest.cs:25-26, :215, :238-244.
- Map content counts: 52 `UX-DR` rows; 28 acceptance rows (8 SAFE + 15 RESP + 2 A11Y + 1 LEAK +
  1 MOB + 1 PERF). implementation-readiness-report-2026-08-02.md:603 (spec recorded at v1).
