---
title: 'Prove the packaged Conversations client against its supported host'
type: 'feature'
created: '2026-11-22'
status: 'in-progress'
route: 'dispatch'
baseline_commit: 'a252b7f6cc7f758a7d10bdc745caade0c77cabe1'
review_loop_iteration: 1
context:
  - '_bmad-output/planning-artifacts/architecture.md'
  - 'docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="human-owned intent">

## Intent

A consumer using the v1 client needs an authenticated service address for create, append, detail and list. The user selected a Conversations-owned external API hosted by Platform. Platform supplies orchestration and identity configuration. EventStore handles admission, persistence, projection delivery and query dispatch.

## Boundaries & Constraints

Expose only the four pilot operations from the external host. The domain workload keeps its internal routes. Authenticated claims bind tenant and principal. The command owner must independently establish the human Party and current tenant write access. Use a narrowly scoped operator-provisioned tenant/principal-to-Party binding for the pilot unless a production authority already supplies that evidence. A missing binding fails closed. The existing independent authority continues to govern restricted operations.

Use the installed Client package, a real EventStore command bus and its persisted read model. Keep keys distinct across logical commands and preserve the entire original request on retry. Use a locally launched deployment for qualification. Publishing packages, pushing revisions, granting live production access and rolling out releases are outside this change.

## I/O & Edge-Case Matrix

| Scenario | Expected behavior |
|---|---|
| Valid create followed by identical retry | Both results identify the same tenant and conversation. |
| Valid append followed by identical retry | Both address the same conversation. The projection contains one message with the submitted ID, text and author. |
| Reused mutation key with changed intent | A conflict cannot create an additional effect. |
| Authorized read during projection lag | Poll until Current detail and list agree, within a finite deadline. |
| Credential and requested scope disagree | Hidden detail returns 404 without detail. Hidden list returns 200 without rows. Neither response discloses private content. |
| Independently authenticated caller in another tenant | The protected conversation is invisible to the caller. Missing credentials fail authentication. |

</frozen-after-approval>

## Code Map

- The client already emits the four versioned HTTP requests. Existing command and read endpoint helpers in `src/Hexalith.Conversations.Server/Api/` define their v1 shapes, filter parsing and safe results. Reuse operation-specific behavior without exposing additional route groups.
- `ConversationsCatalogDeclarations`, `ConversationsCatalogAdapters`, `ConversationAgentAdmissionStage` and `ConversationAggregate` register, authorize and execute commands. The agent append has separate provenance rules. The existing message event feeds both aggregate replay and the persisted timeline.
- EventStore supplies its human bearer policy and gateway client. The existing tenant API demonstrates bearer forwarding through Dapr. The Conversations domain query handlers already read the authorized projection. Platform currently registers only the domain workload.
- The gateway test fixture replaces authority and tenant access. Its direct actor/projection calls cannot establish the adopter journey. The package consumer validator already demonstrates isolated package installation.

## Tasks & Acceptance

- [x] Implement the external host in `src/Hexalith.Conversations.Api/` and enroll it in `Hexalith.Conversations.slnx`. Dispatch human commands and queries through EventStore. Reject conflicting scope before downstream work and preserve typed outcomes.
- [x] Add ordinary append under `src/Hexalith.Conversations/Commands/` and the aggregate, catalog, semantic intent adapter and admission code. Bind each command to the authenticated tenant, actor and aggregate. Validate write access, message ownership, lifecycle and retry semantics.
- [x] Enroll the host in `module/hexalith.module-manifest.v2.json`, Platform composition and the EventStore-owned invocation policy. Configure public bearer authentication, gateway discovery and narrowly scoped grants through the owning repositories.
- [x] Add a package-only consumer under `tests/`. Start the real composition, provision tenant and Party authorization, use two authenticated tenants, and verify the public responses and independently persisted state. Document the resource address, credentials contract, prerequisites and reproduction command in `docs/integration-guide.md`.
- [x] Update the deployment diagram and story status to match the verification result. Preserve predecessor evidence and record any unverified deployment boundary.

**Acceptance criteria**

1. Given the composed services and installed package, when the authorized adopter retries create and append and polls the documented HTTP routes, then both reads reach Current within the deadline and the persisted conversation contains exactly one message.
2. Given the same host and protected conversation, when a mismatched identity or a different tenant requests it, then the operation returns its hidden contract with no private data.
3. Given a missing key, denied identity, unavailable dependency or changed intent on retry, when the API receives the request, then it preserves the appropriate public failure and adds no unauthorized or duplicate mutation.

### Review Findings

**November 22, 2026 — changes requested.** Reviewed the story baseline `a252b7f6` through Conversations `278f43ef`, including the implementing changes in Platform `a03bf4aa` and EventStore `27562656`. The local server test project built in Release, 99 selected pilot, query and catalog tests passed, and the package-only journey compiled against newly packed Client and Contracts packages in an empty cache. The live two-tenant journey requires operator credentials and a deployed host. Its earlier result is recorded below.

Status on November 25, 2026: fourteen patches were resolved by the follow-up specs cited below. The last two, concurrent append occurrences and command-status retention, were resolved in review loop 1 and are not yet committed. The typed-mutation-error finding is deferred to Story 9.4.

#### Product and build issues

- [x] [Review][Patch] **High — valid concurrent appends can fail permanently.** The API stamps each append before dispatch. In a local check, two distinct valid appends executed in reverse timestamp order produced a message event followed by `command_validation_failed`. The second timestamp preceded `LastEventAt`. A separate strict comparison to the domain clock also rejects create and append when the API clock is ahead. Assign authoritative occurrences at the serialized write boundary and cover reordered requests and independent clocks. [PilotCommands.cs:44](../../src/Hexalith.Conversations.Api/PilotCommands.cs#L44) · [ConversationAggregate.cs:118](../../src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs#L118) · [admission clock check](../../src/Hexalith.Conversations.Server/Agents/ConversationAgentAdmissionStage.cs#L98) **Resolved 2026-11-25 in the uncommitted working tree.** The aggregate commits an ordinary append at the later of its assigned `PostedAt` and `LastEventAt` and keeps the year floor. Admission reads an injected `TimeProvider` and accepts pilot occurrences up to the one-minute platform bearer skew.
- [x] [Review][Patch] **High — unhealthy tenant authorization produces a distinguishable public response.** A retryable tenant-access denial triggers pre-processing deferral and a public 503. An unknown tenant or missing Party binding returns 403. AD-6 requires the same public denial for unsafe and unknown tenant state. Preserve the gateway’s recoverable key internally and translate the specific pre-effect authority response at the public API. Keep unknown post-submission outcomes distinct. [ConversationPilotWriteAdmission.cs:36](../../src/Hexalith.Conversations.Server/Agents/ConversationPilotWriteAdmission.cs#L36) · [AD-6](../planning-artifacts/architecture.md#current-planning-entry-point) **Resolved 2026-11-23 in `68a2c8d6`** ([follow-up](spec-five-deferred-pilot-admission-qualification.md)); the pilot maps the verified `owner_admission_deferred` 503 to the ordinary 403 tenant denial.
- [x] [Review][Patch] **High — constructing a typed error can itself fail.** Command metadata accepts nonblank correlation IDs. The error constructor rejects several ordinary substrings. A missing key with such a correlation reaches the error constructor and raises an exception instead of producing the expected 400. Resolve a safe correlation once and reuse it for errors. When the body cannot be decoded, preserve a safe supplied correlation header before falling back to the trace ID. [PilotCommands.cs:55–67](../../src/Hexalith.Conversations.Api/PilotCommands.cs#L55) · [ConversationError.cs:83](../../src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs#L83) **Resolved 2026-11-23 in `65993427`** ([follow-up](spec-five-deferred-pilot-error-outcomes.md)); `SafeErrorCorrelationId` applies the contract safety rules to metadata, a single header, then the trace.
- [x] [Review][Patch] **High — the new Platform pin blocks the repository CI build.** The previous Platform revision carried release tag `v2.3.0`. The story pins `a03bf4aa`, which has no release tag in the remote tag listing. CI checked out that exact revision and stopped during module preparation before compiling or running tests. Qualify a tagged Platform revision that contains the pilot composition and rerun the existing gate. [Story implementation](https://github.com/Hexalith/Hexalith.Conversations/commit/4d74b197d0e91043c84781cd6948a33b3019e7d0) · [failed CI job](https://github.com/Hexalith/Hexalith.Conversations/actions/runs/45035050389/job/137246824659) **Resolved 2026-11-24.** `references/Hexalith.Platform` is pinned to tag `v2.5.0` (`66b64388`, `954473a2`), which contains `a03bf4aa`. CI then passed on `f9f8074f` ([run 45443614200](https://github.com/Hexalith/Hexalith.Conversations/actions/runs/45443614200)).
- [x] [Review][Patch] **Medium — expired receipts direct clients to repeat a permanently expired key.** The API sets reconciliation text on the standard unknown-outcome error. Its serialized retry flag, client action and safe message still tell the client to retry. Make the fields agree on a terminal reconciliation outcome and assert them on the wire. [PilotCommands.cs:115](../../src/Hexalith.Conversations.Api/PilotCommands.cs#L115) · [error descriptor](../../src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCatalog.cs#L57) **Resolved 2026-11-23 in `65993427`** ([follow-up](spec-five-deferred-pilot-error-outcomes.md)).
- [x] [Review][Patch] **Medium — a completed replay can outlive its status record.** The mutation receipt lasts 24 hours. EventStore permits a shorter command-status TTL. In that configuration an accepted replay returns the original message ID, every status poll misses, and the API returns 503 after 45 seconds. Align the supported host’s retention contract or carry trustworthy terminal evidence on replay. [PilotCommands.cs:87](../../src/Hexalith.Conversations.Api/PilotCommands.cs#L87) · [mutation retention](https://github.com/Hexalith/Hexalith.EventStore/blob/275626569a47920c3fe4cc38e0cec33b555f5d7a/src/Hexalith.EventStore.Server/Actors/IdempotencyAdmissionActor.cs#L359) `68a2c8d6` set `EventStore__CommandStatus__TtlSeconds=90000` in the qualification AppHost. EventStore checks on startup that `EventStore:Idempotency:TerminalRetentionSeconds`, 86,400 by default, is at least the status TTL. With that setting alone the gateway would not start. **Resolved 2026-11-25 in the uncommitted working tree.** The qualification AppHost also sets `EventStore__Idempotency__TerminalRetentionSeconds=90000`. `run.py` requires a status TTL of at least 90000 and a terminal retention of at least that TTL. `module/platform-handoff.md` documents both settings and the startup check.
- [x] [Review][Patch] **Medium — an oversized command is reported as retryable uncertainty.** The gateway enforces a 1 MiB request limit before admission. Its explicit HTTP 413 falls through to 503 with the instruction to repeat the request. Preserve the size refusal as a nonretryable validation outcome. [PilotCommands.cs:109–118](../../src/Hexalith.Conversations.Api/PilotCommands.cs#L109) · [gateway request limit](https://github.com/Hexalith/Hexalith.EventStore/blob/275626569a47920c3fe4cc38e0cec33b555f5d7a/src/Hexalith.EventStore/Controllers/CommandsController.cs#L75) **Resolved 2026-11-23 in `65993427`** ([follow-up](spec-five-deferred-pilot-error-outcomes.md)).
- [x] [Review][Patch] **Medium — malformed downstream list data can become a successful empty list.** The argument-exception handler also covers gateway payload deserialization. A missing required page or a null row can throw there and be returned as a hidden HTTP 200. Limit the input-validation catch to parsing the caller’s filter and page, and return unavailable for a malformed owner response. [PilotQueries.cs:65–76](../../src/Hexalith.Conversations.Api/PilotQueries.cs#L65) · [list contract](../../src/Hexalith.Conversations.Contracts/Queries/ConversationListResult.cs) **Resolved 2026-11-23 in `65993427`** ([follow-up](spec-five-deferred-pilot-error-outcomes.md)).

#### Qualification issues

- [x] [Review][Patch] **High — the clean-launch command selects the pre-pilot Conversations manifest.** The pinned Platform checkout records Conversations `08953f5e`. Its manifest has only the domain server and subscribes to the older tenant topic. The AppHost template reads the path specified by the command. Point the source mapping and declaration at the reviewed Conversations and EventStore sources, then verify the resolved manifest includes the API before startup. [platform-handoff.md:627](../../module/platform-handoff.md#L627) · [Platform’s Conversations pin](https://github.com/Hexalith/Hexalith.Platform/tree/a03bf4aa563d1ee9863a699b1dc5aa2dec7b475e/references) **Resolved 2026-11-23 in `68a2c8d6`** ([follow-up](spec-five-deferred-pilot-admission-qualification.md)).
- [x] [Review][Patch] **Medium — the integration guide’s qualification command skips the outage check.** The default command omits the fault API and AppHost arguments. It can print its pass message without checking a lost confirmation or the committed effect before retry. Use the full invocation for AC3 and label a partial run explicitly. [integration-guide.md](../../docs/integration-guide.md#qualify-a-running-pilot-host) · [run.py:65](../../tests/packaged-client-journey/run.py#L65) **Resolved 2026-11-23 in `7c35922c`** ([follow-up](spec-five-deferred-packaged-client-qualification.md)).
- [x] [Review][Patch] **Medium — the pre-effect authority response is tested separately at each end.** The admission test catches the exception directly and the EventStore invoker test constructs its own marked HTTP response. Add a domain host test that observes the actual HTTP 503, empty body and exact pre-processing marker when access evaluation temporarily fails. This exercises the response on which same-key recovery depends. [domain middleware](../../src/Hexalith.Conversations.Server/Program.cs#L70) · [admission test](../../tests/Hexalith.Conversations.Server.Tests/Agents/ConversationPilotWriteTests.cs#L125) **Resolved 2026-11-23 in `68a2c8d6`** ([follow-up](spec-five-deferred-pilot-admission-qualification.md)); `ConversationPilotWriteHostTest` observes the domain `/process` response.
- [x] [Review][Patch] **Medium — the pilot has no regression case for a completed status belonging to another command.** Vary the receipt, tenant, domain and aggregate individually and assert 503 with an unknown outcome. The current successful fixture copies identifiers from the submitted request. [PilotCommands.cs:94](../../src/Hexalith.Conversations.Api/PilotCommands.cs#L94) · [PilotApiTests.cs:95](../../tests/Hexalith.Conversations.Server.Tests/Api/PilotApiTests.cs#L95) **Resolved 2026-11-23 in `7c35922c`** ([follow-up](spec-five-deferred-packaged-client-qualification.md)).
- [x] [Review][Patch] **Medium — the reader denial can pass without reaching the tenant-role check.** An absent or incorrect Party binding produces the same 403 before the role is evaluated. Verify the effective reader binding independently before submitting the denied create. [journey Program.cs:116](../../tests/packaged-client-journey/Program.cs#L116) · [binding check](../../src/Hexalith.Conversations.Server/Agents/ConversationPilotWriteAdmission.cs#L24) **Resolved 2026-11-23 in `68a2c8d6`** ([follow-up](spec-five-deferred-pilot-admission-qualification.md)).
- [x] [Review][Patch] **Medium — denied append cases reuse an already persisted message ID.** Duplicate handling could leave the count at one even if an invalid or denied request reached the write path. Give each case a fresh message ID and verify its absence in the appropriate durable state. [journey Program.cs:107](../../tests/packaged-client-journey/Program.cs#L107) **Resolved 2026-11-23 in `7c35922c`** ([follow-up](spec-five-deferred-packaged-client-qualification.md)).
- [x] [Review][Patch] **Medium — successful mutations need exact public response assertions.** The package journey accepts any non-null value and checks tenant and conversation. It could pass with HTTP 200 on both endpoints or with the wrong operation or key. Assert 201 for create, 202 for append, and the original key and operation on first submission and replay. [journey Program.cs:36](../../tests/packaged-client-journey/Program.cs#L36) **Resolved 2026-11-23 in `7c35922c`** ([follow-up](spec-five-deferred-packaged-client-qualification.md)).
- [x] [Review][Patch] **Medium — the outage fixture accepts an unrelated status lookup.** It marks any GET under the status prefix after a successful POST as the interrupted confirmation. Bind the observed GET to the message ID from the real submission receipt. [status_outage.py:85](../../tests/packaged-client-journey/status_outage.py#L85) **Resolved 2026-11-23 in `7c35922c`** ([follow-up](spec-five-deferred-packaged-client-qualification.md)).
- [x] [Review][Defer] **High — the installed SDK drops typed mutation errors.** In a separate consumer check, a wire-level HTTP 400 `idempotency_key_missing` became `command_validation_failed`, and an expired-receipt HTTP 409 `idempotency_outcome_unknown` became `idempotency_conflict`. The latter advises a new key. The journey verifies the captured body while accepting a generic failure from the client. Story 9.4 owns the existing response-decoding defect. Add adopter-facing assertions there and retain the wire assertions here. [ConversationClient.cs:239–256](../../src/Hexalith.Conversations.Client/ConversationClient.cs#L239) · [journey error checks](../../tests/packaged-client-journey/Program.cs#L107)

#### Rejected

- **Readiness always reports healthy — false as stated.** In the supported Platform source composition, the auxiliary-client hosting startup installs a route-catalog check tagged `ready`. The standalone API’s default self-check does not describe the composed host.
- **Prevalidating the second provisioning input — low.** An invalid second tenant ID can leave the first fixture tenant present. This operator-only lane explicitly requires a fresh disposable deployment, and the resulting input error stops the run before qualification. No product action item.

#### November 25, 2026 review of `7b832c67`

The review covered `d6d35c42..7b832c67`. Changes requested.

- [x] [Review][Patch] **Medium — late reconciliation can leave a valid receipt without command status.** If EventStore reconciles an accepted mutation two hours after writing status, the 24-hour receipt runs until hour 26 and the configured status expires at hour 25. The pilot can then return HTTP 503 after polling the original message ID. The chosen EventStore patch renews a matching `Completed` status before finalizing a recovered completion that returns only a tracking receipt. A missing or mismatched status or a failed write keeps the same-key recovery retryable. The EventStore Debug build and 102 focused tests passed, including replay past the original status expiry. The patch is applied locally in the EventStore checkout. Host qualification still requires a pinned EventStore revision containing it. [Gateway reconciliation](../../references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Pipeline/SubmitCommandHandler.cs#L318) · [Expiry regression](../../references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Pipeline/SubmitCommandHandlerIdempotencyAdmissionTests.cs#L513)
- [x] [Review][Patch] **High — accepted pilot timestamps can prevent Current reads within the deadline (AC1).** The patch admits API times up to 60 seconds ahead and persists them as event times. The named projection rejects a generation before its replay anchor with `projection_handler_failure`. A focused probe observed Completed at zero skew and Failed at +60 seconds. Reconciliation completed after the clock caught up. Its retry backoff can put Current reads beyond the 60-second deadline. A sufficiently old stamp can also leave a newly accepted reordered append Stale. A future-stamped pilot event can reject a later Agent or governance operation whose timestamp precedes it. Use the domain clock when assigning pilot event times and verify both mutations through the persisted projection at the skew boundaries. [Admission](../../src/Hexalith.Conversations.Server/Agents/ConversationAgentAdmissionStage.cs#L109) · [Append occurrence](../../src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs#L126) · [Named projection](../../src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs#L175) The EventStore dispatch handlers assign occurrences from the domain clock after admission and preserve stream order on append. All three cases at −600, 0 and +60 seconds reached Current detail and list on the first named projection attempt. The stored timeline had exactly one message with the original ID, author and text.
- [x] [Review][Patch] **Medium — qualification launch wiring can regress while its tests pass.** Removing terminal retention from a disposable template copy left all four runner tests green. Exercise the template's evaluated gateway configuration and check that EventStore accepts it. The recorded startup probe supplied both settings directly. [AppHost template](../../tests/packaged-client-journey/platform-apphost.cs.template#L79) The follow-up evaluated the template's exact gateway environment registration in an Aspire fixture. EventStore started with both evaluated values at 90,000 seconds and refused startup with terminal retention omitted.
- [x] [Review][Patch] **Medium — the full qualification command can lose its preflight without failing a test.** Bypassing the preflight in a disposable copy left all four runner tests green. Exercise the full entry point and verify that a short status TTL aborts before the outage fixture starts. [Full runner invocation](../../tests/packaged-client-journey/run.py#L131) The full-entry-point regression rejects a short status TTL before starting the outage fixture. Bypassing the preflight in a disposable copy made the regression fail.
- [x] [Review][Patch] **Low — the runner rejects a valid status TTL format that it previously accepted.** The old check and the .NET configuration binder accept `090000`. The new helper rejects it even though both retention bounds are met. Preserve accepted numeric formats for the AppHost preflight. [TTL parser](../../tests/packaged-client-journey/run.py#L25) The preflight accepts zero-padded positive decimal values within the 32-bit range. The runner suite exercises both retention settings.
- [x] [Review][Defer] **Medium — correcting the API clock leaves a terminal same-key rejection.** The semantic digest excludes the owner timestamp. EventStore replays the preceding validation failure even after the host clock is repaired. This behavior predates the enlarged admission window. [Gateway replay](../../references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Pipeline/SubmitCommandHandler.cs#L199)
- [x] [Review][Defer] **Low — the new TTL cannot extend statuses written before reconfiguration.** The store sets TTL on write. Deferred as an existing-deployment boundary outside the fresh qualification host. [Status store](../../references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Commands/DaprCommandStatusStore.cs#L33)
- [x] [Review][Defer] **Low — custom launch arguments can override the gateway environment seen by the runner.** The preflight has always checked reported environment values. The documented template uses those values directly. [Runner](../../tests/packaged-client-journey/run.py#L34) · [Gateway configuration](../../references/Hexalith.EventStore/src/Hexalith.EventStore/Program.cs#L14)
- [x] [Review][Defer] **Medium — caller-stamped commands can set an occurrence ahead of the domain clock.** This pre-existing behavior and its effect on later human appends are already in the [deferred work ledger](deferred-work.md#deferred-from-story-93-remaining-patch-review-2026-11-25).
- [x] [Review][Defer] **Low — evidence entries and timeline messages use different equal-time ordering.** The existing tie-break behavior is already in the [deferred work ledger](deferred-work.md#deferred-from-story-93-remaining-patch-review-2026-11-25).
- [x] [Review][Defer] **Medium — the runner tests are absent from the maintained CI lane.** The gap predates this patch and is already in the [deferred work ledger](deferred-work.md#deferred-from-story-93-remaining-patch-review-2026-11-25).

The four runner tests and 99 focused server tests passed. PDB source checks matched the reviewed C# files. The live two-tenant journey remains unverified for this candidate. The recorded full Contracts run passed 647 of 648, so the [completion policy](../../docs/runbooks/current-change-validation.md#routine-path) keeps the story open.

##### Rejected

- **Medium — spec frontmatter says `done` while sprint tracking says `review`.** Confirmed. The workflow handles story status after review disposition.
- **Low — older review notes call the fixes uncommitted.** The notes describe the prior working tree. The commit range in this review identifies the committed candidate.
- **Low — a timestamp cutoff includes both appends tied at that instant.** This follows the inclusive timestamp contract. A position cutoff selects the intermediate prefix.

## Implementation Notes

The user approved the Conversations-owned API, Platform hosting and an operator-provisioned tenant/principal-to-Party binding. The initial build used the Platform producer's source mapping and Builds 6.6.0. The Conversations root's pinned CLI and the Platform catalog builder require different Builds versions. The qualification used the producer-mapped checkout for the solution, host and packages.

### Remaining review patches (2026-11-24)

- Concurrent appends: the aggregate is the serialized write boundary. It commits an ordinary append at the later of the boundary-assigned `PostedAt` and the conversation's `LastEventAt`, so a valid append that arrives after a later-stamped one commits after it. Keep the year floor on the assigned value. Do not change the shared `IsValidOccurrence` used by Agent commands.
- Independent clocks: `ConversationAgentAdmissionStage` accepts pilot create and append occurrences up to one minute ahead of the domain clock. That is the `ClockSkew` the platform configures for bearer validation in `JwtBearerAuthenticationContract`, not the library's five-minute default; name that source in the code comment. Larger skews still fail with `invalid_owner_allocation`. Read the clock through an optional injected `TimeProvider`, defaulting to `TimeProvider.System`.
- Tests: two valid appends handled in reverse stamp order both commit, and the second commits no earlier than the first. For both create and append, cover an API clock 600 seconds behind, 60 seconds ahead (accepted) and 61 seconds ahead (rejected), naming the theory for the forward bound. Use the existing `PumpClock` test clock in the same test namespace and add no package dependency.
- Clock documentation: in `module/platform-handoff.md`, state that the API and domain hosts must keep synchronized clocks. Admission tolerates one minute of forward skew. A human append can commit as far ahead of the domain clock as the actual skew. Caller-stamped Agent and governance commands earlier than the latest event are rejected as invalid occurrences, as they already are under ordinary concurrency.
- Status retention: `tests/packaged-client-journey/platform-apphost.cs.template` also sets `EventStore__Idempotency__TerminalRetentionSeconds` to `90000`. `run.py` requires the terminal retention to be at least the status TTL, and the status TTL to be at least `90000`. Cover both in `test_run.py`, including a retention value longer than ten digits. Document both settings and the EventStore startup check in `module/platform-handoff.md`.

## Spec Change Log

- **2026-11-24, review loop 1.** Trigger: Blind 1, 3 and 8, Edge 1, and the Verification Gap reviewer's other finding. Amended the independent-clocks, tests and clock-documentation notes under "Remaining review patches". Known-bad state avoided: a five-minute window, justified by the library default, was five times the platform's configured one-minute bearer skew. It let a future-stamped human append reject correctly stamped Agent and governance commands for up to five minutes, and the clock requirement was undocumented. KEEP: the aggregate commit at the later of `PostedAt` and `LastEventAt`, with the year floor and the reordered-append test. Also keep the optional `TimeProvider` in admission, the `invalid_owner_allocation` reason, the terminal-retention template setting, the `run.py` checks and `test_run.py` cases, and the retention wording in `module/platform-handoff.md` and `docs/integration-guide.md`.

## Review Triage Log

The three review layers returned the following dispositions. Each entry refers to the working source candidate.

| Review finding | Verdict and disposition |
| --- | --- |
| Blind 1, bounded detail options | High, patch. The public adapter discarded the two read-purpose flags. Preserve them through the owner and check the response purpose. |
| Blind 2, caller event time | High, patch. A caller could advance LastEventAt. Use an owner timestamp for the committed event. |
| Blind 3, retryable authority | High, patch. Ordinary domain rejection permanently reserved the key on temporary failure. Recover only from a pre-effect authority failure. |
| Blind 4, gateway conflict mapping | Medium, patch. Concurrency and idempotency errors have different recovery semantics. Classify the signed gateway outcome. |
| Blind 5, legacy create descriptor | High, patch. Changing the existing descriptor changes historical digests. Isolate the ordinary human create from the previous route. |
| Blind 6, downstream read 401 | Medium, patch. Preserve the authentication status so the caller can renew the bearer. |
| Blind 7, dropped read correlation | Medium, patch. Carry the validated request correlation to the owner. |
| Blind 8, status polling load | Low, patch. Apply bounded backoff within the existing deadline. |
| Blind 9, same-tenant denied mutation | Medium, patch. A second bearer with gateway submission permission now makes a correctly scoped fresh create as a tenant reader and must receive a wire denial. |
| Blind 10, changed create replay | Medium, patch. The consumer now alters the label on an existing key and verifies the original persisted details. |
| Blind 11, uncommitted dependency revisions | Medium, patch. Supply export/apply instructions and a complete local source patch bundle for the participating owners. The user has not requested publication. |
| Blind 12, setup reproduction | Medium, patch. Include a local Platform entry point, tenant setup commands and an explicit checkpoint wait. Operator-provided credentials remain required. |
| Edge 1, bounded detail | High, patch. Same root cause as Blind 1. |
| Edge 2, lost retry after transient authority | High, patch. Same root cause as Blind 3. |
| Edge 3, status failure after receipt | High, patch. Once admitted, failure to confirm cannot establish nonexecution. Return an unknown outcome and check the committed effect independently. |
| Edge 4, future clock | High, patch. Same root cause as Blind 2. |
| Edge 5, expired idempotency key | High, patch. Expiry needs reconciliation and must not advise a new key for the same uncertain effect. |
| Edge 6, projection delivery between reads | Medium, patch. Wait for the target message in detail and list before comparing them. |
| Edge 7, successful payload with unsuccessful HTTP | Medium, patch. Require HTTP 200 as well as Current for both reads. |
| Edge 8, structured AppHost Party settings | Medium, patch. A .NET JSON array is a configuration section without a scalar value. Bind and serialize the section for the domain. |
| Edge 9, previous create receipts | High, patch. Same root cause as Blind 5. |
| Edge 10, dependency-loss coverage | Medium, patch. The fault fixture forwards a real command, interrupts only status, checks raw 503 and verifies an independent committed append before replay. |
| Edge 11, checkout reproducibility | Medium, patch. Same root cause as Blind 11. |
| Verification 1, integration invocation | Medium, patch. Provide an explicit, repeatable local lane with provisioning, completion and teardown commands. CI changes are outside the requested work. |
| Verification 2, synthetic client errors | Medium, patch. Inspect the raw status, error code and correlation on mutation failures before accepting the package result. |
| Verification 3, unknown status after commit | High, patch. Exercise the real API and gateway using an isolated status interruption. |
| Verification 4, participant roles | Medium, patch. Test matching non-creator author/actor pairs for Member, Observer and nonparticipant. |
| Verification 5, lost bounded-purpose flags | High, patch. Same root cause as Blind 1. |
| Verification 6, ordinary append clock | High, patch. Same root cause as Blind 2. |

### November 24, 2026 review of the remaining patches

The review covered the uncommitted changes for the two open patches.

| Review finding | Verdict and disposition |
| --- | --- |
| Blind 1, Edge 1, Verification other: a future-stamped human append blocks caller-stamped commands | Medium, bad_spec. Admission accepted pilot occurrences five minutes ahead. Agent `IsValidOccurrence` and the governance validations reject stamps before `LastEventAt`, so correctly clocked commands failed inside that window. The window now uses the platform skew and is documented. |
| Blind 3, skew constant cites the wrong default | Medium, bad_spec. `JwtBearerAuthenticationContract` sets `ClockSkew` to one minute and the API uses it. Same amendment. |
| Blind 8, clock behavior undocumented | Low, bad_spec. The handoff names no clock requirement. Same amendment. |
| Blind 2, Edge 3, Agent posts still reject reordering | Medium, defer. An Agent post stamped before a committed human append was rejected before this change. Agent occurrences bind the portable timestamp, so they cannot be moved forward. |
| Edge 2, unbounded Agent or governance occurrences | Medium, defer. Caller-stamped Agent and governance times have no forward bound, which predates this story. A far-future stamp used to make later human appends fail. They now commit at that time. |
| Blind 6, evidence and timeline order differ on equal times | Low, defer. Evidence entries tie on `EntryId` and the timeline on append position. The tie-break predates this story, but forward-moved appends make equal times more frequent. |
| Blind 4, skew rejection shares `invalid_owner_allocation` | Low, rejected. It requires skew beyond the tolerance and would add a branch and a new reason code. |
| Blind 5, no backward bound and misleading test name | Low, rejected. Admitting a slow API clock is intended. A lagging stamp moves forward to `LastEventAt`. The theory is renamed for its forward bound. |
| Blind 7, offset-dependent year floor | Low, rejected. The floor keeps the existing `IsValidOccurrence` semantics, and the pilot API stamps UTC. |
| Blind 9a, borrowed test clock | Low, patch. Reuse the `PumpClock` test clock in the same test namespace. The project’s `FakeTimeProvider` types are private test doubles, so no package is added. |
| Blind 9b, accepted rows do not assert the access check | False. Only `ConversationPilotWriteAdmission` can accept these commands, after an allowed access decision. |
| Blind 9c, default clock path untested | Low, rejected. The default is `TimeProvider.System` and adds no branch to test. |
| Blind 9d, no over-length retention case | Low, patch. Folded into the amended notes. |
| Blind 9e, duplicate fixture dictionary | Low, rejected. Cosmetic. |
| Verification 1, unrun qualification template | Medium, patch. Exercise the real EventStore startup check directly. The full live run needs operator credentials and stays an unverified deployment boundary. |

### November 25, 2026 review after loop 1

| Review finding | Verdict and disposition |
| --- | --- |
| Blind 1, the year-floor comment names the boundary-assigned time | Low, patch. The comment now names the API-assigned `PostedAt`. |
| Blind 2, `AppendMessage` documentation calls `PostedAt` the assigned instant | Low, patch. `PostedAt` is the API-assigned lower bound, and the event commits at the later of it and `LastEventAt`. |
| Blind 3, handoff clock paragraph omits creates | Low, patch. Creates use the same window and commit at their API time. |
| Edge 7, handoff overstates the bound on a human append's time | Low, patch. A human append also inherits a later time recorded by a caller-stamped command. |
| Blind 7, Edge 6, runner docstring claims EventStore binding parity | Low, patch. The helper accepts only a canonical positive 32-bit decimal value, a new row covers `090000`, and the handoff names the AppHost resource environment. |
| Blind 8, integration guide omits the clock prerequisite | Low, patch. One sentence links to the handoff requirement. A runner skew preflight was rejected as new machinery. |
| Blind 9, reordered-append test asserts only a lower bound | Low, patch. It asserts the exact committed time. |
| Verification 1, tied append times are not projected | Medium, patch. A materializer test projects tied times whose message IDs sort against commit order. The timeline keeps commit order. |
| Verification 2, the runner suite is not part of CI | Medium, defer. `test_run.py` has run only locally since `7c35922c`. Adding it to CI is a placement decision for the module's after-unit-tests action. |
| Verification other, Edge 2, evidence order on tied times | carried: Blind 6, low, defer. |
| Edge 1, unbounded Agent or governance times | carried: Edge 2, medium, defer. |
| Edge 4, no backward bound | carried: Blind 5, low, rejected. |
| Blind 6, no recovery guidance for rejected caller-stamped commands | Medium, defer with Blind 2 and Edge 3. The Agent and governance ordering rule predates this story. |
| Blind 4, the skew constant is not tied to the bearer contract by a test | Low, rejected. Drift would affect only the stated rationale, and the check would need a new cross-repository test. |
| Blind 5, no log or metric for skew rejections or forward-moved appends | Low, rejected. It needs skew, and the fix would add logging to the admission stage. |
| Blind 10, the template repeats the retention literal | Low, rejected. The runner rejects a retention below the TTL before qualification. |
| Edge 3, forward skew beyond the window is not retryable | Low, rejected. A retry is stamped by the same skewed clock, so a retryable response would only repeat. The strict check was also terminal. |
| Edge 5, runner ignores case or colon variants of gateway keys | Low, rejected. The runner reads the keys this repository's template sets. |

## Verification

Use producer-mapped builds and focused aggregate, admission, host and Platform tests. The isolated consumer must resolve the candidate client from a local NuGet feed. Observe the genuine command and projection paths and read persisted state independently. Report environment or package qualification failures separately from assertions that executed.


### Initial source and package verification

- The updated `Hexalith.Conversations.slnx` built in the producer-mapped Release checkout. All 1,683 server tests and 39 client tests passed. The 155 focused server cases and 104 focused aggregate cases passed. Eleven HTTP checks against the actual Kestrel process covered the pilot route boundary, failed credentials, both hidden shapes, invalid mutations and dependency failures.
- The isolated consumer restored `Hexalith.Conversations.Client` and `Hexalith.Conversations.Contracts` version `2.1.1-story93.1` from the candidate feed and built without warnings or errors. The runner starts from an empty package cache and verifies both package identities.
- The Platform source AppHost ran the API, domain, EventStore and Tenants to Healthy. Each receiver committed the generated routing catalog. The API answered 200 on /ready and 401 for an anonymous list. The dedicated Platform ACL regression passed. The owned manifests grant the API human command submission, human status lookup and query submission.
- The full contracts suite passed 637 of 638 tests. The signed SM2 record expects 1,810 bytes for the minimal fixture's `Directory.Build.props`. The pulled baseline contains 1,427 bytes (`git cat-file -s HEAD:tests/fixtures/Hexalith.Conversations.MinimalModule/Directory.Build.props`). The fixture and its release record are outside this story.

- The first real tenant provisioning run committed both tenants and their members. The producer emitted five events on `tenants.events`. The previous Conversations subscription consumed zero from `system.tenants.events`. The corrected owner declaration and local profile consume `tenants.events`. The next run observed both durable memberships and initialized conversation indexes before the installed client ran.
- A subsequent create committed and produced a durable conversation, but Dapr denied the API's status lookup on its overlapping command paths. The gateway now exposes dedicated human submit paths on the existing actions, the technical catalog recognizes those paths under the same human policies, and Platform grants the API only those POSTs and the human status GET. The original gateway paths remain available to existing callers.
- November 22 local adopter qualification passed. The installed Client SHA-256 was `5945b7bc5b16fd3864ca11ca167896c4fec43b80406228d0c7cfc36ed3a1d4fc` and Contracts was `f2cbd56dbbc6082cf9ca1c79c4e3f349b63d13dd4ef327d6b376823ec0a8d67d`. Both reads used resource-bound query assertions. The consumer observed the original conversation on create replay, exactly one message after append replay and a conflict for changed intent. Current detail and filtered list agreed. It verified the exact hidden bodies for conflicting credentials and the second tenant, an empty Current list under that tenant, anonymous 401, missing key 400 and cross-tenant write 403. An independent strong read of the domain's Dapr state confirmed the message count, ID, text and author.
- The run used Platform source composition, an operator-managed local issuer and disposable Redis. Tenants created the two tenants and committed their roles through the gateway. The package consumer used external HTTP for every business operation. The published image and production identity rollout remain unqualified.

### Final reviewed candidate

- On November 22, 2026 the isolated consumer restored version `2.1.1-story93.2` from a local feed into an empty package cache and compiled against those packages. The Client SHA-256 was `0ac3df1847c45a7788dc71fd5aacedc40df644186c8baa6294005a0032335661` and the Contracts SHA-256 was `10f546ccb22c7dbf03b44e328ea5a38b6f564703211f73b5e399ae83bec4435d`.
- A fresh Platform source AppHost started the API, domain, EventStore and Tenants. All seven catalog receivers committed their routing configuration. The operator provisioned two distinct tenants through EventStore, and the domain observed the contributor and reader roles in durable state. The host bound the two Party assignments from structured configuration.
- The installed client passed all three acceptance criteria. Identical create and append retries preserved the original identity and a single message. Changed create and append each returned a conflict. Authorized detail and list reached Current within 60 seconds. Bounded read purposes withheld the timeline, foreign detail and list returned their exact hidden bodies, and a correctly scoped reader with permission to submit commands received a write denial. The consumer checked the raw mutation error codes and correlations. The stored message had the original ID, author and text and an owner-assigned timestamp.
- The consumer submitted an append through a second instance of the real API. After the real gateway accepted it, the test interrupted status confirmation. The caller received HTTP 503 with `idempotency_outcome_unknown`. A strong read of the domain store found the committed message before retry. Retrying the same request at the regular host left one message.
- The reviewed Release solution built with zero warnings and errors. All 1,706 server, 189 domain and 39 client tests passed, as did 15 Platform enrollment tests, the dedicated Dapr ACL regression and 16 focused EventStore recovery tests. The contracts suite passed 637 of 638. Its sole failure compared the baseline minimal module fixture with the historical signed size recorded above.
- All 29 review findings were addressed. This run qualified the local source deployment. Published images and production rollout require separate verification.

### Remaining review patches (2026-11-25)

- The solution built with zero warnings and errors in a source-mapped Debug workspace. Builds CLI 6.8.1, built from the pinned Builds gitlink `bf8a28b4`, prepared the workspace. The root tool manifest pins CLI 6.6.2, which refuses preparation with HXW009 because it differs from the owned producer `v6.8.1`.
- Each maintained test project ran from its built assembly with the producer environment. Domain 189, client 76, server 1,800, Portable 308, Conformance 91, DeletionConformance 35, TestSupport 44, Admin.Web 14 and integration 54 tests passed. AppHost passed 20 and skipped its opt-in boundary lane. Contracts passed 647 of 648. The failing source-mode `PlatformEnrollmentMappingTest` case reports HXW006 for the unmapped `Hexalith.Builds.Tooling` reference of the unchanged Contracts test project. It failed the same way in the previous review-loop run.
- The server count includes the reordered-append test and the six forward-bound admission cases. `python3 -m unittest discover -s tests/packaged-client-journey -v` passed four tests, including 19 gateway and host settings cases.
- The real EventStore gateway at `a7b325d7` ran in Development with a disposable symmetric bearer key. With only `EventStore__CommandStatus__TtlSeconds=90000` it refused to start: `Idempotency terminal retention must be at least the status/archive TTL (90000 seconds).` With `EventStore__Idempotency__TerminalRetentionSeconds=90000` added, it reported that the application started and ran until the 90-second probe timeout. A retention of 89999 failed the same check.
- The live two-tenant journey needs operator credentials and was not rerun. It remains an unverified deployment boundary, as do published images and production rollout.
- After the re-derivation, the admission clock test reuses `PumpClock`, and the Server test project gained no package reference. The source-mapped solution rebuilt with zero warnings and errors. Server passed 1,800 tests and domain 189, with no failures or skips.
- Final gate after the loop-1 review patches, November 25, 2026: the source-mapped solution built with zero warnings and errors. Each maintained test project ran on its own. Domain passed 189, client 76, server 1,801, Portable 308, Conformance 91, DeletionConformance 35, TestSupport 44, Admin.Web 14 and integration 54. AppHost passed 20 and skipped its opt-in boundary lane. Contracts passed 647 of 648; the HXW006 case is recorded in the deferred ledger. The packaged-client runner suite passed four tests.

### Follow-up after review of `7b832c670a1ff8d52ca3fa0258360400618f82df`

- The source-mapped Debug solution built with zero warnings and errors. The focused pilot test dispatched create and append through the EventStore SDK and exercised the named projection and both query paths at −600, 0 and +60 seconds. All three cases reached Current with the expected stored timeline.
- The corrected fixture maps Builds.Tooling at the direct catalog root selected by the producer. The starting revision reproduced HXW006. All 10 focused enrollment tests and all 648 Contracts tests passed after the correction.
- All five packaged-client runner tests passed. In a disposable copy, bypassing the host preflight failed the full-entry-point regression. The package-mapped Release Client and Contracts `2.1.1-story93.3` installed into an empty cache and compiled without warnings.
- The gateway startup probe consumed retention values produced by evaluating the exact template registration through Aspire resource callbacks. The probe reported application startup with both configured values. With terminal retention removed, the same EventStore host refused startup because the default 86,400 seconds was shorter than the 90,000-second status TTL.
- Aspire 17 discovered the named `apphost.cs` entry point in the disposable Platform checkout. The earlier custom filename was rejected during CLI discovery, and the handoff now uses the recognized entry point.
