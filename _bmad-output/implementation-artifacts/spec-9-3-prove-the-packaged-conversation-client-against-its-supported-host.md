---
title: 'Prove the packaged Conversations client against its supported host'
type: 'feature'
created: '2026-11-22'
status: 'done'
route: 'dispatch'
baseline_commit: 'a252b7f6cc7f758a7d10bdc745caade0c77cabe1'
review_loop_iteration: 0
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

## Implementation Notes

The user approved the Conversations-owned API, Platform hosting and an operator-provisioned tenant/principal-to-Party binding. The initial build used the Platform producer's source mapping and Builds 6.6.0. The Conversations root's pinned CLI and the Platform catalog builder require different Builds versions. The qualification used the producer-mapped checkout for the solution, host and packages.

## Spec Change Log

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
