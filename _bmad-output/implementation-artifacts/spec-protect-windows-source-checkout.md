---
title: Preserve Windows path protection during source checkout
status: in-progress
baseline_commit: 76a25d8610972f7f6a3b61bcba917fe83d580aff
---

The current Windows CI checkout disables NTFS protection globally to retain eighteen immutable archived NuGet cache entries whose names Windows cannot materialize. Preserve the archived Git objects and the full index while validating every current path with native protected Git first. Permit one command-scoped sparse materialization only after exact native index parity, Windows ordinal path uniqueness, and all eighteen original path/mode/blob/size tuples are verified. Afterwards require the complete candidate index, clean protected status and exactly the eighteen archive exclusions. Keep persistent NTFS protection enabled.

The bootstrap reads the guard from the exact GitHub candidate commit without first expanding its tree. No runtime, authorization, package or dependency selectors change. Root Builds/Platform initialization stays explicit and nonrecursive. Pure refusal controls run in the Linux repository job and the Windows job before compilation. Existing release-tooling checks follow the real guard and retain their full source-selection checks.

## File Scope

Allowed files for this story:

- `.github/workflows/ci.yml` - protected bootstrap and refusal controls
- `tools/checkout-windows.py` - native path validation and exact archive exclusions
- `tools/tests/test_checkout_windows.py` - refusal and parity controls
- `tests/tooling/test_release_tooling.py` - preserve the current workflow boundary assertions
- `docs/release-evidence/windows-path-checkout-2899.json` - retain the earlier native path-only measurement
- `_bmad-output/implementation-artifacts/spec-protect-windows-source-checkout.md` - record scope and actual validation

## Validation

The helper is byte-identical to the prepared guard previously executed with native Git for Windows against2899c7a: 50,996 complete entries, 50,978 protected entries, exactly18 immutable exclusions and clean protected status. The retained report explicitly claims path/index policy only and no source execution or candidate acceptance. It does not qualify this new candidate.

At current base76a25d8, all18 actual immutable archive tuples and sizes are verified from the real Git objects; the complete tree has50,999 entries. Four pure control methods pass, including90 missing/rename/blob/mode negative subcases and independent diagnostic, omission, alias and unmerged-index refusals. The current workflow contract check passes and actionlint accepts the workflow. The real current candidate's Windows checkout and TestSupport tests remain required in hosted CI before acceptance.

The existing Linux sparse-selection test and the workflow-boundary check both pass against the actual source tree (2 methods,8.196s). The first local sparse fixture lacked materialized partial-clone blobs; hydrating only this task-owned source checkout fixed the fixture environment without changing the guard or its assertions. Full CI and final review remain open. The user's instruction waives human validation only.

## File List

- `.github/workflows/ci.yml`
- `tools/checkout-windows.py`
- `tools/tests/test_checkout_windows.py`
- `tests/tooling/test_release_tooling.py`
- `docs/release-evidence/windows-path-checkout-2899.json`
- `_bmad-output/implementation-artifacts/spec-protect-windows-source-checkout.md`
