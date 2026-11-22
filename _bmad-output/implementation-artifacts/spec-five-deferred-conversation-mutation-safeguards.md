---
title: 'Five deferred conversation mutation safeguards'
type: 'bugfix'
created: '2026-11-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Close BH-R2-04, BH-R2-05, BH-R2-08, BH-R2-09 and VG-R2-02 from the package-publication review. Bind a new Agent post's event identity to the admitted intent and check it against the complete replayed source. Attribute human edits through the authenticated edit operation. Cite edited message text at its edit position, validate deletion acknowledgement targets, and cover the registered participant POST and its route scope check.

</frozen-after-approval>

## Implementation Notes

- Entry revision is `77f78b1ae9d5ad4c774d769c40a7a8fde664f5d5`. The five selected findings protect the integrity of conversation mutations and their returned evidence. The implementation is contained in Conversations.
- Share the client's existing deterministic intent allocation with `ConversationAgentAdmissionStage`. Reuse the verifier's source event identity set. Preserve replay support and exact retries of historical posts.
- Update `ConversationProjectionMaterializer` to retain both the append sequence for timeline order and the edit sequence for current evidence. Keep message timestamps and identifiers stable.
- Use `ConversationAgentClientTests`, `ConversationAdmissionSafeguardTests`, `ConversationProjectionMaterializerTest`, and `ConversationCommandApiTest` for the targeted regressions. Use the routine current-change validation path.
- New posts use the same serialized intent identity that the client already sends. The admission checks follow the repeated authority decision and apply to writes that can append an event. An exact historical retry returns the existing no-op outcome after the aggregate compares the original intent.
- Visible edits update the evidence source sequence. The original append still orders the timeline and dates the message. A redacted edit preserves its existing citation. Previously materialized rows acquire the corrected position when the existing projection dispatch or rebuild rematerializes their source history.
- A confirmed acknowledgement must agree with the reported delivery target. A quarantined checkpoint can describe a later conflicting attempt while carrying the earlier confirmed receipt. Its poison code identifies that disposition.

## Review Triage Log

- Medium, patched. Retained appends preserve original provenance for retry comparison. Replay publishes human editor attribution only after an authenticated edit. A same-text edit can establish that attribution.
- Medium, patched. Quarantine can follow a confirmed receipt. The client permits a later conflicting attempt only with a quarantine reason and a delivery revision that can represent both transitions.
- Medium, patched. A withheld edit could advance a redacted message citation. The materializer now retains its previous citation and keeps the redaction event position on the redaction entry.
- Medium, deferred. Other restricted mutation types need their own review of source event identity ownership and retry rules.
- Low, rejected. The wrapper ID on a semantic retry does not contribute to the original intent or cause an event write. The original immutable fingerprint continues to decide exact retries.
- Low, documented. Existing detail rows receive the new citation during projection rematerialization. The projection test replays an edited stream, and the existing rebuild routes call the same materializer.
- Low, patched. The client test pins both command and posting event IDs to the original wire values.
- Low, rejected. Matching scope reaches the command handler in the grouped API test. The named authenticated host test verifies mismatched scope stops before authority and the matching request reaches its installed authority.

## Verification

- The Release assemblies reported 1,735 server, 62 client, and 189 domain tests passing. The local NuGet packages restored and compiled in a fresh adopter cache. The two Story 7.3 scenarios, root submodule check, and staged whitespace check passed.
- The Contracts suite reported 636 passes and two failures. The same client-source inventory and SM2 fixture-byte checks failed on the clean entry revision with the same values.
- The baseline Platform AppHost stopped at `HXW006` under the available dependency mapping. The shared pre-push checker stopped at the anonymous GitHub fetch of the accepted Hexalith.CiCd branch in this workspace.
