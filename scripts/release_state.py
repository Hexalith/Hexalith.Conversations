#!/usr/bin/env python3
"""Verify NuGet and GitHub release destinations without mutating them."""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import os
import re
import sys
import time
import urllib.error
import urllib.request
import zipfile
from dataclasses import dataclass
from typing import Callable

from release_contract import REPOSITORY, expected_asset_names, load_manifest, validate_semver


@dataclass(frozen=True)
class HttpResponse:
    """Minimal HTTP response used by release-state verification and its tests."""

    status: int
    body: bytes = b""


@dataclass(frozen=True)
class ReleaseAsset:
    """Integrity metadata for one uploaded GitHub release asset."""

    sha256: str
    download_url: str


Request = Callable[[str, str], HttpResponse]
DIGEST_PATTERN = re.compile(r"^sha256:([0-9a-f]{64})$")


def default_request(method: str, url: str) -> HttpResponse:
    """Issue a bounded read-only request to an official endpoint."""
    headers = {"User-Agent": "Hexalith.Conversations-release-verifier"}
    token = os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")
    if url.startswith("https://api.github.com/") and token:
        headers["Authorization"] = f"Bearer {token}"
        headers["Accept"] = "application/vnd.github+json"
    request = urllib.request.Request(url, method=method, headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return HttpResponse(response.status, response.read() if method != "HEAD" else b"")
    except urllib.error.HTTPError as exc:
        return HttpResponse(exc.code, exc.read())
    except (urllib.error.URLError, TimeoutError):
        return HttpResponse(0)


def _github_url(repository: str, suffix: str) -> str:
    return f"https://api.github.com/repos/{repository}/{suffix}"


def _nuget_url(package_id: str, version: str) -> str:
    lower_id = package_id.lower()
    return (
        f"https://api.nuget.org/v3-flatcontainer/{lower_id}/{version}/"
        f"{lower_id}.{version}.nupkg"
    )


def _status(response: HttpResponse, description: str) -> bool:
    if response.status == 200:
        return True
    if response.status == 404:
        return False
    raise ValueError(f"{description} returned unexpected HTTP status {response.status}")


def verify_absent(version: str, repository: str = REPOSITORY, request: Request = default_request) -> None:
    """Require every immutable publication destination to be absent."""
    validate_semver(version)
    packages = load_manifest()
    destinations: dict[str, bool] = {}
    for package in packages:
        destinations[f"NuGet:{package.package_id}"] = _status(
            request("HEAD", _nuget_url(package.package_id, version)),
            f"NuGet package {package.package_id} {version}",
        )
    tag = f"v{version}"
    destinations[f"GitHub:tag:{tag}"] = _status(
        request("GET", _github_url(repository, f"git/ref/tags/{tag}")),
        f"GitHub tag {tag}",
    )
    destinations[f"GitHub:release:{tag}"] = _status(
        request("GET", _github_url(repository, f"releases/tags/{tag}")),
        f"GitHub release {tag}",
    )
    present = sorted(name for name, exists in destinations.items() if exists)
    if present and len(present) != len(destinations):
        raise ValueError(f"PARTIAL_PUBLICATION: immutable destinations are mixed: present={present}")
    if present:
        raise ValueError(f"DESTINATION_OCCUPIED: release {version} already occupies every destination")


def _tag_target_sha(tag: str, repository: str, request: Request) -> str:
    tag_document = _json(
        request("GET", _github_url(repository, f"git/ref/tags/{tag}")),
        f"GitHub tag {tag}",
    )
    object_value = tag_document.get("object")
    if not isinstance(object_value, dict):
        raise ValueError(f"GitHub tag {tag} has no object identity")
    object_type = object_value.get("type")
    object_sha = object_value.get("sha")
    if object_type == "tag" and isinstance(object_sha, str):
        annotated = _json(
            request("GET", _github_url(repository, f"git/tags/{object_sha}")),
            f"annotated GitHub tag {tag}",
        )
        annotated_object = annotated.get("object")
        if not isinstance(annotated_object, dict):
            raise ValueError(f"annotated GitHub tag {tag} has no target")
        object_type = annotated_object.get("type")
        object_sha = annotated_object.get("sha")
    if object_type != "commit" or not isinstance(object_sha, str):
        raise ValueError(f"GitHub tag {tag} does not resolve to a commit")
    return object_sha


def verify_publishable(
    version: str,
    source_sha: str,
    repository: str = REPOSITORY,
    request: Request = default_request,
) -> None:
    """At Semantic Release publish time, accept only its exact tag and absent write destinations."""
    validate_semver(version)
    if len(source_sha) != 40 or any(character not in "0123456789abcdef" for character in source_sha):
        raise ValueError("source SHA must be an exact lowercase 40-character commit")
    for package in load_manifest():
        if _status(
            request("HEAD", _nuget_url(package.package_id, version)),
            f"NuGet package {package.package_id} {version}",
        ):
            raise ValueError(
                f"DESTINATION_OCCUPIED: NuGet package {package.package_id} {version} already exists"
            )
    tag = f"v{version}"
    if _tag_target_sha(tag, repository, request) != source_sha:
        raise ValueError(f"GitHub tag {tag} does not resolve to source {source_sha}")
    if _status(
        request("GET", _github_url(repository, f"releases/tags/{tag}")),
        f"GitHub release {tag}",
    ):
        raise ValueError(f"DESTINATION_OCCUPIED: GitHub release {tag} already exists")


def _json(response: HttpResponse, description: str) -> dict[str, object]:
    if response.status != 200:
        raise ValueError(f"{description} returned HTTP status {response.status}")
    try:
        document = json.loads(response.body)
    except (UnicodeError, ValueError) as exc:
        raise ValueError(f"{description} returned invalid JSON") from exc
    if not isinstance(document, dict):
        raise ValueError(f"{description} did not return a JSON object")
    return document


def _poll_nuget_packages(
    version: str,
    request: Request,
    attempts: int,
    delay_seconds: float,
) -> dict[str, bytes]:
    """Download every package with bounded retry and return its bytes by package ID."""
    packages = load_manifest()
    payloads: dict[str, bytes] = {}
    last_observation: dict[str, str] = {}
    for attempt in range(attempts):
        for package in packages:
            if package.package_id in payloads:
                continue
            try:
                response = request("GET", _nuget_url(package.package_id, version))
            except (TimeoutError, OSError) as exc:
                last_observation[package.package_id] = f"transient exception {type(exc).__name__}"
                continue
            if response.status == 200 and response.body:
                payloads[package.package_id] = response.body
            elif response.status == 404:
                last_observation[package.package_id] = "HTTP 404"
            elif response.status in (0, 408, 425, 429) or 500 <= response.status <= 599:
                last_observation[package.package_id] = f"transient HTTP {response.status}"
            elif response.status == 200:
                last_observation[package.package_id] = "empty HTTP 200 response"
            else:
                raise ValueError(
                    f"NuGet package {package.package_id} {version} returned unexpected HTTP status "
                    f"{response.status}"
                )
        if len(payloads) == len(packages):
            return payloads
        if attempt + 1 < attempts:
            time.sleep(delay_seconds)
    missing = [package.package_id for package in packages if package.package_id not in payloads]
    raise ValueError(
        "PUBLICATION_INCOMPLETE: NuGet packages could not be downloaded within the bounded poll; "
        f"missing={missing}, last={last_observation}"
    )


def _release_asset_digests(release: dict[str, object], version: str) -> dict[str, ReleaseAsset]:
    """Require the exact uploaded release asset inventory and return its integrity metadata."""
    tag = f"v{version}"
    if (
        release.get("tag_name") != tag
        or release.get("draft") is not False
        or release.get("prerelease") is not False
    ):
        raise ValueError(f"GitHub release {tag} is missing, draft, prerelease, or bound to another tag")
    assets = release.get("assets")
    expected = expected_asset_names(version)
    if not isinstance(assets, list) or len(assets) != len(expected):
        raise ValueError(f"GitHub release {tag} must contain exactly eight assets")

    verified_assets: dict[str, ReleaseAsset] = {}
    for asset in assets:
        if not isinstance(asset, dict):
            raise ValueError(f"GitHub release {tag} has a malformed asset")
        name = asset.get("name")
        size = asset.get("size")
        digest = asset.get("digest")
        download_url = asset.get("browser_download_url")
        if not isinstance(name, str) or not name or name in verified_assets:
            raise ValueError(f"GitHub release {tag} has a missing or duplicate string asset name")
        if asset.get("state") != "uploaded":
            raise ValueError(f"GitHub release asset {name} is not uploaded")
        if not isinstance(size, int) or isinstance(size, bool) or size <= 0:
            raise ValueError(f"GitHub release asset {name} has no nonzero size")
        match = DIGEST_PATTERN.fullmatch(digest) if isinstance(digest, str) else None
        if match is None:
            raise ValueError(f"GitHub release asset {name} has no usable SHA-256 digest")
        expected_url_prefix = f"https://github.com/{REPOSITORY}/releases/download/{tag}/"
        if not isinstance(download_url, str) or download_url != f"{expected_url_prefix}{name}":
            raise ValueError(f"GitHub release asset {name} has an unexpected download URL")
        verified_assets[name] = ReleaseAsset(match.group(1), download_url)
    if set(verified_assets) != expected:
        raise ValueError(
            f"GitHub release assets differ from the four package/symbol pairs; "
            f"missing={sorted(expected - set(verified_assets))}, "
            f"unexpected={sorted(set(verified_assets) - expected)}"
        )
    return verified_assets


def _canonical_package_digest(payload: bytes, description: str) -> str:
    """Hash package entries while excluding NuGet.org's added repository signature."""
    try:
        with zipfile.ZipFile(io.BytesIO(payload)) as archive:
            names = archive.namelist()
            if len(names) != len(set(names)):
                raise ValueError(f"{description} contains duplicate archive entries")
            entries = sorted(name for name in names if name.casefold() != ".signature.p7s")
            if not entries:
                raise ValueError(f"{description} has no package payload")
            digest = hashlib.sha256()
            for name in entries:
                encoded_name = name.encode("utf-8")
                content = archive.read(name)
                digest.update(len(encoded_name).to_bytes(8, "big"))
                digest.update(encoded_name)
                digest.update(len(content).to_bytes(8, "big"))
                digest.update(content)
            return digest.hexdigest()
    except (UnicodeError, zipfile.BadZipFile) as exc:
        raise ValueError(f"{description} is not a valid NuGet archive") from exc


def verify_present(
    version: str,
    source_sha: str,
    repository: str = REPOSITORY,
    request: Request = default_request,
    attempts: int = 20,
    delay_seconds: float = 15,
) -> None:
    """Poll NuGet, then require an exact GitHub tag/release and eight assets."""
    validate_semver(version)
    if len(source_sha) != 40 or any(character not in "0123456789abcdef" for character in source_sha):
        raise ValueError("source SHA must be an exact lowercase 40-character commit")
    if attempts < 1 or delay_seconds < 0:
        raise ValueError("poll attempts must be positive and delay must be non-negative")

    nuget_payloads = _poll_nuget_packages(version, request, attempts, delay_seconds)

    tag = f"v{version}"
    if _tag_target_sha(tag, repository, request) != source_sha:
        raise ValueError(f"GitHub tag {tag} does not resolve to source {source_sha}")

    release = _json(
        request("GET", _github_url(repository, f"releases/tags/{tag}")),
        f"GitHub release {tag}",
    )
    release_assets = _release_asset_digests(release, version)
    for package_id, nuget_payload in nuget_payloads.items():
        asset_name = f"{package_id}.{version}.nupkg"
        asset = release_assets[asset_name]
        asset_response = request("GET", asset.download_url)
        if asset_response.status != 200 or not asset_response.body:
            raise ValueError(f"GitHub release asset {asset_name} could not be downloaded")
        if hashlib.sha256(asset_response.body).hexdigest() != asset.sha256:
            raise ValueError(f"GitHub release asset {asset_name} does not match its API digest")
        nuget_digest = _canonical_package_digest(nuget_payload, f"NuGet package {package_id} {version}")
        asset_digest = _canonical_package_digest(asset_response.body, f"GitHub release asset {asset_name}")
        if asset_digest != nuget_digest:
            raise ValueError(
                f"NuGet package {package_id} {version} payload does not match GitHub asset {asset_name}"
            )


def main() -> int:
    parser = argparse.ArgumentParser(description="Verify immutable Conversations release destinations.")
    parser.add_argument("state", choices=("absent", "publishable", "present"))
    parser.add_argument("version")
    parser.add_argument("--repository", default=REPOSITORY)
    parser.add_argument("--source-sha")
    parser.add_argument("--attempts", type=int, default=20)
    parser.add_argument("--delay-seconds", type=float, default=15)
    args = parser.parse_args()
    if args.state == "absent":
        verify_absent(args.version, args.repository)
    elif args.state == "publishable":
        if args.source_sha is None:
            raise ValueError("--source-sha is required for publishable verification")
        verify_publishable(args.version, args.source_sha, args.repository)
    else:
        if args.source_sha is None:
            raise ValueError("--source-sha is required for present verification")
        verify_present(
            args.version,
            args.source_sha,
            args.repository,
            attempts=args.attempts,
            delay_seconds=args.delay_seconds,
        )
    print(f"Verified release destinations are {args.state} for {args.version}.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:  # noqa: BLE001 - command-line verification reports one concise failure.
        print(f"Release-state verification failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
