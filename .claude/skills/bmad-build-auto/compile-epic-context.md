# Compile Epic Context

**Task**
Given an epic number, the epics file, the planning artifacts directory, and a desired output path, compile a clean, focused, developer-ready context file (`epic-<N>-context.md`).

**Steps**

1. Read the epics file and extract the target epic's title, goal, and list of stories.
2. Scan the planning artifacts directory for the standard files (PRD, architecture, UX/design, product brief).
3. Pull only the information relevant to this epic.
4. Derive the exact overlay and architecture version identities that govern the target epic from the planning artifacts. Do not use current worktree defaults or infer missing identities.
5. Write the compiled context to the exact output path using the format below.

## Exact Output Format

Use these headings:

```markdown
---
overlay_version: '{exact target-epic overlay identity}'
architecture_version: '{exact target-epic architecture identity}'
---

# Epic {N} Context: {Epic Title}

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

{One clear paragraph: what this epic achieves and why it matters.}

## Stories

- Story X.Y: Brief title only
- ...

## Requirements & Constraints

{Relevant functional/non-functional requirements and success criteria for this epic (describe by purpose, not source).}

## Technical Decisions

{Key architecture decisions, constraints, patterns, data models, and conventions relevant to this epic.}

## UX & Interaction Patterns

{Relevant UX flows, interaction patterns, and design constraints (omit section entirely if nothing relevant).}

## Cross-Story Dependencies

{Dependencies between stories in this epic or with other epics/systems (omit if none).}
```

### Historical Epic 6 v8 exception

When the target is Epic 6 governed by `epic-6-authority-2026-08-01-v8` and `conversations-architecture-2026-08-01-v8`, the accepted detailed V8 context contract overrides the condensed Stories example above. Preserve or reconstruct that complete historical context from the exact V8 authority bytes; never replace it with a three-story retrospective summary or a newer V9-V12 identity. It must contain exactly one `### 6.1 ` through `### 6.12 ` heading, the complete FR-10 through FR-16 landing-zone table, the preservation denominators, the V8 dependency graph, projection-proof lifecycle, final-record, promotion, oracle-tier, SM-C2, AppHost, readiness-hold, and immutable-history semantics. If those exact governing bytes or any required semantic cannot be inspected, write nothing and report the context as blocked.

## Rules

- **Scope aggressively.** Include only what a developer working on any story in this epic actually needs. When in doubt, leave it out — the developer can always read the full planning doc.
- **Describe by purpose, not by source.** Write "API responses must include pagination metadata" not "Per PRD section 3.2.1, pagination is required." Planning doc internals will change; the constraint won't.
- **No full copies.** Never quote source documents, section numbers, or paste large blocks verbatim. Always distill.
- **No story-level details.** The story list is for orientation only. Individual story specs handle the details.
- **Nothing derivable from the codebase.** Don't document what a developer can learn by reading the code.
- **Be concise and actionable.** Target 800–1500 tokens total. This file loads into bmad-build-auto's context alongside other material.
- **Never hallucinate content.** If source material doesn't say something, don't invent it.
- **Frontmatter is evidence.** Emit both version scalars exactly once. Never omit, duplicate, or replace them with a newer unrelated authority identity.
- **Omit empty sections entirely**, except Goal and Stories, which are always required.

## Error handling

- **If the epics file is missing, the target epic is not found, or either governing version identity cannot be derived exactly:** write nothing and report the problem to the calling agent. Goal, Stories, and identity-bound frontmatter cannot be populated safely.
- **If planning artifacts are missing or empty:** still produce the file with Goal and Stories populated from the epics file. Under Requirements & Constraints, write: "Planning artifacts were unavailable; only epics-file context was used." Never hallucinate content to fill missing sections.
