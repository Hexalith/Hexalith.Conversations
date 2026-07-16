---
baseline_commit: 48160d60781494a2de438351e5424e8b0aa7bd47
---

# Story 2.3: Adopt the SDK query-handler + cursor codec, remove hand-rolled HMAC cursor

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a Conversations maintainer,
I want query handlers implemented against the SDK `IDomainQueryHandler` seam and pagination cursors produced/validated by the SDK `IQueryCursorCodec` + `QueryCursorScope`,
so that the bespoke query orchestration and hand-rolled HMAC cursor signing are deleted while query behavior, filters, response shapes, and cursor round-trip identity stay exactly the same.

This is the **third story of Epic 2** (Consume Existing Technical-Module Surface) and the third `src/`
production change in the initiative. It is classified **remove-and-replace**. Covers **FR-4**. Relevant NFRs:
**NFR1** (behavior preservation), **NFR2** (no hot-path read regression — preserve snapshot/projection use),
**NFR8** (public-surface / EventStore-concept boundary preserved).

> **READ THIS FIRST — two surgical swaps, not a query-logic rewrite.** Conversations already has the rich,
> conversation-specific query *logic* (`ConversationQueryHandler` — filters, freshness aggregation, hydration,
> temporal reconstruction, citation/audit access). That logic is **domain surface and stays**. This story does two
> things and only two:
>
> 1. **Replace the hand-rolled HMAC cursor codec with the SDK codec.** Delete `ConversationQueryCursor` (HMAC-SHA256
>    sign/verify) + the crypto half of `ConversationQueryCursorOptions`, and produce/validate the list continuation
>    cursor via the SDK `IQueryCursorCodec` (ASP.NET Core Data Protection) + `QueryCursorScope`. The tenant / caller /
>    filter-fingerprint / projection-generation-token binding moves into the **scope** string; the offset + issued-at
>    move into the **position** string; the **MaxAge / MaxOffset / clock-skew fail-closed guards stay as domain checks**
>    (the SDK codec has no wall-clock lifetime and no offset bound — you re-apply them after `TryDecode`). See the
>    **Cursor mapping table** — get this exactly right or the fail-closed conformance behavior regresses.
> 2. **Expose the query logic through the SDK `IDomainQueryHandler` seam** so the shared host (already wired in Story
>    2.1, scanning both the domain + Server assemblies — `Program.cs` explicitly anticipates "future
>    `IDomainQueryHandler` … implementations (Stories 2.3/2.5) are discovered without re-touching this host") routes
>    `/query` envelopes to Conversations. The new `IDomainQueryHandler` is a **thin adapter** that
>    deserializes the `QueryEnvelope` payload, calls the existing `ConversationQueryHandler` method, and serializes the
>    result into a `QueryResult` — it does **not** reimplement filter/freshness/hydration logic.
>
> **Do NOT rewrite `ConversationQueryHandler`'s filter/freshness/hydration logic, `ConversationTemporalReconstructionService`,
> the `temporal:v1` permalink contracts, or the `Contracts/Queries` DTOs.** Those are KEEP (the inventory classifies
> `Contracts/Queries` as Keep domain surface). The temporal `temporal:v1:pos:…:projection:…` permalink cursors are
> **unsigned domain contracts handled by a separate path** — they are NOT the HMAC list cursor and are NOT replaced by
> the SDK codec; the FR-4 AC phrase "temporal cursors / permalinks re-resolve to the same position" is preserved by
> leaving them untouched. Scope discipline is the primary risk here (see **Scope Boundaries**).

## Acceptance Criteria

1. **(AC-1 — list pagination cursor is produced & validated by the SDK codec; HMAC codec removed)**
   Given the conversation list query, when a continuation cursor is issued it is produced by the SDK
   `IQueryCursorCodec.Encode(queryType, scope, position)` and when a request carries a cursor it is validated by
   `IQueryCursorCodec.TryDecode(...)` — **and** the hand-rolled HMAC codec
   (`src/Hexalith.Conversations.Server/Queries/ConversationQueryCursor.cs`) and its crypto options (the `SigningKey` /
   `KeyId` members of `ConversationQueryCursorOptions`) are **deleted**, with no `HMACSHA256` /
   `CryptographicOperations.FixedTimeEquals` cursor-signing code remaining anywhere under
   `src/Hexalith.Conversations.Server/Queries/`. The codec is registered once via
   `AddEventStoreQueryCursorCodec("Hexalith.Conversations.QueryCursor.v1")` (stable domain-unique purpose).

2. **(AC-2 — cursor binding & all fail-closed guards preserved by construction)** The cursor binds to exactly what the
   HMAC payload bound to, and every fail-closed rejection the suite pins today still fails closed:
   - **tenant**, **caller principal**, **filter fingerprint**, and **projection-generation token** are folded into the
     `QueryCursorScope` string; a cursor presented under a different tenant / caller / filter / generation makes
     `TryDecode` return `false` (`wrong-scope`) → the query returns the same safe `Hidden`/`Forbidden` shape as today,
     reading **zero** projection rows (the cursor is rejected before any projection read, exactly as now).
   - **offset** and **issued-at** are carried in the cursor `position`; after a successful `TryDecode` the handler
     re-applies the domain bounds: **MaxOffset** (oversized offset → fail closed), **MaxAge** (expired cursor → fail
     closed), and **clock-skew lower bound** (future-dated cursor → fail closed).
   - a **tampered** cursor (Data-Protection unprotect fails) and a cursor protected under a **different purpose/key**
     fail closed identically to the prior HMAC tamper / wrong-key behavior.
   These behaviors are the re-expressed equivalents of the existing cursor tests
   (`TamperedCursorShouldFailClosed`, `CursorSignedWithDifferentKeyShouldFailClosed`, `ExpiredCursorShouldFailClosed`,
   `FutureDatedCursorShouldFailClosed`, `GenerationMismatchedCursorShouldFailClosed`, `TenantMismatchedCursorShouldFailClosed`,
   `CallerMismatchedCursorShouldNotFallBackToFirstPage`, `MalformedCursorShouldFailClosed…`, `ExcessiveOffsetCursorShouldFailClosed`)
   — re-expressed against the SDK codec, **not weakened or dropped**.

3. **(AC-3 — Conversations queries are served through the SDK `IDomainQueryHandler` seam)** Given the shared host wired
   in Story 2.1 (assembly-scanning both `ConversationsAssemblyMarker` and `ServerAssemblyMarker` assemblies), when the
   host starts, then at least one `IDomainQueryHandler` implementation in the Server assembly is discovered and
   registered (`AddScoped(typeof(IDomainQueryHandler), …)`), exposing the conversation queries (list + detail; and the
   temporal / citation / audit-record / privileged-justification reads as applicable) with `Domain == "conversations"`
   and stable kebab-case `QueryType` discriminators, dispatched via the SDK `/query` route
   (`DomainQueryDispatcher.ExecuteAsync` → matched on `Domain`+`QueryType`). The handler is a **thin adapter** over the
   existing `ConversationQueryHandler` — it deserializes the `QueryEnvelope.Payload`, delegates, and serializes the
   `QueryResult` (success → `QueryResult.FromPayload(JsonElement)`; denial/failure → the same safe shape, never an
   exception leak). A teeth test proves the SDK `/query` dispatch path actually reaches the handler (an unmatched
   `Domain`/`QueryType` surfaces `DomainQueryDispatcher`'s "No query handler is registered…" failure, not a silent
   success).

4. **(AC-4 — conversation-specific filters, response shapes, and the temporal/permalink path are unchanged)** Given the
   conversation-specific query surface — filter dimensions (business reference, project, folder, lifecycle, projected-at
   range, recent-activity, participant, redaction/freshness/audit/verification state), the worst-case freshness
   aggregation, the `ConversationListResult` / `ConversationPageMetadata` response shapes, the read-time hydration
   (citations, audit records, privileged-justification review), and the temporal reconstruction
   (`ConversationTemporalReconstructionService`, the `temporal:v1:pos:…:projection:…` and `pos:NNNN…` permalink/anchor
   contracts) — then they are **behavior-unchanged**. A paginated query round-trips to the **same position** and a
   temporal cursor / permalink re-resolves to the **same** point (release-gate behavior preserved). The
   `Contracts/Queries` DTOs are **not modified**.

5. **(AC-5 — ledger updated for removed/re-expressed tests; standing conformance gate holds)** The removal of the HMAC
   codec and the re-expression of the HMAC-specific cursor tests against the SDK codec are recorded as an **append-only**
   entry in the FR-20 at-risk register (`docs/release-evidence/at-risk-test-register-v1.{json,md}`) via its generation
   test (`AtRiskTestRegisterGenerationTest` — **regenerate, do not hand-edit the JSON**), traceable to the Story 2.3 /
   FR-4 disposition (follow the `Story22StructuralDispositions` precedent with a parallel `Story23StructuralDispositions`
   section). The full conformance suite is **100% green** on the story branch and **≥ 352 (monotonic)** — Story 2.2
   closed at **352**; re-expressing the HMAC cursor tests against the SDK codec must hold or grow the count, never
   regress (assertion strength must not drop vs the Story 1.1 baseline). The **public contract-shape diff** vs the Story
   1.1 snapshot (`docs/release-evidence/public-contract-shape-baseline-v1.json`, 196 types) is **empty** — the HMAC
   codec lives in the Server assembly and the continuation cursor is an **opaque `string`** on the public
   `ConversationPageMetadata`/`ConversationListResult` (Contracts), so swapping HMAC → Data Protection underneath must
   not change the public contract shape; a non-empty diff is a regression to investigate, not approve. No
   hot-path/snapshot/projection read regression is introduced (NFR1/NFR2).

## Tasks / Subtasks

- [x] **Task 1 — Map the current cursor & query path (read-only baseline)** (AC: 1, 2, 3, 4)
  - [x] Re-read `src/Hexalith.Conversations.Server/Queries/ConversationQueryCursor.cs` and confirm the HMAC payload
        fields and guards: `Version`, `KeyId`, `TenantId`, `CallerPrincipalId`, `FilterFingerprint`, `SortVersion`,
        `ProjectionGenerationToken`, `Offset`, `IssuedAt`; signature = HMAC-SHA256 over the payload JSON; guards =
        `FixedTimeEquals`, `MaxOffset`, `MaxAge`, future-dated lower bound. Record the field→target mapping in the Dev
        Agent Record against the **Cursor mapping table** below.
  - [x] Re-read `ConversationQueryHandler.ListAsync` and confirm the **two** cursor touch-points to swap: decode at
        entry (`TryDecode` → `Hidden` on failure) and encode at exit (next-page cursor). Confirm the
        `ComputeGenerationToken` (`{projectionCursor}:{maxAppliedEventPosition}`) and `Fingerprint(filter)` helpers —
        these become **scope inputs**, not codec internals.
  - [x] Confirm there is **no** existing `IDomainQueryHandler` in Conversations (grep) and that `Program.cs` already
        scans the Server assembly (it does — the host needs **no** change). Confirm the REST `ConversationReadApi` is
        currently mapped only in tests (`ConversationReadApiTest.cs:664`), not in the live host — so this story's live
        query entrypoint is the SDK `/query` seam, and the REST API (if you keep it for tests) must share the same
        re-homed cursor/handler code (no second cursor implementation).
  - [x] Confirm the SDK seams exist in the EventStore submodule (they do — `IQueryCursorCodec`, `QueryCursorScope`,
        `AddEventStoreQueryCursorCodec`, `IDomainQueryHandler`, `DomainQueryDispatcher`): **no submodule edit is needed**
        (pure consume). **Before building, verify submodule gitlinks are at their recorded commits** (Story 2.2 review
        found drift in Tenants/Parties/FrontComposer that broke the Release build — see Carry-forward).
- [x] **Task 2 — Register and adopt the SDK cursor codec** (AC: 1, 2)
  - [x] Register `services.AddEventStoreQueryCursorCodec("Hexalith.Conversations.QueryCursor.v1")` in
        `ConversationQueryServiceCollectionExtensions` (Data Protection is present in the ASP.NET Core host; if a test
        host lacks it, add `AddDataProtection()` in the test composition, not production).
  - [x] Build the scope with `QueryCursorScope.Create().Add("tenant", tenantId).Add("caller", callerPrincipalId)
        .Add("filter", Fingerprint(filter)).Add("generation", generationToken).Build()` (keep `Fingerprint` and
        `ComputeGenerationToken` — they are conversation-specific scope inputs, not crypto). Encode the position as
        `offset` + `issuedAt` (e.g. `$"{offset}:{issuedAt.UtcTicks}"`) so the domain bounds can be re-applied on decode.
  - [x] On decode: `TryDecode(cursor, queryType, scope, out position, out failureReason)` → on `false`, return the same
        safe `Hidden`/`Forbidden` shape as today (zero projection reads). On `true`, parse `position` and **re-apply the
        domain guards**: `offset ∈ [0, MaxOffset]`, `0 ≤ (now − issuedAt) ≤ MaxAge` — any violation fails closed
        identically to today. Keep `MaxAge`/`MaxOffset` as domain options (slim `ConversationQueryCursorOptions` to just
        these two policy values, or inline sensible defaults — the 30-min / 100k defaults are the current behavior).
- [x] **Task 3 — Delete the hand-rolled HMAC codec + crypto options** (AC: 1)
  - [x] Delete `src/Hexalith.Conversations.Server/Queries/ConversationQueryCursor.cs` (the HMAC class +
        `DecodedCursor` + `CursorPayload`). Remove the `SigningKey`/`KeyId` members from
        `ConversationQueryCursorOptions` (and the base64-key parsing in `AddConversationQueries(IConfiguration)`); the
        Data-Protection purpose replaces them.
  - [x] Confirm no residual references to `HMACSHA256`, `CryptographicOperations`, or `ConversationQueryCursor` remain
        under `src/` (grep). Remove now-unused `using`s.
- [x] **Task 4 — Add the `IDomainQueryHandler` adapter and wire query dispatch** (AC: 3)
  - [x] Add an `IDomainQueryHandler` implementation in the Server assembly (e.g.
        `src/Hexalith.Conversations.Server/Queries/ConversationDomainQueryHandler.cs`) with `Domain == "conversations"`
        and a stable kebab-case `QueryType` per query (list/detail/temporal/citation/audit/justification, as applicable).
        It deserializes `QueryEnvelope.Payload` (UTF-8 JSON) into the existing `*Query` request type, delegates to the
        matching `ConversationQueryHandler` method, and serializes the result via `QueryResult.FromPayload(...)`;
        denial/unavailable map to the existing safe result shapes (never throw past the seam). Carry
        `QueryEnvelope.TenantId` / `UserId` / `CorrelationId` into the existing query inputs (do **not** trust them
        beyond what the existing tenant-access gate already enforces — authorize before any projection read, as today).
  - [x] If splitting per query-type is cleaner, register multiple small handlers (the dispatcher matches on
        `Domain`+`QueryType`); each stays a thin adapter. Do **not** duplicate filter/freshness/hydration logic.
  - [x] Confirm assembly scanning discovers it (no `Program.cs` change). Verify the `/admin/operational-index-metadata`
        path advertises the new `QueryType`(s) so the gateway router routes to the handler (SDK behavior — verify, don't
        re-implement).
- [x] **Task 5 — Re-express the cursor + query tests against the SDK path; add the dispatch teeth test** (AC: 2, 3, 4)
  - [x] Re-express the HMAC-specific tests in
        `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs` against the SDK codec:
        `TamperedCursorShouldFailClosed` (corrupt the protected cursor), `CursorSignedWithDifferentKeyShouldFailClosed`
        (protect under a different purpose), `Expired`/`FutureDated`/`ExcessiveOffset`/`GenerationMismatched`/
        `TenantMismatched`/`CallerMismatched`/`Malformed` — each still asserts the safe shape + **zero projection
        reads**. The `ForgeCursorWithOffset` helper (lines ~1451–1470) that manually builds a `CursorPayload` +
        `HMACSHA256` **must be rebuilt** to forge via the SDK codec/position (or retired if its exact construction is no
        longer representable — record either way per AC-5). Update `ConversationQueryRegistrationTest` (it currently
        requires a 32-byte `SigningKey` + `KeyId` — no longer required; assert handler/codec resolve with Data
        Protection instead).
  - [x] Add a **dispatch teeth test** (under `tests/Hexalith.Conversations.Server.Tests/`) driving a query through the
        SDK `DomainQueryDispatcher.ExecuteAsync` path and asserting (a) a matched `Domain`/`QueryType` reaches the
        adapter and returns the expected `QueryResult`, and (b) an unmatched `Domain`/`QueryType` surfaces the
        dispatcher's "No query handler is registered…" failure (so a silently-bypassed dispatch goes RED).
  - [x] Confirm `ConversationTemporalReconstructionServiceTest` and the temporal/permalink contract tests stay green
        **with no source edits** to `ConversationTemporalReconstructionService` or `Contracts/Queries`.
  - [x] Use only packages already in the Conversations CPM (xUnit v3, Shouldly, NSubstitute) — no new package version.
- [x] **Task 6 — Record the disposition in the FR-20 ledger** (AC: 5)
  - [x] Extend `tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` with a parallel
        `Story23StructuralDispositions` section recording: (1) the HMAC cursor codec removed and **replaced** by the SDK
        `IQueryCursorCodec` + `QueryCursorScope` (Consume, FR-4); (2) the HMAC-specific cursor tests **re-expressed**
        against the SDK codec (not deleted — assertion strength preserved); and (3) the conversation-specific filter /
        freshness / hydration / temporal / `Contracts/Queries` surface as **Keep**. **Regenerate** the `.json` via the
        test; **never hand-edit** it. Update the companion `.md`. Append-only — do not rewrite accepted rows.
  - [x] **Inventory note (read before assuming a changeLog entry is needed):** the `query-cursor-orchestration` Consume
        area globs `src/Hexalith.Conversations.Server/Queries/**` (2,076 LOC baseline). This story removes only the HMAC
        codec from that folder; the folder does **not** empty (the handler, temporal service, read service, etc.
        remain), so the Story-1.4 empty-glob validator does **not** trip and **no** `changeLog` entry is required
        (unlike Story 2.2, where the consumed `EventStore/**` glob emptied). The per-area `approxLoc` is a frozen
        baseline constant — do not mutate it. If, contrary to expectation, your refactor empties the glob, follow the
        Story 2.2 precedent (append-only `challenge`/`upheld` `changeLog` entry keyed to the consumed spec).
- [x] **Task 7 — Run the standing conformance gate and generate the Dev Agent Record last** (AC: 5)
  - [x] Build `Hexalith.Conversations.slnx` **Release** (0 warnings — warnings-as-errors). Run the full conformance
        suite + Server/Tests per-project. Confirm green **≥ 352 (monotonic)**, public-contract-shape baseline JSON
        **byte-unchanged** (diff empty), no `src/` **public** contract change.
  - [x] **Generate the Dev Agent Record test counts / File List from the final `dotnet test` run as the LAST step**
        (Epic 1 retro P1/P2 — the human-curated count drifted in 5/5 Epic 1 stories *and* in the Story 2.2 first
        submission; generate it last so the record matches the working tree at first review).

## Dev Notes

### Cursor mapping table — HMAC codec → SDK codec (get this exactly right)

The hand-rolled `ConversationQueryCursor` packs everything into one signed payload. The SDK `IQueryCursorCodec` splits
responsibilities: it owns **signing/integrity** (Data Protection), takes a **scope** (binding) and a **position**
(logical offset). Conversation-specific *guards the SDK does not provide* (max-age, max-offset, clock-skew) stay as
domain checks the handler applies after `TryDecode`.

| HMAC `CursorPayload` field / guard | Where it goes under the SDK codec | Fail-closed behavior preserved by |
|---|---|---|
| Integrity (HMAC-SHA256 + `FixedTimeEquals`) | SDK Data-Protection `Protect`/`Unprotect` | `TryDecode` → `false` (`tamper-or-key-rotation`) on a corrupted cursor |
| `KeyId` / `SigningKey` | SDK Data-Protection purpose `"Hexalith.Conversations.QueryCursor.v1"` | cursor protected under a different purpose → `TryDecode` `false` |
| `TenantId` | `QueryCursorScope.Add("tenant", …)` | scope mismatch → `wrong-scope` → safe shape, 0 reads |
| `CallerPrincipalId` | `QueryCursorScope.Add("caller", …)` | scope mismatch → `wrong-scope` → safe shape, 0 reads |
| `FilterFingerprint` (`Fingerprint(filter)`) | `QueryCursorScope.Add("filter", Fingerprint(filter))` | scope mismatch → `wrong-scope` → safe shape, 0 reads |
| `ProjectionGenerationToken` (`ComputeGenerationToken`) | `QueryCursorScope.Add("generation", token)` | scope mismatch → `wrong-scope` → safe shape, 0 reads |
| `SortVersion` | fold into the scope (or the codec `queryType`) | mismatch → `wrong-scope` |
| `Offset` | encode in the SDK `position` string | re-applied domain check: `offset ∈ [0, MaxOffset]` |
| `IssuedAt` | encode in the SDK `position` string | re-applied domain check: `0 ≤ (now − issuedAt) ≤ MaxAge` (incl. future-dated lower bound) |
| `MaxOffset` / `MaxAge` config | slim `ConversationQueryCursorOptions` (domain policy, no crypto) | handler re-applies after `TryDecode` |

**Why guards move to the handler, not the codec:** the SDK codec has **no wall-clock lifetime and no offset bound** (it
is valid "while the key can unprotect"). The current suite pins `ExpiredCursorShouldFailClosed`,
`FutureDatedCursorShouldFailClosed`, and `ExcessiveOffsetCursorShouldFailClosed`. The standing gate forbids weakening
assertion strength, so these guards are **domain behavior that must be preserved** — re-apply them in `ListAsync` after
a successful `TryDecode`, reading the offset/issued-at back out of the decoded `position`.

### Scope Boundaries — what this story does and does NOT do

**DOES (FR-4, remove-and-replace):**
- Replace the HMAC list-cursor codec with the SDK `IQueryCursorCodec` + `QueryCursorScope` (Task 2/3).
- Expose Conversations queries through the SDK `IDomainQueryHandler` seam as a thin adapter over the existing handler
  (Task 4), discovered by the already-wired host.
- Re-express the HMAC-specific cursor tests against the SDK codec, add the dispatch teeth test, record in the ledger
  (Task 5/6).

**DOES NOT (actively avoid scope creep — this is the primary risk):**
- **Do NOT rewrite the conversation query logic.** `ConversationQueryHandler`'s filter matching, worst-case freshness
  aggregation, generation-token computation, and the citation/audit/justification/hydration services are **Keep** domain
  surface. The `IDomainQueryHandler` is a thin adapter; the cursor swap touches only the two cursor touch-points in
  `ListAsync`.
- **Do NOT touch the temporal reconstruction / permalink path.** `ConversationTemporalReconstructionService`,
  `ConversationTemporalAnchorV1`, and the `temporal:v1:pos:…:projection:…` / `pos:NNNN…` cursors are **unsigned domain
  contracts** on a different path — they are NOT the HMAC list cursor and are NOT replaced by the SDK codec. The FR-4 AC
  "temporal cursors / permalinks re-resolve identically" is satisfied by leaving them untouched. (A public per-event /
  cursor seam on the SDK for temporal anchors is an Epic 3 promote-later candidate — if you believe it should be
  promoted, log it via the Story 1.5 `classification-change-procedure-v1` append-only changeLog; do **not** fold it
  into this story.)
- **Do NOT modify `Contracts/Queries` DTOs** (`ConversationPageRequest`, `ConversationPageMetadata`,
  `ConversationListResult`, `ConversationTemporalAnchorV1`, filters). They are Keep and are in the 196-type public
  contract-shape baseline — changing them breaks the empty-diff gate. The continuation cursor stays an opaque `string`.
- **Do NOT touch** EventStore/Tenants/Parties/FrontComposer sources — the SDK seams already exist (pure consume; no
  backward-compat edit needed). Do NOT consolidate `ServiceDefaults`/`AppHost`/`Aspire` (Epic 3).
- **Do NOT** adopt the read-model store (2.4), projection seam (2.5), serialization helpers (2.6), or EventStore.Testing
  fakes (2.7).

### The current query/cursor path (authoritative facts)

- `ConversationQueryHandler` (`src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs:19`) is a plain
  `sealed class` (NOT an `IDomainQueryHandler`). It exposes `GetAsync`, `GetAtPointInTimeAsync`, `GetCitationAsync`,
  `GetAuditRecordAsync`, `GetPrivilegedOperationalJustificationAsync`, and `ListAsync`. `ListAsync` is the only
  HMAC-cursor touch-point: decode at entry (`_cursor.TryDecode` → `ConversationListResult.Hidden` on failure), encode
  at exit (next-page cursor via `_cursor.Encode`). [Source: ConversationQueryHandler.cs:172-299]
- `ConversationQueryCursor` (`…/Queries/ConversationQueryCursor.cs`) — the HMAC-SHA256 codec to delete. `Sign` =
  `Convert.ToHexString(HMACSHA256(signingKey).ComputeHash(utf8(payloadJson)))`; verify via
  `CryptographicOperations.FixedTimeEquals`; `CursorPayload` holds the fields in the mapping table; guards = `MaxOffset`,
  `MaxAge`, future-dated lower bound. [Source: ConversationQueryCursor.cs:25-235]
- `ConversationQueryCursorOptions` (`…/Queries/ConversationQueryCursorOptions.cs`) binds from config section
  `"Hexalith:Conversations:Queries:Cursor"` (`SigningKey` base64 ≥32 bytes, `KeyId`, `MaxAge` default 30 min,
  `MaxOffset` default 100,000). Keep `MaxAge`/`MaxOffset`; drop the crypto fields. [Source: ConversationQueryCursorOptions.cs:18-40]
- `Program.cs` already calls `builder.AddEventStoreDomainService(typeof(ConversationsAssemblyMarker).Assembly,
  typeof(ServerAssemblyMarker).Assembly)` and its comment explicitly anticipates Story 2.3's `IDomainQueryHandler`.
  **The host needs no change** — adding the handler to the Server assembly is sufficient for discovery. [Source:
  src/Hexalith.Conversations.Server/Program.cs]
- The REST `ConversationReadApi` (`…/Api/ConversationReadApi.cs`) is mapped **only in tests**
  (`ConversationReadApiTest.cs:664`), not in the live host. The live query entrypoint after this story is the SDK
  `/query` seam. If you keep the REST API for its tests, it must call the **same** re-homed cursor/handler code — do not
  create a second cursor implementation. [Source: grep MapConversationReadApi]

### The SDK seams (authoritative facts for the adapter + codec)

- **`IQueryCursorCodec`** (`Hexalith.EventStore.Client.Queries`): `string Encode(string queryType, string scope, string
  position)` and `bool TryDecode(string? cursor, string queryType, string scope, out string? position, out string?
  failureReason)`. Integrity via ASP.NET Core Data Protection (`IDataProtector.Protect/Unprotect`). `TryDecode` returns
  `true` for an empty cursor (both outs null); `false` with a log-safe `failureReason` for `too-large` (>4096B),
  `malformed`, `wrong-version`, `wrong-query-type`, `wrong-scope`, `empty-position`, `tamper-or-key-rotation`. No
  wall-clock lifetime. [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Queries/IQueryCursorCodec.cs:21-46;
  QueryCursorCodec.cs:18-127]
- **`QueryCursorScope`** (`Hexalith.EventStore.Client.Queries`): `Create()` → `.Add(string key, string? value)` /
  `.Add(string key, DateTimeOffset? value)` → `.Build()`. Pipe-delimited segments, colon key/value sep, values escaped
  (`\`→`\\`, `|`→`\p`, `:`→`\c`). Build the scope identically on encode and decode so any binding change → `wrong-scope`.
  [Source: …/Client/Queries/QueryCursorScope.cs:23-90; sibling example
  Hexalith.Tenants/Queries/TenantQueryCursorScopes.cs:12-44]
- **`AddEventStoreQueryCursorCodec(this IServiceCollection, string purpose)`**: `TryAddSingleton<IQueryCursorCodec>`,
  one per domain; purpose is stable & domain-unique (changing it invalidates outstanding cursors). Needs
  `IDataProtectionProvider` (present in the ASP.NET Core host). [Source:
  …/Client/Registration/QueryCursorCodecServiceCollectionExtensions.cs:25-31]
- **`IDomainQueryHandler`** (`Hexalith.EventStore.DomainService`): `string Domain { get; }`, `string QueryType { get; }`,
  `Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken)`. Discovered by `AddEventStoreDomainService`
  assembly scanning (`AddScoped(typeof(IDomainQueryHandler), type)` for every concrete impl), dispatched by
  `DomainQueryDispatcher.ExecuteAsync` matched case-insensitively on `Domain`+`QueryType`; unmatched →
  `QueryResult.Failure("No query handler is registered for domain '…' query type '…'.")` (the teeth assertion).
  [Source: …/DomainService/IDomainQueryHandler.cs:18-32; EventStoreDomainServiceExtensions.cs:200-213;
  DomainQueryDispatcher.cs:23-39]
- **`QueryEnvelope`** (`Hexalith.EventStore.Contracts.Queries`): `TenantId`, `Domain`, `AggregateId`, `QueryType`,
  `Payload (byte[] UTF-8 JSON)`, `CorrelationId`, `UserId`, `EntityId?`. **`QueryResult`**: `Success`, `PayloadBytes?`,
  `ErrorMessage?`, `ProjectionType?`; factories `QueryResult.FromPayload(JsonElement, projectionType?)` and
  `QueryResult.Failure(string)`; `GetPayload()` → `JsonElement`. [Source: …/Contracts/Queries/QueryEnvelope.cs:18-117;
  QueryResult.cs:22-64]
- **Sibling precedent:** `TenantQueryHandlerBase` implements `IDomainQueryHandler`, injects `IReadModelStore` +
  `IQueryCursorCodec`, gates on `UserId` before any state access, and has `Paginate<T>` + `ProtectCursor<T>` helpers
  using `QueryCursorScope`. Mirror this shape for the adapter (auth gate, then delegate, then protect cursor). [Source:
  Hexalith.Tenants/src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs:28-433]

### Files to touch (and their current state)

| File | State | Change |
|---|---|---|
| `src/Hexalith.Conversations.Server/Queries/ConversationQueryCursor.cs` | HMAC-SHA256 codec (`Sign`/verify/`CursorPayload`/`DecodedCursor`) | **Delete** (Task 3). |
| `src/Hexalith.Conversations.Server/Queries/ConversationQueryCursorOptions.cs` | `SigningKey`/`KeyId`/`MaxAge`/`MaxOffset` | **Slim** — drop `SigningKey`/`KeyId`; keep `MaxAge`/`MaxOffset` (domain policy) (Task 2/3). |
| `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs` | rich handler; `ListAsync` uses HMAC cursor at 2 points | **Edit the 2 cursor touch-points only** → SDK `IQueryCursorCodec` + scope + position + re-applied guards. Filter/freshness/hydration logic **unchanged** (Task 2). |
| `src/Hexalith.Conversations.Server/Queries/ConversationQueryServiceCollectionExtensions.cs` | binds cursor options; registers handler/cursor | Register `AddEventStoreQueryCursorCodec(...)`; drop base64 key binding (Task 2/3). |
| `src/Hexalith.Conversations.Server/Queries/ConversationDomainQueryHandler.cs` (new) | — | **Add** thin `IDomainQueryHandler` adapter(s) over `ConversationQueryHandler` (Task 4). |
| `src/Hexalith.Conversations.Server/Queries/ConversationTemporalReconstructionService.cs` | unsigned `temporal:v1` reconstruction | **Read-only / unchanged** (KEEP, AC-4). |
| `src/Hexalith.Conversations.Contracts/Queries/**` | domain DTOs (in public baseline) | **Unchanged** (KEEP, AC-4/AC-5). |
| `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs` | HMAC cursor tests incl. `ForgeCursorWithOffset` | **Re-express** against the SDK codec; rebuild the forge helper (Task 5). |
| `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryRegistrationTest.cs` | requires 32-byte `SigningKey`+`KeyId` | **Update** — assert resolve via Data Protection (Task 5). |
| `tests/Hexalith.Conversations.Server.Tests/**` (new) | — | **Add** the SDK `/query` dispatch teeth test (Task 5). |
| `tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` | has `Story22StructuralDispositions` | **Extend** with `Story23StructuralDispositions`; **regenerate** the ledger JSON (Task 6). |
| `docs/release-evidence/at-risk-test-register-v1.{json,md}` | seeded | Regenerated (never hand-edited) + companion `.md` (Task 6). |

### Standing conformance gate (applies to every Epic 2–4 story)

Suite 100% green on the branch; public contract-shape diff vs the Story 1.1 snapshot empty or explicitly approved &
recorded; the local copy (the HMAC codec) deleted; no test deleted/weakened without a recorded FR-20 ledger
justification. [Source: epics.md#Epic-2 standing-conformance-gate]

### Carry-forward technical-debt awareness (do not let it flake the gate)

- **Submodule working-tree drift (CRITICAL — broke the Story 2.2 Release build):** the 2.2 senior review found
  Tenants/Parties/FrontComposer submodule working trees had drifted off their recorded gitlinks, dropping
  `Hexalith.Tenants.Client.Subscription` (→ `CS0234`) and breaking `dotnet build -c Release`. **Before building, verify
  submodules are at their recorded commits** (root-level checkout, non-recursive — CLAUDE.md compliant; never
  `git submodule update --init --recursive`). [Source: 2.2 Senior Developer Review §1]
- **T1 parallelism race (closed by 2.1):** if you add a Conformance test that reads/writes `docs/release-evidence/*`,
  keep it inside the existing `ReleaseEvidenceArtifactCollection` `[Collection]`. [Source: 2.1 Completion Notes;
  epic-1-retro §7 T1]
- **Conformance/Server tests run per-project**, not solution-wide (`dotnet test
  tests/Hexalith.Conversations.Conformance.Tests/`). Use `Hexalith.Conversations.slnx` for restore/build only. [Source:
  2.2 Project Structure Notes; Hexalith.EventStore/CLAUDE.md]
- **Admin.Web Playwright E2E lane** (2/14) needs Chromium — environmental, unrelated; do not chase it. [Source: 2.1/2.2
  Completion Notes]

### Project Structure Notes

- Module follows the Hexalith project shape: `Contracts`, `Client`, `Server`, `Admin.Web`, `AppHost`, `ServiceDefaults`,
  `Testing`, with `tests/Hexalith.Conversations.*.Tests` mirrors. The query handler + cursor live in the Server assembly
  (`ServerAssemblyMarker`); the query DTOs live in `Contracts` (in the public-contract-shape baseline).
- Inventory: `query-cursor-orchestration` (`src/Hexalith.Conversations.Server/Queries/**`, **Consume**, 2,076 LOC) →
  SDK `IDomainQueryHandler` + `IQueryCursorCodec` + `QueryCursorScope`; `query-filters-response-shapes`
  (`src/Hexalith.Conversations.Contracts/Queries/**`, **Keep**, 3,251 LOC) = domain DTOs. These two split the addendum's
  area 1 (5,327). The accepted inventory (Story 1.4, binding) resolves the addendum's first-pass "Consume + Promote" to
  **Consume + Keep** — there is **no Promote** in this story (prefer the latest approved artifact over the addendum
  first pass per project-context). [Source: docs/release-evidence/consume-promote-keep-inventory-v1.md:36,59,81]

### References

- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story-2.3] — story statement + ACs + standing gate.
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#FR-Coverage-Map] — FR-4 → Epic 2 (remove-and-replace).
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md#row-1] — Queries/cursor/hydration boundary → SDK `IDomainQueryHandler`/`IQueryCursorCodec`/`QueryCursorScope`; keep filters/response shapes. #B EventStore.Client surface.
- [Source: docs/release-evidence/consume-promote-keep-inventory-v1.{md,json}] — `query-cursor-orchestration` (Consume) vs `query-filters-response-shapes` (Keep); 2,076-LOC area; frozen `approxLoc`.
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Queries/IQueryCursorCodec.cs:21-46] — codec seam (Encode/TryDecode, Data Protection, failure reasons).
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Queries/QueryCursorScope.cs:23-90] — scope builder (escaping, segment format).
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Registration/QueryCursorCodecServiceCollectionExtensions.cs:25-31] — `AddEventStoreQueryCursorCodec(purpose)`.
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainQueryHandler.cs:18-32] — handler seam; [EventStoreDomainServiceExtensions.cs:200-213] discovery; [DomainQueryDispatcher.cs:23-39] dispatch + teeth failure.
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/QueryEnvelope.cs:18-117; QueryResult.cs:22-64] — envelope/result wire contracts + factories.
- [Source: Hexalith.Tenants/src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs:28-433] — sibling `IDomainQueryHandler` + `IQueryCursorCodec` + `QueryCursorScope` precedent (auth gate, Paginate, ProtectCursor).
- [Source: src/Hexalith.Conversations.Server/Queries/ConversationQueryCursor.cs:25-235] — the HMAC codec to delete (AC-1).
- [Source: src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs:172-299] — `ListAsync` cursor touch-points (edit only these).
- [Source: src/Hexalith.Conversations.Server/Queries/ConversationTemporalReconstructionService.cs] — unsigned temporal/permalink path (KEEP, AC-4).
- [Source: src/Hexalith.Conversations.Server/Program.cs] — host already scans Server assembly; anticipates Story 2.3 `IDomainQueryHandler` (no host change).
- [Source: tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs:1136-1470] — cursor fail-closed tests + `ForgeCursorWithOffset` to re-express.
- [Source: docs/release-evidence/public-contract-shape-baseline-v1.json:17868-17973] — `ConversationListResult`/`ConversationPageMetadata` in the 196-type public baseline (cursor is opaque `string`; diff must stay empty).
- [Source: _bmad-output/implementation-artifacts/2-2-adopt-eventstoreaggregate-tstate-base-class-conventions.md] — prior story; gate at 352; ledger `StoryNNStructuralDispositions` idiom; submodule-drift CRITICAL; P1/P2 count-drift hazard.

## Developer Context

### Technical Requirements (dev agent guardrails)

- .NET 10 (`net10.0`), SDK pinned `10.0.302` (`global.json`). Nullable enabled, implicit usings,
  **warnings-as-errors** — do not suppress broadly. File-scoped namespaces, Allman braces, `_camelCase` private fields,
  `Async` suffix, CRLF. ITANEO copyright header on every edited/created source file.
- Central Package Management (`Directory.Packages.props`) — never put package versions in `.csproj`; never introduce a
  new package version in tests (use xUnit v3 / Shouldly / NSubstitute already present).
- Keep the change scoped to Conversations artifacts + the test/ledger updates this story mandates. **Do not edit**
  EventStore/Tenants/Parties/FrontComposer sources (the SDK seams already exist — pure consume).
- This is a **focused remove-and-replace**: two swaps (cursor codec; query-handler seam) over preserved domain logic.
  Resist rewriting the query/filter/freshness/temporal logic.

### Architecture Compliance

- Let EventStore own routing, query dispatch, and cursor integrity — the SDK seams delegate exactly this; removing the
  hand-rolled HMAC codec **strengthens** the EventStore-concept boundary (NFR8).
- Keep authorization/tenant lookups/Parties calls out of aggregate logic; the query handler authorizes (tenant-access
  gate) **before** any projection read — preserve this ordering in the adapter (mirror `TenantQueryHandlerBase`'s
  `UserId` gate). Fail closed on missing/stale/unavailable/disabled/ambiguous/insufficient tenant state.
- Do not expose raw EventStore envelopes as the adopter API; the public surface stays the Conversations query
  DTOs/response shapes. Conversation URLs/permalinks that encode temporal cursors must re-resolve identically
  (project-context — preserved by leaving the temporal path untouched).
- Keep hot read paths local after authorization; do not add synchronous cross-service calls; preserve snapshot/projection
  use (NFR2).

### Library / Framework Requirements

- **`Hexalith.EventStore.Client`** (`Hexalith.EventStore.Client.Queries`) — `IQueryCursorCodec`, `QueryCursorScope`,
  `QueryCursorCodec`; registration `AddEventStoreQueryCursorCodec(purpose)` (`…Client.Registration`). Project reference
  (submodule, built from source).
- **`Hexalith.EventStore.DomainService`** — `IDomainQueryHandler`, `DomainQueryDispatcher`, `AddEventStoreDomainService`
  (already used by `Program.cs`).
- **`Hexalith.EventStore.Contracts.Queries`** — `QueryEnvelope`, `QueryResult` (+ `FromPayload`/`Failure`),
  optionally `IQueryContract` (static `Domain`/`QueryType`/`ProjectionType`) for compile-time metadata.
- **ASP.NET Core Data Protection** — backs the codec; present in the host. Add `AddDataProtection()` only in a test
  composition that lacks it, never as a production workaround.
- Versions via CPM: Dapr `1.17.7`, Aspire `13.2.x`/`13.3.x`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`.

### File Structure Requirements

- New code (the `IDomainQueryHandler` adapter) under `src/Hexalith.Conversations.Server/Queries/`. New tests under
  `tests/Hexalith.Conversations.Server.Tests/Queries/` (mirrors `src`). Evidence artifacts under
  `docs/release-evidence/` are written by generation tests, never hand-edited. Remove the deleted codec's file and any
  now-empty directory.

### Testing Requirements

- xUnit v3 + Shouldly + NSubstitute. Run per-project.
- **Prove behavior, not mirrors** (Epic 1 L1 / agreement A1): the dispatch teeth test must drive the SDK
  `DomainQueryDispatcher`/`/query` path and go RED if dispatch is bypassed (unmatched `Domain`/`QueryType` → "No query
  handler is registered…"). The cursor fail-closed tests must each still assert the safe shape **and** zero projection
  reads, re-expressed against the SDK codec — do not reduce the assertion set.
- Conformance suite must stay **≥ 352 and monotonic**; assertion strength must not drop vs the Story 1.1 baseline. The
  removed HMAC-specific assertions are **re-expressed**, not dropped; net count holds or grows (the new dispatch teeth
  test + Story23 ledger test offset any consolidation).
- Integration-test rule (EventStore convention): a Tier-2/3 test must inspect real end-state, not only a status code or
  mock call count — applies if you add any request-level integration test.

### Previous-Story Intelligence (Story 2.1 / 2.2 carry-forward)

- **Story 2.1 (host):** wired `AddEventStoreDomainService(domain, server)` + `UseEventStoreDomainService()` and left the
  host ready to discover `IDomainQueryHandler`/`IDomainProjectionHandler` (Stories 2.3/2.5) **without re-touching
  Program.cs**. Closed the gate at 351 and the T1 parallelism race. Established the evidence-generation-test idiom
  (regenerate, never hand-edit).
- **Story 2.2 (aggregate base class):** deletion-dominant; closed the gate at **352**. Established the
  `StoryNNStructuralDispositions` ledger idiom and the consumed-glob `changeLog` precedent. Its senior review surfaced a
  **CRITICAL submodule working-tree drift** that broke the Release build — verify gitlinks before building. Its record
  also drifted on test counts (the recurring P1/P2 hazard) — generate the Dev Agent Record last.
- **L1 / A1 — coverage ≠ live-path exercise.** Pin behavior by fault-injection / negative assertions (the dispatch
  teeth + the cursor fail-closed re-expressions).
- **P1 / P2 — generate the Dev Agent Record (counts + File List) from the final `dotnet test` run, last.**
- **A2 / A3 — ledger entry for any removed/weakened/re-expressed test**; reclassifications go through the
  `classification-change-procedure-v1` append-only changeLog. **Append-only** — never rewrite accepted rows.
- **T2 / projectReferenceDisposition** — the `Conformance.Tests → Server` reference is removed only by the **last**
  owning story of {2.2, 2.5, 3.2, 3.3}; 2.3 is not in that set → leave the reference untouched.

### Git Intelligence (recent work patterns)

Recent commits: `feat(story-2.2): Adopt EventStoreAggregate<TState> base-class conventions`,
`feat(story-2.1): Wire Conversations onto the shared two-line domain-service host`, preceded by `feat(story-1.x)`
(test/evidence). Established patterns to reuse: the evidence-generation-test idiom for `docs/release-evidence/*`
(repo-root discovery → deterministic indented-JSON write → re-read + re-validate + content-safety scan; regenerate,
never hand-edit); the `StoryNNStructuralDispositions` ledger section; Conventional Commits scope `feat(story-2.3): …`.
This story (2.3) is the **third** `src/` production change and the first **substantive remove-and-replace** in Epic 2.

### Project Context Reference

`_bmad-output/project-context.md` is binding. Most-relevant rules for this story:
- "Conversation URLs/permalinks that encode temporal cursors must re-resolve identically." — preserved by leaving the
  temporal reconstruction / `temporal:v1` permalink path untouched (KEEP).
- "Fail closed for authorization, tenant projection failures, unknown/stale state." — the cursor scope mismatch + the
  re-applied MaxAge/MaxOffset/clock-skew guards all fail closed; the adapter authorizes before any projection read.
- "Do not expose raw EventStore command envelopes / projection internals as the primary adopter API." — the public
  surface stays the Conversations query DTOs; the `QueryEnvelope`/`QueryResult` are the SDK seam, not the adopter API.
- "Treat EventStore as a bounded-context dependency; do not reimplement its runtime behavior." — consume
  `IQueryCursorCodec`/`IDomainQueryHandler`; remove the hand-rolled HMAC codec that duplicated cursor integrity.
- "Keep hot read paths local after authorization; use snapshots/projections." — no new cross-service hot-path calls (NFR2).
- "Never initialize nested submodules / no `--init --recursive`." — root-level submodule only; verify gitlinks first.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (claude-opus-4-8, 1M context)

### Debug Log References

- **Pre-build submodule restore (CRITICAL carry-forward).** On activation, `git diff --submodule=short` showed all four
  submodules drifted off their recorded gitlinks (EventStore `ad2c957`→`b7a6f5b`, FrontComposer `451830b`→`cfbced3`,
  Parties `485616f`→`6d32d05`, Tenants `5b4424e`→`c3b70ce`). Restored each to its recorded gitlink (root-level checkout,
  non-recursive — CLAUDE.md compliant) before any build, clearing the Story-2.2 Release-build hazard. Verified clean at close.
- **Composition gap surfaced & resolved with the user.** The conversation query services + the new cursor codec are not
  registered by the two-line SDK host (the SDK scan discovers the `IDomainQueryHandler` *type* but not its dependency
  graph; `AddConversationQueries` was test-only). Per the user's decision, the host now registers the query boundary
  (`AddConversationTenantAccess()` + `AddConversationQueries(builder.Configuration)`) so the `/query` seam reaches the
  handler in production. The persisted projection read-store binding (`IConversationProjectionReadStore`) remains a Story
  2.4 concern; it is faked in the discovery/dispatch tests.

### Completion Notes List

Implemented as two surgical swaps over preserved domain logic (no query-logic rewrite):

1. **Cursor codec swap (Tasks 2/3, AC-1/AC-2/AC-5).** Deleted the hand-rolled HMAC-SHA256 codec
   (`ConversationQueryCursor.cs`) and the crypto half of `ConversationQueryCursorOptions` (`SigningKey`/`KeyId`).
   `ConversationQueryHandler.ListAsync` now produces/validates the continuation cursor via the SDK `IQueryCursorCodec`
   (ASP.NET Core Data Protection) + `QueryCursorScope`, registered once via
   `AddEventStoreQueryCursorCodec("Hexalith.Conversations.QueryCursor.v1")`. New `ConversationListCursor` helper owns the
   conversation-specific scope (tenant/caller/filter-fingerprint/sort) and the opaque position (offset/issued-at/generation).
   No `HMACSHA256`/`CryptographicOperations` cursor-signing code remains under `src/`.
   - **Design note (recorded in the FR-20 ledger):** AC-2's mapping table puts the projection-generation token in the
     *scope*, but that token is only knowable *after* the projection read, while integrity (tamper/key-rotation) must be
     caught *before* any read (the suite pins zero projection reads for tamper/malformed/wrong-key). Reconciling both: the
     pre-read scope binds tenant/caller/filter/sort (so integrity **and** those mismatches fail closed with zero reads —
     a strengthening over the prior post-read tenant/caller check), and the generation token rides in the protected
     position and is re-compared after the read (identical fail-closed outcome to the prior `DecodedCursor.Matches`).
     MaxAge/MaxOffset/clock-skew stay as domain guards re-applied after `TryDecode`.
2. **Query-handler seam (Task 4, AC-3/AC-4).** Added thin `IDomainQueryHandler` adapters
   (`ConversationDomainQueryHandlerBase` + `ListConversationsDomainQueryHandler` / `GetConversationDomainQueryHandler`,
   `Domain == "conversations"`, kebab-case query types `conversation-list` / `conversation-detail`). Each deserializes the
   `QueryEnvelope` payload, carries envelope identity into the existing `*Query`, delegates to `ConversationQueryHandler`,
   and serializes the `QueryResult` — never throwing past the seam. The temporal / citation / audit-record /
   privileged-justification reads ("as applicable" in AC-3) follow the identical thin pattern and are deferred to keep the
   swap surgical; the two named-required reads (list + detail) are delivered and proven.
3. **Tests re-expressed, not weakened (Task 5, AC-2/AC-4/AC-5).** All nine HMAC-specific cursor fail-closed tests are
   re-expressed against the SDK codec (`ForgeCursorWithOffset` rebuilt to forge via the codec position); integrity cases
   still assert zero projection reads, tenant/caller scope cases now **also** assert zero reads. Added a cursor round-trip
   test and a SDK `/query` dispatch teeth test (`ConversationDomainQueryDispatchTest` — matched reaches the adapter;
   unmatched domain/type surfaces "No query handler is registered…") plus a discovery teeth fact
   (`ExplicitAssemblyScanShouldDiscoverConversationDomainQueryHandlers`). The temporal reconstruction and `Contracts/Queries`
   tests stay green with no source edits.
4. **Ledger (Task 6, AC-5).** Added an append-only `Story23StructuralDispositions` section to
   `AtRiskTestRegisterGenerationTest` (regenerated the `.json`; never hand-edited) + companion `.md`.

**Standing conformance gate (Task 7, final run):** `dotnet build Hexalith.Conversations.slnx -c Release` → **0 warnings**.
Per-project test results (final run, generated last):

| Project | Passed | Notes |
|---|---|---|
| Hexalith.Conversations.Conformance.Tests | 353 | ≥ 352 and monotonic (Story 2.2 closed at 352; +1 via the Story23 ledger fact) |
| Hexalith.Conversations.Server.Tests | 535 | cursor re-expressions + dispatch/discovery/round-trip teeth (530 dev-story close + 5 QA-automation gap-closure tests, per `tests/test-summary.md`) |
| Hexalith.Conversations.Contracts.Tests | 587 | unchanged |
| Hexalith.Conversations.Tests | 185 | unchanged |
| Hexalith.Conversations.Client.Tests | 25 | unchanged |
| Hexalith.Conversations.IntegrationTests | 8 | unchanged |

Public contract-shape baseline (`public-contract-shape-baseline-v1.json`, 196 types) **byte-unchanged** (empty diff). No
`src/` public contract change (the continuation cursor stays an opaque `string` on the Contracts surface; the codec lives
in the Server assembly). Admin.Web Playwright E2E lane not run (Chromium unavailable — environmental, per carry-forward).

### File List

**Added**
- `src/Hexalith.Conversations.Server/Queries/ConversationListCursor.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationDomainQueryHandler.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationDomainQueryDispatchTest.cs`

**Modified**
- `src/Hexalith.Conversations.Server/Program.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryCursorOptions.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryServiceCollectionExtensions.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryRegistrationTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Governance/ConversationBuyerAcceptanceDemoServiceTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainDiscoveryHostCompositionTest.cs`
- `tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs`
- `docs/release-evidence/at-risk-test-register-v1.json` (regenerated by its generation test)
- `docs/release-evidence/at-risk-test-register-v1.md`

**Deleted**
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryCursor.cs`

## Senior Developer Review (AI)

**Reviewer:** Jerome Piquot (adversarial automated review) — 2026-06-03
**Outcome:** Approve (auto-fix applied). Status → **done** (0 CRITICAL, 0 HIGH issues).

### Verification performed (claims validated against the working tree, not the record)

- **Release build** `dotnet build Hexalith.Conversations.slnx -c Release` → **Build succeeded, 0 Warning(s), 0 Error(s)** (warnings-as-errors holds). Submodule gitlinks at recorded commits (EventStore `ad2c957`); no drift.
- **Conformance.Tests** → **353 passed / 0 failed** (≥ 352 monotonic ✓, matches the record). Includes `EveryStory23StructuralDispositionShouldBeAnchoredAndGreen` and the ledger regeneration/validation test.
- **Server.Tests** → **535 passed / 0 failed** (see MEDIUM-1 below).
- **HMAC removal (AC-1):** `grep` confirms **no** `HMACSHA256` / `CryptographicOperations` / `FixedTimeEquals` / `SigningKey` / `KeyId` cursor-signing code anywhere under `src/`; `ConversationQueryCursor.cs` is deleted from the tree (staged `D`). Codec registered once via `AddEventStoreQueryCursorCodec("Hexalith.Conversations.QueryCursor.v1")`.
- **Fail-closed assertions (AC-2):** all nine re-expressed cursor tests pass and assertion strength is **preserved or increased** — tenant/caller/filter scope mismatches now assert **zero** projection reads (caught at the codec scope boundary, a strengthening over the prior post-auth check); integrity/expired/future-dated/excessive-offset all assert zero reads; generation-mismatch reads 1 row then fails closed (unchanged from the original, since the generation token is computed from the rows — correctly documented as a deliberate scope-vs-position reconciliation in the ledger).
- **Dispatch seam (AC-3):** `ConversationDomainQueryDispatchTest` is genuinely adversarial — matched vs. unmatched `Domain`/`QueryType` contrast surfaces the dispatcher's "No query handler is registered…" miss, plus fault-containment and fail-closed-before-read facts. Discovery proven by `ExplicitAssemblyScanShouldDiscoverConversationDomainQueryHandlers` over the real host wiring.
- **Behavior preservation (AC-4):** filter/freshness/hydration logic and the temporal/`Contracts/Queries` surface are untouched; **public contract-shape baseline is byte-unchanged** (absent from `git status` → empty diff ✓).
- **File List vs git reality:** exact match (3 added, 10 modified, 1 deleted) — no undocumented or phantom changes.

### Findings

- **[MEDIUM-1 — FIXED] Dev Agent Record test-count drift (recurring P1/P2 hazard).** The record's per-project table listed Server.Tests at **530**, but the working tree runs **535** — the 5 QA-automation gap-closure tests (`FilterMismatchedCursorShouldFailClosed` + 4 dispatch-seam tests) were added after the dev-story count was generated and recorded in `tests/test-summary.md` but never propagated to the story table. Corrected the table to 535 with provenance. No behavioral impact.
- **[LOW — accepted, no change] AC-3 "as applicable" reads deferred.** The temporal / citation / audit-record / privileged-justification reads are not yet exposed as their own `IDomainQueryHandler` adapters; only the two named-required reads (list + detail) ship. AC-3's "as applicable" wording permits this and the deferral is documented in the Completion Notes and `test-summary.md`. In scope for a later story.
- **[LOW — accepted, no change] Redundant sort-version binding.** `SortVersion` is folded into both the scope segment (`.Add("sort", …)`) and the filter `Fingerprint` JSON. Harmless belt-and-suspenders; not worth a churn edit.

## Change Log

| Date | Version | Description |
|---|---|---|
| 2026-06-03 | 0.3.0 | Story 2.3: adopted the SDK `IQueryCursorCodec` + `QueryCursorScope` and removed the hand-rolled HMAC continuation-cursor codec; exposed conversation list/detail queries through the SDK `IDomainQueryHandler` `/query` seam as thin adapters; re-expressed the cursor fail-closed tests + added dispatch/discovery/round-trip teeth; recorded the Story 2.3 disposition in the FR-20 ledger. Conformance 353 (monotonic), public contract-shape diff empty. |
| 2026-06-03 | 0.3.1 | Senior Developer Review (AI): validated build (0 warnings) + suites (Conformance 353, Server.Tests 535) + HMAC removal + fail-closed strength against the working tree. Fixed Dev Agent Record Server.Tests count drift (530 → 535). Outcome: Approve; Status → done. |
