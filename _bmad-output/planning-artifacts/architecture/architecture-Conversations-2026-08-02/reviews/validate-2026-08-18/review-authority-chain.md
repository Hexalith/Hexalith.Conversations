# Authority-Chain Integrity Review — architecture.md

- **Reviewer lens:** ad-hoc authority-chain integrity (marker mechanics, discovery, sidecar consistency, git state)
- **Date:** 2026-08-18
- **Target:** `_bmad-output/planning-artifacts/architecture.md` (150,505 bytes, worktree clean, last touched by commit `1355a10` "feat(planning): authorize V12 remediation checkpoint")
- **Worktree HEAD:** `29c56fa` "docs(planning): rebind V14 candidate after A3 remediation"

## Verdict

**FAIL — authority-discovery break (critical), on top of fully verified mechanics.** Every
mechanical claim in the document checks out: all three predecessor block byte-counts and SHA-256
digests reproduce exactly, the v8 prefix pin reproduces exactly, marker pairing is well-formed, the
`supersedes=` chain v8→v9→v10→v11→v12 is unbroken, and the authority bundle's digest and every
sidecar cross-pin I could test verify byte-for-byte. But the document's own discovery rule — "machine
readers determine current authority from the last complete architecture overlay marker, never from
the historical frontmatter alone" (lines 1840–1842) — now terminates at the V12 overlay
(lines 2248–2358), while the true current authority has moved into sidecar JSON files
(`v13-current-proof-authority-v1.json`, `v14-current-candidate-authority-v1.json`) and two approved
2026-08-18 proposals that the document never names. A compliant reader of architecture.md alone
reaches a materially wrong picture of the current candidate, the A1 disposition, the execution
graph, and the acceptance criteria now in force. Compounding this, one approved authority-bearing
proposal is entirely untracked and another has drifted in the worktree away from the exact bytes the
canonical authority bundle pins as `canonical-authority-input`. The append-only chain inside the
document was abandoned after V12 without leaving a forwarding pointer, which is precisely the
failure mode the overlay machinery was built to prevent.

---

## 1. Mechanical verification (PASS)

### 1.1 Byte-range convention (determined and reproduced)

No overlay documents its digest convention; I determined it by testing six candidate ranges.
Exactly one convention reproduces all recorded values:

> **Block** = the bytes from the first byte of the `BEGIN` marker line through the last byte of the
> `END` marker line, **excluding** the END line's trailing LF.
> **v8 prefix** = bytes `0` .. start of the V9 `BEGIN` marker line (trailing blank-line LF before
> the marker included).

| Recorded claim (marker line) | Recorded | Computed under convention | Result |
| --- | --- | --- | --- |
| `v8-prefix-sha256` (V9 BEGIN, line 1802) | `7fd33168f34bb7d3326b4abb0eb79999270c11fefc7f50ec3acdd62fb1b86df5` | bytes 0–119265: `7fd33168…86df5` | **MATCH** |
| `v9-block-bytes=18270`, `v9-block-sha256` (V10 BEGIN, line 2127; restated in body, lines 2145–2146) | 18,270 B, `4686212387189e78f98de5352d12eb8544d1a9f78c97dfc446266fa3d4d3f3d9` | bytes 119265–137535: 18,270 B, `46862123…f3d9` | **MATCH** |
| `v10-block-bytes=3846`, `v10-block-sha256` (V11 BEGIN, line 2194; body lines 2209–2210) | 3,846 B, `893315bff3f12d7b949dbeae2a2dfbb301023461ad62c0c6066480a87700774b` | bytes 137537–141383: 3,846 B, `893315bf…774b` | **MATCH** |
| `v11-block-bytes=3042`, `v11-block-sha256` (V12 BEGIN, line 2248; body lines 2263–2264) | 3,042 B, `a97385c11b92fb95f1acf7f4ba370404c4781855581c81a427ed6895a5a4c4d1` | bytes 141385–144427: 3,042 B, `a97385c1…c4d1` | **MATCH** |

For the future V13 append (see Finding 1), the current terminal block measures, under the same
convention:

- **V12 block:** 6,075 bytes, SHA-256 `3050b326c5759fc51bc0e800944b0a1a591ab1782f6798f12abfdc10051b5796`
- **Prefix through end of V11 (start of V12 BEGIN):** `dd71fe7c3227bad83ce1bcf15e1ccc9c9c72445bdeba8a7adac539b9b28564a9`

### 1.2 Marker pairing and nesting (well-formed)

Four BEGIN and four END markers, each unique, correctly ordered, non-overlapping, non-nested,
separated by single blank lines; the file ends exactly at the V12 END line's LF (byte 150505 = EOF):

| Overlay | BEGIN offset (line) | END line-end offset | Notes |
| --- | --- | --- | --- |
| V9 | 119265 (l. 1802) | 137536 (l. 2125) | |
| V10 | 137537 (l. 2127) | 141384 (l. 2192) | |
| V11 | 141385 (l. 2194) | 144428 (l. 2246) | |
| V12 | 144429 (l. 2248) | 150505 (l. 2358) | terminal; END line is the last line of the file |

### 1.3 Supersession chain (unbroken)

`V9 supersedes=conversations-architecture-2026-08-01-v8` → `V10 supersedes=…-2026-08-02-v9` →
`V11 supersedes=…-2026-08-03-v10` → `V12 supersedes=…-2026-08-04-v11`. Each supersession is
obligation-scoped in the body text as the document requires (V9: "only for the remaining-work
execution projection", l. 1814–1815; V10: "only for the Story 10.3/10.4 workflow and
reusable-guidance projection", l. 2139–2140; V11 additive checkpoint; V12 additive checkpoint).

### 1.4 Sidecar cross-pins (all verified)

| Pin | Recorded | Computed | Result |
| --- | --- | --- | --- |
| `v9-authority-bundle-v1.json` → `bundleDigest` (SHA-256 over `<sha256><2 spaces><path><LF>` lines, path-ordinal sort — exactly as V9 overlay l. 1884–1887 specifies) | `26747e43bcf7e7e6…db271610` | identical | **MATCH** |
| bundle → `architecture.md` sha | `61ed0018486749…c3b0ea14` | worktree file identical | **MATCH** |
| bundle → `epic-6-current-execution-view-v2.md` sha | `8fce0f3863098b…dbed1e` | worktree file identical | **MATCH** |
| bundle → `v12-pre-ir0-remediation-authority-v1.json` sha | `e1c3b470b7705c…fc9497` | worktree file identical | **MATCH** |
| v14 → `pointInTimePredecessor` (v13 sidecar) sha | `f2f02115502d42d6e74f1e34351eeda1e1d778b35e2dee485821ac53e448138f` | worktree v13 identical | **MATCH** |
| v13 → `currentProofContract` sha | `de1a21c11f84f0…ab2de9` | worktree contract identical | **MATCH** |
| v12 sidecar → `supersessionContract` sha | `69e220a0a6ff49…588bf4` | worktree contract identical | **MATCH** |
| v14 → `provenanceLedger` sha (`_bmad-output/implementation-artifacts/sprint-status-provenance-v1.md`) | `d60236386b2934…14f9d2` | worktree ledger identical | **MATCH** |
| v13 `planningCandidate` `08d38fc0…` | — | exists: commit "test(planning): fix retrospective status-fault mutation syntax" | **OK** |
| v12/v14/bundle `planningCandidate` `151f9651…` | — | exists: `HEAD~1`, "fix(planning): restore context safeguards and rebind A3 candidate" | **OK** |

One bundle-vs-worktree mismatch exists and it is a finding, not a verification error — see
Finding 2.

---

## 2. Findings

### Finding 1 — CRITICAL — The document's own discovery rule now resolves to stale authority; V13/V14 are invisible

**Rule under test:** "Machine readers determine current authority from the last complete
architecture overlay marker, never from the historical frontmatter alone" (architecture.md
l. 1840–1842). Append-only correction plus that rule is the entire safety argument for the frozen
frontmatter.

**Evidence:**

- The last complete overlay marker is **V12** (`conversations-architecture-2026-08-04-v12`, BEGIN
  l. 2248, END l. 2358). Discovery therefore yields V12.
- True current authority is no longer V12. It is carried by sidecars and proposals **never named
  anywhere in architecture.md** (`grep -in 'v13|v14|current-proof|current-candidate|E6-CURRENT'`
  over the document returns zero hits):
  - `_bmad-output/planning-artifacts/v13-current-proof-authority-v1.json` — checkpoint
    `E6-CURRENT-PROOF`, predecessor `E6-REMEDIATION`, action A1 `status: done`, resolved by the
    independent decision `epic-6-completion-supersession-current-proof-decision-v1.json`
    (`ACCEPTED`, 2026-08-09, release owner).
  - `_bmad-output/planning-artifacts/v14-current-candidate-authority-v1.json` — checkpoint
    `E6-CURRENT-CANDIDATE`, predecessors `E6-REMEDIATION` and `E6-CURRENT-PROOF`, current planning
    candidate `151f96519a30f1b16530851e73e51ac5ad74b355`, sprint-status provenance-ledger
    relocation pinned by digest.
  - `sprint-change-proposal-2026-08-18-e6-remediation-a3.md` (APPROVED by the release owner
    2026-08-18) and `sprint-change-proposal-2026-08-18-readiness-gate-ac10-ac11.md` (APPROVED
    2026-08-18), the second of which amends the first's release-owner-approved acceptance criteria
    AC-10 and AC-11 and fault F-10.

**Stale conclusions a builder reading only architecture.md would reach:**

1. **A1 disposition.** V12 (l. 2300–2321) states A1 is the exact Story 6.7/6.2 done-tree
   reconstruction and "Only `ACCEPTED` satisfies A1." In reality that historical route produced
   FAIL/REJECTED evidence (preserved byte-identical per v13 assertion: "V1-V12 historical
   FAIL/REJECTED completion-supersession evidence remains byte-identical and authoritative for the
   historical question"), and A1 was closed on 2026-08-09 through the **additive current-proof
   route** (V13) that the document neither authorizes nor mentions. Sprint-status confirms:
   `epic-6-retro-item-24…: done` while items 25/26 (A2/A3) remain `open`.
2. **Execution graph.** V12 (l. 2271–2282) fixes the graph at "exactly 33 nodes" with
   `PC-PUBLICATION -> E6-REMEDIATION -> IR-0`. The sidecar chain has since added the
   `E6-CURRENT-PROOF` and `E6-CURRENT-CANDIDATE` checkpoint nodes.
3. **Current candidate.** The document binds candidates only through
   `candidate-binding=v9-authority-bundle-v1.json`; nothing in the document reveals the candidate
   was rebound twice since V12 publication (`a232614`-era → `08d38fc` → `151f9651`, commits
   `151f965` "rebind A3 candidate" and `29c56fa` "rebind V14 candidate after A3 remediation").
4. **Acceptance criteria in force.** AC-10 (zero-skip full lane, now required to be
   ambient-worktree-independent) and AC-11 (the 28-vs-30 count discrepancy declared RESOLVED as a
   misattribution) were amended on 2026-08-18; the reader of the document cannot learn these
   criteria or their amendment exist at all.
5. **Hold.** The reader's conclusion (hold ACTIVE) happens to remain correct — every sidecar agrees
   `implementationHold: ACTIVE` — but only by coincidence of state, not because discovery works.

**Severity rationale:** discovery from the document alone yields the wrong current authority —
the rubric's critical case.

**Repair:** Append (never edit) an `ARCHITECTURE-EXECUTION-OVERLAY-V13` block after V12, following
the established marker grammar, carrying: `supersedes=conversations-architecture-2026-08-04-v12`
(obligation-scoped), `v12-block-bytes=6075`,
`v12-block-sha256=3050b326c5759fc51bc0e800944b0a1a591ab1782f6798f12abfdc10051b5796`, and an
explicit **authority-relocation rule**: name `v13-current-proof-authority-v1.json` and
`v14-current-candidate-authority-v1.json` (with their SHA-256 at the commit that publishes the
overlay), the two approved 2026-08-18 proposals, and state that checkpoint-level authority now
lives in versioned sidecar files whose latest version is announced by the last complete overlay
marker in this document. From then on, every new sidecar version must be accompanied by an appended
overlay (or an appended one-paragraph pointer amendment) so the last-marker rule keeps resolving to
true current authority.

### Finding 2 — HIGH — Approved authority artifacts uncommitted / drifted from their bundle-pinned bytes

**Evidence:**

- `sprint-change-proposal-2026-08-18-readiness-gate-ac10-ac11.md` is **untracked** (`??` in
  `git status`) yet its header records "**Status: APPROVED** by Jerome (release owner) on
  2026-08-18 at HEAD `29c56fa`" and it amends release-owner-approved acceptance criteria of the A3
  proposal. An approved, obligation-bearing correction currently exists only as an unversioned
  worktree file — no history, no digest pin, recoverable from nowhere if lost.
- `sprint-change-proposal-2026-08-18-e6-remediation-a3.md` is pinned in
  `v9-authority-bundle-v1.json` as `role: canonical-authority-input, source: candidate` with SHA-256
  `fbac262470d6991f…aacfa27e1` — which equals the committed blob at `29c56fa`. The worktree copy is
  **modified** (+23/−3, applying the AC-10/AC-11/F-10 amendments) and now hashes
  `cbc2eb2f3db96f04…`. Under the document's own drift rules this is candidate-source drift: V9
  (l. 1858–1861) "Only a change to a canonical blob resolved from `PC` … invalidates IR-0"; V10
  (l. 2180–2182) "Any later canonical repair invalidates and regenerates the candidate-bound
  companions."

**Consequence:** the current worktree no longer matches the bound planning candidate; any consumer
verifying the bundle against the worktree fails closed (the exact `CANDIDATE_SOURCE_DRIFT` family
the A3 proposal itself describes), and the only record of the approved amendment is unprotected.

**Repair:** Commit both proposals (Conventional Commits message, validated with the pinned
commitlint CLI per repository baseline), then regenerate the candidate-bound companion set
(`publish_v9_planning_authority.py` route) at the new commit and rebind — producing the next
candidate rebind commit exactly as `151f965`/`29c56fa` did, and ideally carrying the Finding 1
V13 overlay in the same publication so document discovery and sidecar authority re-converge.

### Finding 3 — HIGH — Frontmatter and V12 sidecar mislead readers who trust them; the only warning sits 1,800 lines away

**Evidence:**

- Frontmatter (frozen, and byte-pinned by the V9 `v8-prefix-sha256`, so it *cannot* be edited
  without breaking the chain): `authorityVersion: 'conversations-architecture-2026-08-01-v8'`
  (l. 7), `status: 'authority-correction-only-not-ready'` (l. 5),
  `currentExecutionView: '…epic-6-current-execution-view-v1.md'` (l. 31), `correctionAuthority`
  ending at the 2026-08-01 proposals (l. 19–30), `supersededAuthorityVersions` ending at v7
  (l. 8–15). Reality: authority is v12-plus-sidecars, the current view is
  `epic-6-current-execution-view-v2.md` (exists in the same folder, committed, bundle-pinned
  `8fce0f38…`, bound to `conversations-architecture-2026-08-04-v12` and PC `151f9651`), and eight
  further correction proposals (2026-08-02 through 2026-08-18) exist. The only in-document warning
  that the frontmatter is frozen provenance is inside the V9 overlay body (l. 1839–1843); a YAML
  frontmatter consumer, or any human skimming the header, sees v8/v1 with no in-band flag.
- `v12-pre-ir0-remediation-authority-v1.json` still records A1 `status: "open"` while
  `v13-current-proof-authority-v1.json` records A1 `status: "done"` and sprint-status records
  `epic-6-retro-item-24…: done`. Two live authority sidecars disagree on the same action's status;
  the v12 sidecar was even regenerated at `29c56fa` (candidate rebind) without reconciling or
  annotating this.

**Severity rationale:** a reader following a legitimate entry point (frontmatter, or the v12
sidecar the V12 overlay points at) reaches materially stale conclusions — the rubric's high case.

**Repair:** Both fixes are append/regenerate-only, respecting the pinned prefix: (a) the Finding 1
V13 overlay must restate, at the *end* of the document where discovery lands, that the frontmatter
keys `authorityVersion`, `status`, `currentExecutionView`, `correctionAuthority`, and
`supersededAuthorityVersions` are v8-frozen provenance and name their current values; (b) the v12
sidecar is `source: generated` — regenerate it to either carry derived statuses or an explicit
`statusAsOf`/"statuses frozen at V12 publication" field so it cannot be read as current.

### Finding 4 — MEDIUM — No canonical index pins the newest authority sidecars; bundle references from V13-era records went silently stale

**Evidence:**

- The 85-artifact list in `v9-authority-bundle-v1.json` contains **neither**
  `v13-current-proof-authority-v1.json` **nor** `v14-current-candidate-authority-v1.json`. The only
  digest protection on v13 is v14's predecessor pin (verified); **nothing pins v14 itself** — the
  head of the authority chain is digest-protected by no other artifact.
- v13 and v14 bind the bundle by **path only** (`authorityBundlePath`), per the V11/V12 one-way
  digest rule. The bundle was then regenerated at `29c56fa`: the 2026-08-09 decision record pins
  `authorityBundleSha256: 0751e0fd…` / `authorityBundleDigest: a2c5011f…`, while the current bundle
  hashes `971ce3e9…` with digest `26747e43…`. The historical decision remains valid for its pinned
  moment, but a reader resolving "the bundle" through v13/v14 today reaches different bytes than
  the ones in force when those authorities were minted, with no in-file signal.

**Repair:** At the next bundle regeneration include the current v13/v14 sidecars (and their
schemas) as pinned artifacts, and have each future sidecar version pin its predecessor's SHA-256
the way v14 already pins v13 — making the head of the chain the only unpinned artifact at any time,
and only until its successor lands.

### Finding 5 — MEDIUM — The digest byte-range convention is nowhere recorded

**Evidence:** Section 1.1. Six plausible conventions exist; exactly one reproduces the recorded
digests, and I had to determine it experimentally. A future verifier (or a marker-emitting script
rewritten under BMAD upgrade pressure — precisely what commit `4ba45a7` did to the context
safeguards) could compute a different range in good faith and report false corruption, or worse,
recompute-and-restate a wrong pin in a new overlay.

**Repair:** The V13 overlay should state the convention normatively in one sentence: "A block digest
is SHA-256 over the bytes from the first byte of the `BEGIN` marker line through the last byte of
the `END` marker line, excluding the END line's trailing LF; a prefix digest covers bytes 0 through
the byte immediately before the `BEGIN` marker line." Add a regression test beside the existing
planning verifiers that recomputes all recorded marker digests from the committed document.

### Finding 6 — LOW — Marker attribute schema is inconsistent across overlays

**Evidence:** The V9 END marker (l. 2125) carries only `version=`, while V10/V11/V12 END markers
also carry `epic-authority=` and `candidate-binding=`. The V10 BEGIN/END markers omit the `hold=`
flag that V11 and V12 carry (`hold=ACTIVE`), even though V10's own body states "The global
implementation hold remains `ACTIVE`" (l. 2188). A parser keying on END-marker attributes, or on
`hold=` presence, sees an inconsistent schema and could mis-infer that no hold applied during V10.

**Repair:** Do not edit the immutable markers. The V13 overlay's markers should carry the full
attribute set (`version`, `epic-authority`, `supersedes`, predecessor bytes/sha, `candidate-binding`,
`hold`), and the marker-grammar sentence from Finding 5's repair should declare the full set
normative for all future overlays.

---

## 3. Git-state summary (for the record)

- `architecture.md`: clean; last commit `1355a10` (V12). History `1355a10` ← `9c7d8e6` (v11) ←
  `27b7829` (v10) — one commit per overlay, as the append-only model intends.
- `sprint-change-proposal-2026-08-18-e6-remediation-a3.md`: committed at `151f965`
  (blob = bundle pin `fbac2624…`), **modified in worktree** (`cbc2eb2f…`) — Finding 2.
- `sprint-change-proposal-2026-08-18-readiness-gate-ac10-ac11.md`: **untracked**, APPROVED —
  Finding 2.
- Sidecars `v12`/`v13`/`v14`, both supersession contracts, the provenance ledger, the bundle, and
  `epic-6-current-execution-view-v2.md`: all committed and byte-identical to their pins.
- `references/Hexalith.FrontComposer`: gitlink drift (disclosed in the readiness-gate proposal's
  approval header as known ambient state; outside this review's scope, but any publication commit
  must not capture it).
