"""Record the repository's unmodified current generator selection policy."""
from pathlib import Path
import hashlib
import json
import os
import sys
import pytest

ROOT = Path.cwd()
DESTINATION = Path(os.environ['EPIC16_GENERATOR_SELECTION'])


class SelectionInventory:
    def pytest_collection_finish(self, session):
        rows = [dict(nodeid=item.nodeid, file=str(item.path.relative_to(ROOT)),
                     fixtures=list(item.fixturenames)) for item in session.items]
        if not rows or {row['file'] for row in rows} != {'_bmad/scripts/tests/test_generate_story_record.py'}:
            raise pytest.UsageError('current generator selection is empty or includes an unrelated file')
        policy = ROOT / '_bmad/scripts/tests/conftest.py'
        DESTINATION.write_text(json.dumps(dict(arguments=list(session.config.args),
            keyword=session.config.option.keyword,
            policy=dict(path=str(policy.relative_to(ROOT)), sha256=hashlib.sha256(policy.read_bytes()).hexdigest()),
            selected=rows), indent=2) + '\n')


raise SystemExit(pytest.main(sys.argv[1:], plugins=[SelectionInventory()]))
