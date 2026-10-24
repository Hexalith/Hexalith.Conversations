---
title: 'Five deferred record evidence safeguards'
type: 'bugfix'
created: '2026-10-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete five deferred record-tooling scopes: bind generic build/restore metadata to solution global properties; validate retained restore assets and owning-project identity; distinguish declared promotion scope from measured range changes without inferring authorship; protect committed File List annotations from allowed output dirt with executable regression coverage; and exercise the generator's actual CLI option registrations rather than relying solely on source substrings. Verify, independently review, append precise ledger resolutions, commit and push main as requested.

</frozen-after-approval>

## Implementation Notes

- Entry is clean main at e06da861c314f54262baab75f802c1496b1ec400. This focused tooling batch has no product choices, migrations or dependency changes. The oneshot route follows the existing deferred-batch pattern under explicit implementation and push authorization. The anticipated footprint is the Python generator/shared evaluator, focused tests, runbook, spec and append-only ledger.
- Latest October 24 review R4/R6 own solution-context and restore-shape gaps. Reuse the bounded evaluator and successor output fixture. Supply solution global properties only for solution commands; preserve direct project behavior and unsupported solution-layout rejection. Require supported native assets versions, graph object shapes and matching project identity, permitting package-free graphs. Add native disposable SDK controls alongside hermetic faults. These checks do not assert complete NuGet dependency-graph validity or compiled binary provenance.
- July 28 promotion attribution: render caller-declared scope separately from derived changed-in-range state and explicitly state that authorship is not inferred. Keep historical five-column records readable without editing accepted bytes; any new recorded range state must agree with Git.
- July 28 File List annotation scope: current production derivation already preserves committed status with setdefault output accounting. Add direct regression coverage for added/modified/deleted source statuses despite allowed output edits, deletion and untracked output. Disclose this as verification of existing correct behavior rather than a new production fix.
- July 30 pass-7 CLI registration scope: add executable parser/help controls for every contracted flag with valid argparse forms; supplementary source-string checks remain historical. Negative mutations must retain source strings while removing option registrations and demonstrate that the controls reject the missing capability.
- Follow docs/runbooks/current-change-validation.md: focused suites, relevant operational workflow guards, whitespace, pinned commitlint, fetch/divergence inspection, push main and inspect CI. Evidence-boundary and governed story final-record publication are not-applicable to this freeform spec. No application code or AppHost wiring changes are planned; no application lifecycle action is required.

- Initial verification passed 141 output/reporting cases, 86 adjacent record cases, 43 existing successor cases, 45 result/retention cases and the integrated workflow guard (33/22 assertions). The native result is actual disposable SDK compilation and offline restore, rather than a synthetic stamp fixture. Native CLI help and ten isolated registration-removal mutants are included in the reporting suite.
- Review found a real argument regression for comma-containing checkout paths. Percent-escape MSBuild property values rather than relying on argv separation; persisted native controls cover commas, literal percent-hex text and semicolons. The official Microsoft MSBuild command-line and special-character references describe the property separator and percent-escape behavior.
- Review also reproduced implicit solution Platform=AnyCPU overriding a project default and lexical solution directory identity through a contained directory symlink. Both belong to the same solution-context scope and require native regressions. Physical containment remains required while lexical location supplies the solution globals.
- Native failed restore can write a structurally shaped assets graph containing Error diagnostics. Shape and ownership alone are insufficient for retained evidence: reject Error and malformed diagnostic arrays, while absent/empty/warning-only diagnostics remain supported. This strengthens the selected restore-evidence scope without claiming full NuGet graph validation.


## Review Triage Log

- R1 — medium, patch: literal comma in a solution path broke MSBuild property parsing. Escape literal property values once; native comma, percent-hex and semicolon controls pass.
- R2 — medium, patch: native plain solutions supply Platform=AnyCPU for both build and restore, overriding a project's x64 default. Bind that implicit context and retain a real compiled-output regression.
- R3 — medium, patch: native solution builds supply CurrentSolutionConfigurationContents. A source condition can otherwise bind an unrelated standalone output. Obtain the native configuration context through a bounded metadata-only solution target; forwarding approximate or fabricated XML would be insufficient. Final targeted review must verify this behavior before completion.
- R4 — medium, patch: physical path resolution changed lexical SolutionDir/SolutionPath through a contained directory alias. Preserve native lexical strings independently of physical containment validation; retain a native alias regression.
- R5 — medium, patch: native failed restore assets can carry Error diagnostics despite valid ownership and graph containers. Reject failing/malformed diagnostics; preserve absent/empty/warning-only collections and add a real offline failure control.
- R6 — medium, patch: a target's package entry could be a list or null. Validate immediate target-entry object shapes; malformed native-document variants now fail.
- R7 — medium, patch: contradictory target, dependency-group and project framework keys passed independently. Require basic framework correspondence while permitting runtime-specific target suffixes. Controls cover independent key mismatches and a runtime-specific positive case. This remains shape/coherence verification, not dependency solving.


## Final Verification

- Review recheck confirms the escaping, implicit platform, lexical aliases, restore-error diagnostics, nested target shapes and framework consistency fixes. Final review of exact configuration XML extraction remains required before status completion.
- The native solution configuration query uses an injected Message-only target in a securely allocated temporary directory, a unique base64 marker, strict base64/XML validation, a timeout and an accepted output-size limit. It invokes no project targets. Custom solution imports and import-path environment overrides fail closed. Native build context receives exact XML; restore leaves it unset. This is necessary because native MSBuild explicitly refuses solution-level getProperty/getTargetResult queries.
- npm tooling passes all 92 cases. The entry revision CI failure was independently confirmed from job 120765118996: NU1004 for frozen MinimalModule ServiceDefaults/Aspire lock graphs. Entry product, repository, conformance and AppHost jobs pass. This batch changes neither fixture locks nor platform pins and does not claim to repair that separately deferred failure.
- Pinned commitlint CLI 21.2.3 validates the exact candidate message with zero problems/warnings; /tmp/conversations-record-commitlint.log retains evidence. Message SHA-256: 6cb867694dd9ee45d570e6356d528ba7af7f66a63a0f3e0622236e65bfc896f0. Original deferred-ledger bytes remain a byte-identical prefix. The root submodule checker and whitespace checks pass.

- Final independent recheck resolves all seven findings, including the original configuration-XML reproduction. Native collision and six custom-import probes show no product build target runs and unsupported imports block. No additional findings remain deferred from this review.
- Final aggregate verification passes 354 cases with zero failures/errors/skips in /tmp/conversations-record-final-verification.xml. Six final native punctuation cases also pass after expanding them to both build and restore contexts (/tmp/conversations-record-final-punctuation.xml). npm passes 92 cases. The integrated operational guard verifies both current workflow scenarios (33 and 22 assertions), with no historical final-record publication.
- Native four-layout retained-reader controls snapshot the complete fixture file set, bytes and nanosecond modification times and verify no mutation. The query target name includes a securely generated nonce, preventing a solution project-name collision from triggering an unintended target. Source paths, tests and runbook are finalized; accepted historical bytes and root gitlinks remain unchanged.
