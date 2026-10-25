---
title: 'Qualify module CLI configuration-key compatibility'
type: 'bugfix'
created: '2026-10-25'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve the published Module CLI's APP_API_TOKEN schema incompatibility in
Hexalith.Builds. Compare accepted 4.35.2 package bytes, embedded schema, source
and packaging path. Preserve the existing source fix and malformed-key
rejections; close the smallest packaging qualification gap, test a fresh local
package against the unchanged Conversations manifest, and document release and
Platform acceptance separately. Preserve all existing Conversations evidence,
pending adoption failures and Story 16.1's runtime hold. Do not publish, deploy,
stage, commit, push, change accepted pins or update gitlinks.

</frozen-after-approval>

## Implementation Notes

- Investigation: owning Builds main is clean at
  `72e48d9544bacc218a5fa8290fc3f68ffdea8974`; Conversations' existing dirty
  preparation is explicitly preserved by the request. This is a freeform tool
  continuity fix, not Story 16.1 execution or an adoption-package successor.
- Source schema and strict regression tests already accept APP_API_TOKEN.
  Accepted CLI 4.35.2 identifies
  `4248b8c70cb35ba79fdd7e4dd63049d4c73211a0` and carries the older schema.
- Small, reversible scope: strengthen Builds' existing packaged-schema and
  installed-tool controls and their synthetic gate tests; add diagnosis,
  measurement evidence and release/acceptance handoff in Builds. No intent gap
  or irreversible action is required; use the oneshot route.
- Runtime publication evidence-boundary and story final-record gates are
  not-applicable to this routine Builds fix. Existing scoped adoption evidence
  will be checked for byte preservation, never regenerated.
