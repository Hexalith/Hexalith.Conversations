# Reality-Checked Review — V13 Authority-Relocation And Divergence-Closure Amendment

- **Target:** `_bmad-output/planning-artifacts/architecture.md`, block between
  `ARCHITECTURE-EXECUTION-OVERLAY-V13:BEGIN` (line 2360) and `:END` (line 2569),
  version `conversations-architecture-2026-08-18-v13`.
- **Reviewer lens:** every committed factual claim reality-checked against the
  repository (digests recomputed, sidecar JSONs read, code read, props read,
  solution read, chain digests recomputed, candidate lists compared).
- **Date:** 2026-08-18. Read-only inspection; no submodules initialized or
  updated.

## Verdict

**PASS.** Every committed factual claim in the V13 overlay reproduces against
the repository. All five named SHA-256 digests match recomputed values; both
sidecar JSONs support every content claim the overlay makes about them; the
DC-7 ratified key grammar matches the accepted Story 6.2 code exactly; all
Factual Refresh statements are observed true; the full V8→V13 marker digest
chain reproduces byte-for-byte (the append disturbed nothing); and the DC-2
promoted vocabulary lists are an exact member-for-member match with all four
in-document candidate sites, with no silent additions or drops. Two LOW
observations are recorded below; neither is a factual error in the overlay.

---

## 1. Named SHA-256 digests (five files)

Computed with `sha256sum` over working-tree bytes in
`/home/administrator/projects/hexalith/conversations/_bmad-output/planning-artifacts/`.

| File | Stated | Observed | Verdict |
| --- | --- | --- | --- |
| `v13-current-proof-authority-v1.json` | `f2f02115502d42d6e74f1e34351eeda1e1d778b35e2dee485821ac53e448138f` | identical | MATCH |
| `v14-current-candidate-authority-v1.json` | `e96c34dfdf7f2cd8619b75abc42aad40ab0d8606d3ab798bf2b9b58fac83da7f` | identical | MATCH |
| `sprint-change-proposal-2026-08-18-e6-remediation-a3.md` | `cbc2eb2f3db96f0451c5b6d7d18a915901e3890594b147cbd18194a67fd1d2c0` | identical | MATCH |
| `sprint-change-proposal-2026-08-18-readiness-gate-ac10-ac11.md` | `66d12d5f26bc38e0ee20e0b26827b5c1cf0b6f8d06512231edf408cd1b4494dd` | identical | MATCH |
| `epic-6-current-execution-view-v2.md` | `8fce0f3863098ba4061b4ef0768e7855c54de091d18162acb8d34b2dcbdbed1e` | identical | MATCH |

The overlay qualifies the two proposal digests as "approved bytes staged for
the next publication commit". Observed `git status --porcelain`: the a3
proposal is staged-modified (`M `), the readiness-gate proposal is staged-new
(`A `), and `architecture.md` itself is staged-modified — exactly the
pre-publication state the overlay's Chain Hygiene section describes.
**Verdict: SUPPORTED.**

## 2. Sidecar content claims

### `v13-current-proof-authority-v1.json`

| Claim (overlay) | Observed (JSON) | Verdict |
| --- | --- | --- |
| checkpoint `E6-CURRENT-PROOF` | `"checkpointId": "E6-CURRENT-PROOF"` | MATCH |
| predecessor `E6-REMEDIATION` | `"predecessors": ["E6-REMEDIATION"]` (sole member) | MATCH |
| Retro action A1 is `done` | `actionInventory[0]`: `"id": "A1"`, `"status": "done"` | MATCH |
| hold ACTIVE | `"implementationHold": "ACTIVE"` | MATCH |

The "as of 2026-08-09" date and the "closed … by the independent decision …
(`ACCEPTED`, release owner)" clause are not carried in the v13 sidecar itself;
they are carried by
`epic-6-completion-supersession-current-proof-decision-v1.json`, which I read:
`"decisionDate": "2026-08-09"`, `"decision": "ACCEPTED"`,
`"decisionAuthority.owner": "Release owner"`, and it binds
`"checkpoint": "E6-CURRENT-PROOF"` and names
`currentProofAuthorityPath: …/v13-current-proof-authority-v1.json`. The
decision file also confirms the overlay's historical-preservation claim:
`historicalEvidencePreserved.historicalResult: "FAIL"`,
`historicalDecision: "REJECTED"`, `superseded: false`. **Verdict: SUPPORTED
(jointly by sidecar + decision file; nothing the JSONs contradict).**

### `v14-current-candidate-authority-v1.json`

| Claim (overlay) | Observed (JSON) | Verdict |
| --- | --- | --- |
| checkpoint `E6-CURRENT-CANDIDATE` | `"checkpointId": "E6-CURRENT-CANDIDATE"` | MATCH |
| predecessors `E6-REMEDIATION` and `E6-CURRENT-PROOF` | `"predecessors": ["E6-REMEDIATION", "E6-CURRENT-PROOF"]` | MATCH |
| current planning candidate `151f96519a30f1b16530851e73e51ac5ad74b355` | `"planningCandidate": "151f96519a30f1b16530851e73e51ac5ad74b355"`; commit verified an ancestor of HEAD (`29c56fa` is the rebind commit) | MATCH |
| `implementationHold: ACTIVE` | `"implementationHold": "ACTIVE"` | MATCH |
| Chain-hygiene claim: "the v14 pattern is normative — every future sidecar version pins its predecessor's SHA-256" | v14 `pointInTimePredecessor` pins v13 by path and `"sha256": "f2f02115…448138f"`, which equals the recomputed v13 digest | MATCH |

**Verdict: SUPPORTED — no claim the JSONs do not support.**

## 3. DC-7 ratified key grammar vs code

Source: `/home/administrator/projects/hexalith/conversations/src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs`.

| Grammar element (overlay) | Observed (code) | Verdict |
| --- | --- | --- |
| state store `statestore` | `StateStoreName = "statestore"` | MATCH |
| summary/detail key `projection:conversations:<segment(tenantId)>:<segment(conversationId)>` | `ConversationKeyPrefix = "projection:conversations:"`; `ConversationKey` = prefix + `EncodeKeySegment(tenantId)` + `:` + `EncodeKeySegment(conversationId)` — same composition order (tenant first) | MATCH |
| tenant index key `projection:conversations-index:<segment(tenantId)>` | `TenantIndexKeyPrefix = "projection:conversations-index:"`; `TenantIndexKey` = prefix + `EncodeKeySegment(tenantId)` | MATCH |
| dispatch-ledger key `projection:conversations-dispatch:<sha256-lowercase-hex(dispatchId)>` | `DispatchLedgerKeyPrefix = "projection:conversations-dispatch:"`; `SHA256.HashData(Encoding.UTF8.GetBytes(dispatchId))` + `Convert.ToHexStringLower(digest)` | MATCH |
| `segment` = unpadded base64url (`+`→`-`, `/`→`_`) over UTF-8 | `Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+','-').Replace('/','_')` | MATCH |

**Verdict: EXACT MATCH — no grammar divergence between overlay and code.**

## 4. Factual Refresh (dated 2026-08-18)

| Claim | Observed | Verdict |
| --- | --- | --- |
| `Hexalith.Builds` props pin Aspire `13.4.6` | `references/Hexalith.Builds/Props/Directory.Packages.props`: `Aspire.Hosting`, `Aspire.Hosting.Testing`, `Aspire.Azure.*`, `Aspire.Hosting.Redis/Docker/Azure.*` all `13.4.6`. (`Aspire.Hosting.Keycloak`/`Kubernetes` at `13.4.6-preview.1.26319.6` — same base; `CommunityToolkit.Aspire.Hosting.Dapr` at `13.4.1-beta.687` is a community package, not an Aspire pin.) | MATCH (see LOW obs. O-1) |
| … and Dapr `1.18.5` | `Dapr.Client`, `Dapr.AspNetCore`, `Dapr.Actors[.AspNetCore]`, `Dapr.Actors.Generators`, `Dapr.AI[.Microsoft.Extensions]`, `Dapr.Workflow` — all `1.18.5` | MATCH |
| `src/Hexalith.Conversations.ServiceDefaults/` removal completed | `src/` contains: Conversations, Admin.Web, AppHost, Client, Contracts, Server, Testing — no ServiceDefaults directory | MATCH |
| Root `.slnx` includes `Client.Tests` and `Admin.Web.Tests` | `Hexalith.Conversations.slnx` `/tests/` folder lists `Hexalith.Conversations.Client.Tests` (line 51) and `Hexalith.Conversations.Admin.Web.Tests` (line 49) | MATCH |
| … and does not yet include the module-internal-tier conformance project | Only `Hexalith.Conversations.Conformance.Tests` (line 52); no internal-tier conformance project present | MATCH |

## 5. V13 BEGIN marker predecessor pins and chain reproduction

Digest convention applied exactly as the overlay's normative grammar states:
block = first byte of BEGIN line through last byte of END line excluding the
END line's trailing LF; prefix = bytes 0 through the byte immediately before
the BEGIN line.

| Pin | Stated | Recomputed | Verdict |
| --- | --- | --- | --- |
| `v12-block-bytes` (V13 BEGIN) | `6075` | 6075 (lines 2248–2358) | MATCH |
| `v12-block-sha256` (V13 BEGIN) | `3050b326c5759fc51bc0e800944b0a1a591ab1782f6798f12abfdc10051b5796` | identical | MATCH |
| `v11-block-bytes/sha256` (V12 BEGIN) | `3042` / `a97385c1…a5a4c4d1` | 3042 / identical (lines 2194–2246) | MATCH |
| `v10-block-bytes/sha256` (V11 BEGIN) | `3846` / `893315bf…0a87700774b` | 3846 / identical (lines 2127–2192) | MATCH |
| `v9-block-bytes/sha256` (V10 BEGIN) | `18270` / `46862123…d4d3f3d9` | 18270 / identical (lines 1802–2125) | MATCH |
| `v8-prefix-sha256` (V9 BEGIN) | `7fd33168f34bb7d3326b4abb0eb79999270c11fefc7f50ec3acdd62fb1b86df5` | identical (119,265 prefix bytes before line 1802) | MATCH |

Structural grammar claims also verified: overlays are separated by exactly one
blank line (lines 2126, 2193, 2247, 2359 are all empty), and the file ends at
the V13 END line's trailing LF (`… -->\n`, nothing after). The V13 BEGIN
marker carries the full attribute set the grammar requires (`version`,
`epic-authority`, `supersedes`, `v12-block-bytes`, `v12-block-sha256`,
`candidate-binding`, `hold`). The V10 markers indeed carry no `hold=`, and the
V10 body states "The global implementation hold remains `ACTIVE`" — the
overlay's "schema gap, not a hold gap" characterization is supported.

**Verdict: the append disturbed nothing — the entire V8-prefix → V13 chain
reproduces byte-for-byte. For the record, the V13 block itself measures 13,530
bytes with SHA-256
`c5aab7cff4c063409fb5b11649e3228bc5fef0f5f12f7076d9bae243f36bc035` (working
tree; a future V14 marker must pin the committed bytes).**

## 6. DC-2 promoted lists vs in-document candidate lists

Promoted (overlay, lines 2462–2464): `TrustState` = `Unknown, Pending,
Verified, Contradicted, Stale, Redacted, Unavailable, Forbidden`;
`ProjectionFreshness` = `Current, Stale, Rebuilding, Unavailable, Forbidden,
Redacted`.

| Site | Document members | Comparison | Verdict |
| --- | --- | --- | --- |
| Line 554 (candidate canonical trust states) | Unknown, Pending, Verified, Contradicted, Stale, Redacted, Unavailable, Forbidden | 8/8, same members, same order | EXACT MATCH |
| Line 918 (candidate trust states) | Unknown, Pending, Verified, Contradicted, Stale, Redacted, Unavailable, Forbidden | 8/8, same members, same order | EXACT MATCH |
| Line 1211 (freshness states "such as") | Current, Stale, Rebuilding, Unavailable, Forbidden, Redacted | 6/6, same members, same order | EXACT MATCH |
| Lines 1277–1282 (Blocking Freshness minimum shape) | Current, Stale, Rebuilding, Unavailable, Forbidden, Redacted | 6/6, same members, same order | EXACT MATCH |

No silent additions, no drops, no reordering. The overlay's note that a name
appearing in both categories (`Stale`, `Unavailable`, `Forbidden`, `Redacted`)
denotes distinct typed members is a design ruling, not a factual claim, and is
consistent with both source lists.

## Ancillary claims spot-checked

- **Frontmatter provenance restatement:** frontmatter carries
  `authorityVersion: 'conversations-architecture-2026-08-01-v8'`,
  `currentExecutionView: …epic-6-current-execution-view-v1.md` (the frozen
  `v1` reference the overlay acknowledges), `baselineRevision`, `status`,
  `supersededAuthorityVersions`, `correctionAuthority` — all present and
  untouched, consistent with "v8-frozen, byte-pinned by the V9
  `v8-prefix-sha256`" (which reproduces). SUPPORTED.
- **DC-8 (ADR namespace):** `docs/adrs/` contains `0001`, `0002`,
  `0003-projection-read-store-population-proof.md` (matching "accepted `0003`
  is the projection read-store population proof") and no `0004` (consistent
  with "reserved"). SUPPORTED.
- **Naming-note disambiguation:** the sidecars are indeed a separate
  planning-authority version namespace (`v13-`/`v14-` files bind architecture
  `…-2026-08-04-v12` internally), so the overlay's namespace note is accurate
  and necessary. SUPPORTED.

## Observations (no factual error; recorded for completeness)

- **O-1 (LOW).** "Pins Aspire `13.4.6`" is true for every mainline Aspire
  package, but two hosting integrations (`Aspire.Hosting.Keycloak`,
  `Aspire.Hosting.Kubernetes`) are pinned at `13.4.6-preview.1.26319.6` and
  the community `CommunityToolkit.Aspire.Hosting.Dapr` at `13.4.1-beta.687`.
  The summary statement is fair; a future factual refresh could note the
  preview variants.
- **O-2 (LOW).** The marker-grammar section attributes the missing `hold=`
  attribute only to "the V10 markers", but the V9 BEGIN/END markers also carry
  no `hold=` (V9's body likewise states the hold is active, so it is equally a
  schema gap, not a hold gap). The statement made is true; it is merely not
  exhaustive about V9.

## Conclusion

All six mandated verification areas reproduce against the repository with zero
mismatches. The V13 overlay's committed factual claims are reality-backed, its
digest chain is intact, and its self-declared pre-publication state (staged,
uncommitted, bundle-drift acknowledged with a rebind obligation) matches the
observed git index exactly.
