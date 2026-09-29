# AD-4 preparation approval — 2026-09-27

The user replied **“I approve AD-4”** to the Story 7.1 terminal acceptance proposal in this conversation.

This records approval to prepare the additive acceptance contract and checker described in that proposal. It is sufficient authorization to continue that preparation without another proposal-approval request. It supplies no missing test result, integration identity, signed publication, or protected-host observation.

| Binding | Value |
| --- | --- |
| Approved proposal | [Story 7.1 terminal acceptance proposal](story-7-1-terminal-acceptance-proposal-2026-09-27.md) |
| Proposal exact-byte SHA-256 | `d05f8fb90fe3337f6d31b94f8268f966c1ebca1ea41ec9592b0f2a602612004d` |
| Reused investigation | [Story 7.2 completion-resume audit](story-7-2-completion-resume-2026-09-27.md) |
| Audit exact-byte SHA-256 | `3d578336bb2502e423e0baaf75c4a5ffcd61279702c8d5d7ccf08c09fd2659dc` |
| Observed repository tip | `aefe4003cc94f49021e942adb9cbb8ca9ccf86cd` on `main` |
| Current work | [AD-4 readiness contract and inspector](spec-ad4-acceptance-readiness.md) |

The first implementation is a read-only readiness inspector and staged contract. Its result distinguishes observed committed inputs from missing or unverified evidence and cannot authorize a transition, even when files contain self-declared approvals. This is preparation for the eventual protected acceptance checker, not that checker's adoption.

The actual integration revision, exact integration-path policy, authenticated entry/recovery decision, protected checker source and provenance route, current prerequisite evidence, post-integration proof, and final-record binding remain required before terminal publication. The proposed contract must make those requirements concrete without selecting an accepted-main commit from the observed tip.

This conversation receipt is not the V23 signed owner authority, an architecture successor, or an `ACCEPTED` publication. No cryptographic signer identity, repository-host approval identifier, or unavailable decision timestamp is asserted. The historic proposal remains unchanged as the exact document approved.

The implementation hold remains `ACTIVE`. Story 7.2 remains `in-progress`. Historical records, result artifacts, gitlinks, and authority pointers remain preserved. No push is authorized.

## Preparation evidence — 2026-09-27

The additive [readiness inspector](../../_bmad/scripts/inspect_story_7_1_acceptance.py),
[closed result schema](../../_bmad/schemas/story-7.1-ad4-readiness-result-v1.schema.json),
[focused tests](../../tests/tooling/test_ad4_acceptance_readiness.py), and
[staged contract](../../docs/runbooks/story-7.1-ad4-acceptance.md) now implement the
approved preparation. The spec retains exact verification commands and the review
log; no independent review approval or terminal-tooling adoption is implied by
this receipt.

After independent review and fixes, the focused suite passed 24 tests and 52 subtests. Inspection of the observed
`aefe4003cc94f49021e942adb9cbb8ca9ccf86cd` revision returned schema-valid
`BLOCKED / AD4_PREREQUISITE_ABSENT`, exit 2, all fifteen committed prerequisites
absent, and both preserved pairs structurally and digest verified. The result
retains ACTIVE, all four authority flags false, unsupported terminal verification
and publication, and a null accepted-main identity. Fake-complete approval inputs
also remain blocked in the tests.

The original 45-file preservation snapshot still matches every SHA-256 and
nanosecond modification time; the approved proposal's digest above is unchanged.
The root-submodule and whitespace checks passed. No prior tooling, authority
pointer, record, result artifact, gitlink, or sprint state was changed. The future
owner, integration, trust and final-binding evidence remains required exactly as
set out in the staged contract; this implementation does not request or supply
another generic approval.
