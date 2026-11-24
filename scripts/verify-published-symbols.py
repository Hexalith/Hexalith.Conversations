#!/usr/bin/env python3
"""Check NuGet.org serves every portable PDB from the published release archives."""

from __future__ import annotations

import argparse
from hashlib import sha256
from http import HTTPStatus
from http.client import HTTPException
from pathlib import Path
import struct
import sys
from urllib.error import HTTPError
from urllib.parse import quote
from urllib.request import Request, urlopen
from uuid import UUID

from release_contract import load_manifest, read_portable_pdb, validate_semver


SYMBOL_SERVER = "https://symbols.nuget.org/download/symbols"
PDB_ID_SIZE = 20


def portable_pdb_id_offset(content: bytes) -> int:
    """Find the content ID at the beginning of the portable #Pdb metadata stream."""
    if not content.startswith(b"BSJB"):
        raise ValueError("expected portable PDB metadata")
    offset = None
    try:
        version_length, = struct.unpack_from("<I", content, 12)
        cursor = 16 + ((version_length + 3) & ~3)
        stream_count, = struct.unpack_from("<H", content, cursor + 2)
        cursor += 4
        for _ in range(stream_count):
            stream_offset, stream_size = struct.unpack_from("<II", content, cursor)
            name_start = cursor + 8
            name_end = content.index(b"\0", name_start, name_start + 32)
            if stream_offset + stream_size > len(content):
                raise ValueError("portable PDB stream extends beyond the file")
            if content[name_start:name_end] == b"#Pdb":
                if offset is not None or stream_size < 32:
                    raise ValueError("expected one complete #Pdb metadata stream")
                offset = stream_offset
            cursor = (name_end + 4) & ~3
    except (struct.error, ValueError) as error:
        raise ValueError(f"invalid portable PDB metadata: {error}") from error
    if offset is None or offset < cursor:
        raise ValueError("portable PDB lacks a contained content identifier")
    return offset


def verify_symbol(name: str, content: bytes) -> None:
    """Request the canonical symbol and check that the returned bytes match."""
    offset = portable_pdb_id_offset(content)
    guid = UUID(bytes_le=content[offset:offset + 16])
    key = f"{guid.hex}FFFFFFFF"
    encoded_name = quote(name.lower(), safe="")
    # Deterministic portable PDB checksums hash the image with its ID zeroed.
    checksum = sha256(content[:offset] + bytes(PDB_ID_SIZE) + content[offset + PDB_ID_SIZE:]).hexdigest()
    request = Request(
        f"{SYMBOL_SERVER}/{encoded_name}/{key}/{encoded_name}",
        headers={"SymbolChecksum": f"SHA256:{checksum}"},
    )
    try:
        with urlopen(request, timeout=30) as response:
            if response.status != HTTPStatus.OK:
                raise ValueError(f"NuGet symbol server returned HTTP {response.status} for {key}")
            length = response.headers.get("Content-Length")
            if length is not None and (not length.isdecimal() or int(length) != len(content)):
                raise ValueError(f"NuGet symbol server returned a different content length for {key}")
            downloaded = response.read(len(content) + 1)
    except HTTPError as error:
        raise ValueError(f"NuGet symbol server returned HTTP {error.code} for {key}") from error
    except (HTTPException, OSError) as error:
        raise ValueError(f"could not download symbol {key}: {error}") from error
    if downloaded != content:
        raise ValueError(f"NuGet symbol server returned different bytes for {key}")


def published_package_ids(version: str) -> tuple[str, ...]:
    """Return the complete symbol inventory for a Conversations release."""
    packages = tuple(package.package_id for package in load_manifest())
    # EventStoreExtensions joined the four original packages in v1.5.0.
    release = tuple(int(part) for part in version.split("-", 1)[0].split("."))
    if release < (1, 5, 0):
        return tuple(package for package in packages if package != "Hexalith.Conversations.EventStoreExtensions")
    return packages


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package_directory", type=Path, help="Directory containing the published .snupkg assets.")
    parser.add_argument("version", help="Exact published version with no leading v.")
    args = parser.parse_args()
    try:
        version = validate_semver(args.version)
        packages = published_package_ids(version)
    except ValueError as error:
        parser.error(str(error))

    failed = False
    for package_id in packages:
        archive = args.package_directory / f"{package_id}.{version}.snupkg"
        try:
            pdb = read_portable_pdb(archive, package_id, framework=None, version=version)
            verify_symbol(f"{package_id}.pdb", pdb)
        except (OSError, ValueError) as error:
            print(f"{archive.name}: {error}", file=sys.stderr)
            failed = True
        else:
            print(f"{archive.name}: NuGet.org serves the published portable PDB")
    return int(failed)


if __name__ == "__main__":
    raise SystemExit(main())
