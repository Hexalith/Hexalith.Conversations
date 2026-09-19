#!/usr/bin/env python3
"""Command-line entry point for release-state verification."""

import sys

from release_state import main


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:  # noqa: BLE001 - command-line verification reports one concise failure.
        print(f"Release-state verification failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
