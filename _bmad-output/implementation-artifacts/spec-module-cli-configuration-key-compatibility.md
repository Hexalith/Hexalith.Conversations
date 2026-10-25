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
- Implemented embedded manifest/source parity and installed SDK positive plus
  eighteen per-field malformed-key controls in Builds'
  `Tools/test-g4-tool-package-contracts.ps1`; extended its existing synthetic gate.
  No schema or shared fixture rewrite was needed.
- Official local Release build/package and isolated install succeeded. Ordinary
  validation of the unchanged Conversations declaration passed in human/JSON.
  Both adoption modes still fail: seven passing rows and seventeen failing rows
  (fourteen selectors, three Dapr), exactly matching prior non-declaration rows.
- Focused Debug build succeeded with zero warnings/errors; direct native test
  assembly run passed 647 schema/CLI/adoption tests, with no failures or skips.
  Builds' additive `evidence/g4/module-configuration-keys-20261025/` retains
  native logs, stream hashes, package/DLL/schema identities, before inventory and
  preservation proof. Diagnostic package qualification is release-ineligible.
- Added Builds `docs/module-cli-configuration-key-compatibility.md` with
  diagnosis, reproduction and future release/Platform/consumer handoff. No
  staging, commit, push, publication, deployment, pin or gitlink update occurred.

## Review Triage Log

- Low, patched: removed the transient measurement harness from deliverable
  evidence; reused-ID overwrites are not offered as a supported replay tool.
  Command/exit/raw-stream records remain retained.
- Low, rejected: fixed paths in captured helper/configuration inputs describe
  the original observation. Parameterizing them would rewrite those inputs.
  The handoff explicitly uses fresh paths for replay and labels captured inputs.
- Low, patched: added the real installed-package qualification replay command,
  including its eighteen malformed-key checks and ineligibility limits.
- Medium, patched: the qualifier now logs each malformed key's field,
  JSON-escaped value and input hash, distinguishing same-filename invocations.
- Medium, patched: resource inspection now copies raw streams; fresh raw-byte
  inspections reconfirm both exact embedded-schema hashes without decoding.
- Low, rejected: an extra synthetic older-resource compiler harness would add
  scope. Existing source/embedded tests, missing-resource injection, exact parity
  guard and installed APP_API_TOKEN positive controls already reject the old
  schema; real source/loose/embedded bytes were compared independently.
- False: published NuSpec identity is retained in `results.json` under
  `packages[].nuspecRepository`, extracted directly from the original ZIP.
- Low, patched: retained the complete compressed 4,206-file before inventory
  and explicit recomputed preservation result for independent audit.
- Low, patched: added published 4.35.2 JSON validation, preserving both exact
  HXP002 fields alongside the candidate's machine-readable success.
- Low, patched: SDK fixture secret reference now derives from the selected
  secret name, avoiding an unrelated fixture-rename failure.
