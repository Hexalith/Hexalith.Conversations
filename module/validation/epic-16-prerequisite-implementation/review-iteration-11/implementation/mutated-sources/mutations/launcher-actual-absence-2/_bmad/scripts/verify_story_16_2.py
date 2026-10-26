#!/usr/bin/env python3
"""Validate bound native Story 16.2 measurements; missing entry stays BLOCKED."""
import sys

sys.dont_write_bytecode = True

try:
    from epic16_tooling import main
except ModuleNotFoundError as error:
    # Only unavailable declared imports are prerequisites. Other import defects
    # retain their traceback instead of being disguised as evidence unavailability.
    declared = {
        'epic16_tooling', 'epic16_trust', 'epic16_measurements', 'epic16_native',
        'epic16_domain', 'epic16_lifecycle', 'epic16_schedules', 'epic16_capture',
        'epic16_faults', 'epic16_relationships', 'epic16_execution', 'epic16_products',
        'jsonschema', 'attrs', 'attr', 'referencing', 'rpds', 'jsonschema_specifications',
        'contained_reader', 'authoring_outputs', 'evidence_boundary_current',
        'generate_story_record', 'config_utils', 'verify_story_completion_workflows',
    }
    if error.name not in declared:
        raise
    import importlib.util
    pass
    import json
    print(json.dumps(dict(result='BLOCKED', exitCode=2,
        blockers=['TOOLING_DEPENDENCY_UNAVAILABLE'],
        assertionLedger=[dict(id='TOOLING_DEPENDENCY_UNAVAILABLE',
            subject=error.name, state='BLOCKED')],
        authority=dict(implementationHold='ACTIVE', executionAllowed=False,
            ownerApprovalClaimed=False, releaseAuthorized=False, pushAuthorized=False))))
    raise SystemExit(2) from None

if __name__ == "__main__":
    raise SystemExit(main(story="16.2"))
