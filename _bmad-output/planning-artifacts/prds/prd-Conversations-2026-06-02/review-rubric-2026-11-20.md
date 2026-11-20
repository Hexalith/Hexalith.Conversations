# PRD quality review — Conversations, November 20

## Overall verdict

The draft gives Release, API and projection owners testable work and identifies the evidence still needed for pilot acceptance. The initial review raised two medium findings. A second review verified both corrections. All seven dimensions now rate strong.

## Decision-readiness — strong after revision

Section 2 puts package qualification first and assigns the host and recovery investigations to owners who can supply the missing evidence. FR-20, SM-C2 and OQ-1 each have their own decision and evidence requirements.

### Findings

- **Medium** — Addendum B bundles the statuses into “until the exact successor receives independent authority.” A reader could interpret preservation-manifest approval as a route to clearing the failed performance gate or making repository grants effective. Limit the approval condition to the preservation manifest and point to the independent SM-C2 and OQ-1 requirements in §§2 and 12.

**Resolved on November 20.** Addendum B ties approval to the exact successor and its preservation evidence, requires all four benchmark paths to pass for SM-C2, and specifies current runtime evidence and effective grants for OQ-1. It records the historical hold separately.

## Substance over theater — strong

The authoring result in §7 gives measured files, an explicitly estimated comparison and the uncertainty in that comparison. The package mismatch and the restarted tenant returning an empty list give a maintainer concrete failures to investigate.

## Strategic coherence — strong

Sections 1, 5 and 7 connect the reduction in authoring work to contract preservation and the per-path latency budget. The sprint adds qualification for users of the existing product while retaining the pilot's scope and the other modules' ownership.

## Done-ness clarity — strong

Section 2.1 requires one stored message after retries, complete tenant history through a cutoff, and independent installs at compatibility boundaries. The linked sprint supplement specifies executable scenarios and a matched performance comparison. Unknown platform behavior, package ranges and release gates are evidence obligations with named owners.

## Scope honesty — strong after revision

Sections 5 and 14 preserve deferred capabilities and the historical product contract. The initial review found an inherited assumption about release status in the wording for removing superseded types.

### Findings

- **Medium** — Sections 9–10 call the product “greenfield/pre-release” and permit removing “any Conversations-local type” that is superseded. The availability of the 2.x packages is established, while external production use is still an assumption. Limit the removal latitude to private implementation types and qualifying plumbing-only tests. Refer package-visible changes to the release owner's compatibility review in §13.

**Resolved on November 20.** Section 9 limits removals to superseded private plumbing and its qualifying tests, with release-owner review and FR-20 justification. Sections 10 and 13 place changes to package-visible types under release-owner compatibility review.

## Downstream usability — strong

The nine affected stories are identifiable from §2.1, including the extra acceptance on 12.3 and 12.4. The PRD points to the sprint supplement for engineering scenarios and to bound evidence for the October authoring result.

## Shape fit — strong

The short current-readiness table works for a brownfield developer platform. The three named authoring journeys explain who benefits, while the separately preserved customer contract remains available to designers and conformance reviewers.

## Mechanical notes

The initiative headings run from FR-1 to FR-20, with FR-16 explicitly deferred. The update retains all three named initiative journeys and their IDs, and introduces no new assumption tag. The links to the sprint supplement, package guidance, October SM-2 decision and current-change policy resolve from the canonical PRD directory.
