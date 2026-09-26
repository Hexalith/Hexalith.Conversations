# V29 C1 Protected-Branch Exception Attestation

- **Status:** Recorded direct human attestation
- **Recorded at:** 2026-09-26T22:23:06Z
- **Actor and attestor:** Jérôme Piquot
- **Exception effective time:** 2026-09-26T21:49:26Z, the observed C1 `push` run start
- **Exact C1 commit:** `6d2c73be7fcd690467cbed34eec500fdc6481c37`
- **Protected `main` immediately before that push:** `819e45b2f8a493baf1fc92b62e59bdb8dd8696d6`

## Human declaration

Jérôme Piquot is the actor of the one-time protected-branch exception that
landed the exact C1 commit above. The reason was to bootstrap V29 authority
for the six approved post-V28 root-gitlink commits: C1 could not authorize
itself under the prior route. The exception covered only the push that landed
C1 with the complete unpublished ancestry listed below; it is not reusable.

In the conversation that produced this record, the user identified the actor
as “Jérôme Piquot” and answered “Yes, record that declaration” to a question
that stated the actor, exact C1 commit, effective time, and reason above. This
document records a direct statement; it does not claim a cryptographic
signature or independent verification of the user's identity.

## Complete unpublished ancestry at C1 landing

The branch-filtered C1 `push` event supplied
`819e45b2f8a493baf1fc92b62e59bdb8dd8696d6` as its protected base.
`git rev-list --reverse --parents BASE..C1` and the GitHub comparison both
identify exactly these four single-parent commits, in order:

| Commit | Parent | Subject |
| --- | --- | --- |
| `6b6418054ec6ae8b62e4ee82248d1fc747a6baff` | `819e45b2f8a493baf1fc92b62e59bdb8dd8696d6` | `docs(evidence): retract superseded Story 7.2 record` |
| `c7eb9cdcd33be62f8dcb05948ff273f013596885` | `6b6418054ec6ae8b62e4ee82248d1fc747a6baff` | `docs(evidence): publish refreshed Story 7.2 final record` |
| `8c9b711e3990a6622c70a148b1b6fe151def2fc3` | `c7eb9cdcd33be62f8dcb05948ff273f013596885` | `docs(evidence): publish V29 root gitlink authority spec` |
| `6d2c73be7fcd690467cbed34eec500fdc6481c37` | `8c9b711e3990a6622c70a148b1b6fe151def2fc3` | `feat(evidence): bootstrap V29 root gitlink authority` |

The [C1 protected workflow run](https://github.com/Hexalith/Hexalith.Conversations/actions/runs/36274235024)
is a `push` on `main` with head C1, actor `jpiquot`, and start time
2026-09-26T21:49:26Z. Its protected-planning-authority job failed under the
pre-C1 event base, as expected for a bootstrap that cannot self-authorize.
The [GitHub comparison](https://github.com/Hexalith/Hexalith.Conversations/compare/819e45b2f8a493baf1fc92b62e59bdb8dd8696d6...6d2c73be7fcd690467cbed34eec500fdc6481c37)
reports `total_commits: 4` and the same ordered identities.

## Boundary

The later [C2 protected workflow run](https://github.com/Hexalith/Hexalith.Conversations/actions/runs/36274958259)
supplied C1 as its event base; this establishes the historical protected-base
handoff. This attestation does not grant execution, owner approval through the
V29 result, release, or push authority. The V29 implementation hold remains
`ACTIVE`; `executionAllowed`, `ownerApprovalClaimed`, `releaseAuthorized`, and
`pushAuthorized` remain false.
