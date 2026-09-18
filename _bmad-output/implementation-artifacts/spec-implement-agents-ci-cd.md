---
title: 'Verify and Harden Agents CI/CD'
type: 'bugfix'
created: '2026-09-18'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
context:
  - '/home/administrator/projects/hexalith/agents/references/Hexalith.Builds/.github/workflows/ci-cd-standards.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Agents CI/CD implementation in local commit `0c16d0f` must be checked against the established Tenants, EventStore, FrontComposer, and Hexalith.Builds release patterns, corrected wherever executable evidence exposes an error, and its six NuGet package publication states verified against the live registry.

**Approach:** Validate the committed workflow, semantic-release, package inventory, exact-source, and publication contracts with the repository's focused and Release/package-mode gates; apply only proven in-scope fixes in `/home/administrator/projects/hexalith/agents`; report the live NuGet result without pushing, publishing, dispatching a workflow, or changing GitHub settings or secrets.

</frozen-after-approval>

## Implementation Notes

- The target repository's pre-existing modification to `spec-5-1-adopt-hexalith-builds-as-the-sole-package-version-authority.md` is user-owned and must remain untouched.
- Tenants, EventStore, and FrontComposer converge on shared CI callers, exact-green-main admission, an immutable Builds release pin, protected manual release, explicit package manifest/count, package-mode Release builds, and post-publication verification.
- Agents pins `domain-release.yml` and `builds-execution-sha` to its current root-declared Builds commit `cb91511794c8898b738d85dc6c751f82b832cbc9`; the pinned workflow exposes every supplied input.
- Initial evidence: `actionlint`, 86 Python tooling contract tests, and npm signature/attestation audit pass. NuGet flat-container indices return exact HTTP 404 for all six Agents package IDs, so none is currently published.
- Locked JavaScript installation completed with zero vulnerabilities; all 504 registry signatures and 127 attestations verified.
- Shared consumer package authority and the EventStore `3.106.0 >= 3.105.0` floor gate pass.
- Release/package-reference restore and build pass with zero warnings and errors. The five test projects pass individually: 529 Contracts, 6 Client, 787 Domain, 546 Server, and 1,073 UI tests (2,941 total).
- Manifest-driven packing, exact six-package archive validation, and isolated package-consumer validation pass at `0.0.0-ci`; the repository verifier independently confirms all six test versions are absent from NuGet.
- No Agents source/configuration fix was required by executable validation. No workflow was dispatched and no package, tag, release, secret, variable, or environment was changed.
- Review exposed a release-plan race: tags could change during protected-environment approval, allowing the reusable workflow to calculate a version different from the reviewed plan. The caller now passes `reserved-version`, and Semantic Release fails before publication unless its version matches that reservation.
- Local policy/source-proof/planning jobs now have explicit 30/10/10-minute bounds; reusable CI/security/release jobs already own their own bounds.
- Post-patch `actionlint`, release JSON parsing, all 86 tooling tests, and all 546 Server tests pass. The first focused `dotnet test` attempt returned Microsoft Testing Platform exit code 5 after MSBuild serialization switches were forwarded; the prescribed fallback build plus direct xUnit v3 assembly invocation passed.
- Partial-publication reconciliation is real but is not a small safe patch: it requires separate authorization and byte/source identity rules. It was recorded in `deferred-work.md` rather than weakening collision failure or adding duplicate skipping.
