"""Story 16.1 removed the single-replica warning once its restart and two-replica proofs passed.

The Epic 16 removal gate (`epic16_tooling.verify_warning`) compares a baseline that still carries
`epic16_execution.WARNING_MARKER` at `WARNING_PATH` with a candidate that no longer does. These
current-tree checks keep the marker, and the two handoff statements that the warning remains, from
coming back now that the enrolled host persists the durable tenant projection.
"""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "_bmad/scripts"))

import epic16_execution as execution  # noqa: E402

HANDOFFS = ("module/README.md", "module/platform-handoff.md")
RETAINED_CLAIMS = (
    "SINGLE-REPLICA-ONLY warning remains",
    "single-replica/restart warning remains unchanged",
)


def test_warning_surface_no_longer_declares_single_replica_only() -> None:
    program = (ROOT / execution.WARNING_PATH).read_text(encoding="utf-8")

    assert execution.WARNING_MARKER not in program
    assert "builder.Services.AddConversationTenantAccess(builder.Configuration);" in program


def test_handoffs_no_longer_claim_the_warning_remains() -> None:
    for path in HANDOFFS:
        text = (ROOT / path).read_text(encoding="utf-8")
        assert execution.WARNING_MARKER not in text, path
        for claim in RETAINED_CLAIMS:
            assert claim not in " ".join(text.split()), (path, claim)
