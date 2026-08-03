#!/usr/bin/env python3
"""Generate the deterministic non-amending Epic 6 v8 execution view."""

from __future__ import annotations

import argparse
import hashlib
import os
from pathlib import Path
import re
import tempfile


GENERATOR_VERSION = "1.0.0"
OVERLAY_VERSION = "epic-6-authority-2026-08-01-v8"
ARCHITECTURE_VERSION = "conversations-architecture-2026-08-01-v8"
BEGIN_MARKER = (
    "<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8:BEGIN "
    f"version={OVERLAY_VERSION} supersedes=epic-6-authority-2026-08-01-v7 -->"
)
END_MARKER = (
    "<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8:END "
    f"version={OVERLAY_VERSION} -->"
)
EXPECTED_STORIES = tuple(range(1, 13))
COMPLETED_STORY_RECORD = (
    "_bmad-output/implementation-artifacts/"
    "6-2-migrate-conversations-to-platform-owned-hosting.md"
)
COMPLETED_STORY_EVIDENCE = (
    "docs/release-evidence/consume-promote-keep-story-6-2-disposition-v1.json",
    "docs/release-evidence/projection-read-store-population-proof-v2.json",
    "docs/release-evidence/sm-c2-hot-path-baseline-v1.json",
    "docs/release-evidence/sm-c2-hot-path-post-v1.json",
)


def sha256(data: bytes) -> str:
    """Return canonical lowercase SHA-256."""

    return hashlib.sha256(data).hexdigest()


def extract_inclusive_block(text: str, start: str, end: str) -> str:
    """Extract one marker-delimited block, including both markers."""

    if text.count(start) != 1 or text.count(end) != 1:
        raise ValueError("v8 authority markers must each occur exactly once")
    start_index = text.index(start)
    end_index = text.index(end, start_index) + len(end)
    return text[start_index:end_index]


def extract_section(text: str, heading: str, next_heading: str) -> str:
    """Extract a heading-delimited section, including its start heading."""

    if text.count(heading) != 1 or text.count(next_heading) != 1:
        raise ValueError(f"expected one section boundary for {heading!r}")
    start_index = text.index(heading)
    end_index = text.index(next_heading, start_index)
    return text[start_index:end_index].rstrip()


def extract_story_sections(block: str) -> list[str]:
    """Extract the twelve complete effective story definitions in numeric order."""

    matches = list(re.finditer(r"^### Story 6\.(\d+):.*$", block, re.MULTILINE))
    story_ids = tuple(int(match.group(1)) for match in matches)
    if story_ids != EXPECTED_STORIES:
        raise ValueError(f"expected Story 6.1-6.12 exactly once, found {story_ids}")

    sections: list[str] = []
    for match in matches:
        next_heading = re.search(r"^### ", block[match.end() :], re.MULTILINE)
        end_index = match.end() + next_heading.start() if next_heading else len(block)
        sections.append(block[match.start() : end_index].rstrip())
    return sections


def render(root: Path) -> str:
    """Render the complete current view from authoritative repository sources."""

    epics_path = root / "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md"
    architecture_path = root / "_bmad-output/planning-artifacts/architecture.md"
    sprint_path = root / "_bmad-output/implementation-artifacts/sprint-status.yaml"
    completed_story_record_path = root / COMPLETED_STORY_RECORD

    epics_bytes = epics_path.read_bytes()
    architecture_bytes = architecture_path.read_bytes()
    sprint_bytes = sprint_path.read_bytes()
    completed_story_record_bytes = completed_story_record_path.read_bytes()
    completed_story_evidence_bytes = {
        path: (root / path).read_bytes() for path in COMPLETED_STORY_EVIDENCE
    }
    epics = epics_bytes.decode("utf-8")
    block = extract_inclusive_block(epics, BEGIN_MARKER, END_MARKER)

    stories = extract_story_sections(block)
    dispositions = extract_section(
        block,
        "### Current Story Dispositions",
        "### Topological Dependency Plan",
    )
    topology = extract_section(
        block,
        "### Topological Dependency Plan",
        "### High-Risk BDD Scenario Catalogue",
    )
    bdd = extract_section(
        block,
        "### High-Risk BDD Scenario Catalogue",
        "### UX Preservation Planning Contract",
    )

    block_bytes = block.encode("utf-8")
    story_text = "\n\n".join(stories)
    completed_story_evidence_frontmatter = "\n".join(
        f"  - path: '{path}'\n    sha256: '{sha256(content)}'"
        for path, content in completed_story_evidence_bytes.items()
    )
    completed_story_bindings = "\n".join(
        f"- **6.2-E{index}:** [`{path}`](../../{path}) — "
        f"`sha256:{sha256(content)}`"
        for index, (path, content) in enumerate(
            completed_story_evidence_bytes.items(),
            start=1,
        )
    )
    return f"""---
artifact: epic-6-current-execution-view-v1
generated: '2026-08-01'
generator_version: '{GENERATOR_VERSION}'
generation_command: 'python3 _bmad/scripts/generate_epic_6_current_execution_view.py'
source_epics: '_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md'
source_marker: 'EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8:BEGIN'
overlay_version: '{OVERLAY_VERSION}'
architecture_version: '{ARCHITECTURE_VERSION}'
status_source: '_bmad-output/implementation-artifacts/sprint-status.yaml'
source_epics_sha256: '{sha256(epics_bytes)}'
source_v8_block_sha256: '{sha256(block_bytes)}'
source_architecture_sha256: '{sha256(architecture_bytes)}'
source_sprint_status_sha256: '{sha256(sprint_bytes)}'
completed_story_6_2_record: '{COMPLETED_STORY_RECORD}'
completed_story_6_2_record_sha256: '{sha256(completed_story_record_bytes)}'
completed_story_6_2_evidence:
{completed_story_evidence_frontmatter}
status: 'authority-correction-only-not-ready'
---

# Epic 6 Current Execution View

> **AUTHORITY CORRECTION ONLY — NOT READY.** This file is a deterministic,
> non-amending projection of the active v8 block. It does not authorize any
> remaining Epic 6 implementation. Work may start or resume only after
> mechanical v8 validation passes and a separate independent implementation-
> readiness assessment returns `READY`.

The append-only v8 block is authority; this projection exists to give an
implementer one complete, topologically ordered view. The source marker,
versions, hashes, generator identity, and status source above are validated.
Hand editing or semantic drift is a conformance failure.

## Completed Story 6.2 Retrospective Checkpoints

| Checkpoint | Boundary | Historical result | Immutable bindings |
| --- | --- | --- | --- |
| 6.2-H1 Baseline and authority | Frozen inventory, benchmark, ownership, and promotion declarations | Preserved from the immutable completed record. | 6.2-R, 6.2-E1, 6.2-E3 |
| 6.2-H2 Runtime and projection migration | Test-only hosting, platform surfaces, population path, and correctness lanes | Preserved from the immutable completed record. | 6.2-R, 6.2-E2 |
| 6.2-H3 Candidate evidence and closure | Candidate binding, generated record, promotion gate, and historical SM-C2 disposition | Preserved from the immutable completed record. | 6.2-R, 6.2-E2, 6.2-E4 |

### Immutable completed-history bindings

- **6.2-R:** [`{COMPLETED_STORY_RECORD}`](../implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md) — `sha256:{sha256(completed_story_record_bytes)}`
{completed_story_bindings}

These checkpoints are navigation aids only. They are not new work items,
independent completion claims, or permission to rewrite/re-evaluate Story 6.2.

{dispositions}

## Complete Effective Story Definitions

{story_text}

{topology}

{bdd}

## Completion Gate

Authority validation proves only that the v8 planning set is complete,
append-only, internally consistent, acyclic, metric-consistent, UX-preservation
safe, and projection-equivalent. It does not implement any story and does not
run or predetermine the separate implementation-readiness assessment.
"""


def write_atomically(path: Path, content: str) -> None:
    """Replace one generated artifact without exposing a partial file."""

    path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(
        mode="w",
        encoding="utf-8",
        newline="\n",
        dir=path.parent,
        prefix=f".{path.name}.",
        delete=False,
    ) as temporary:
        temporary.write(content)
        temporary_path = Path(temporary.name)
    os.replace(temporary_path, path)


def main() -> int:
    """Generate or validate the checked-in execution view."""

    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()

    root = Path(__file__).resolve().parents[2]
    output_path = root / "_bmad-output/planning-artifacts/epic-6-current-execution-view-v1.md"
    expected = render(root)

    if args.check:
        if not output_path.exists() or output_path.read_text(encoding="utf-8") != expected:
            print("EPIC_6_CURRENT_VIEW_DRIFT")
            return 1
        print("EPIC_6_CURRENT_VIEW_OK")
        return 0

    write_atomically(output_path, expected)
    print(output_path.relative_to(root))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
