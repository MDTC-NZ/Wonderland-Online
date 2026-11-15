#!/usr/bin/env python3
"""Publish administrator-supplied client patch files to a server update directory."""

import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import tempfile

MAX_FILE_SIZE = 512 * 1024 * 1024


def sha256_file(path):
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def is_safe_relative_path(path):
    parts = path.split("/")
    if not path or len(path) > 240:
        return False
    for part in parts:
        if not part or part in (".", "..") or part.endswith((".", " ")):
            return False
        base_name = part.split(".", 1)[0].rstrip(" ").upper()
        if base_name in ("CON", "PRN", "AUX", "NUL") or (
            len(base_name) == 4 and base_name[:3] in ("COM", "LPT") and base_name[3] in "123456789"
        ):
            return False
        if any(not (character.isalnum() or character in " ._-") for character in part):
            return False
    return True


def publish(source_directory, output_directory, version):
    source = source_directory.resolve(strict=True)
    output = output_directory.resolve()
    files_directory = output / "files"

    if not source.is_dir():
        raise ValueError("Source must be a directory.")
    if source == output or source in output.parents:
        raise ValueError("Output must not be inside the source directory.")
    if not version or len(version) > 64 or any(ord(char) < 32 for char in version):
        raise ValueError("Version must be 1-64 printable characters.")

    output.mkdir(parents=True, exist_ok=True)
    files_directory.mkdir(parents=True, exist_ok=True)
    entries = []

    for directory, subdirectories, filenames in os.walk(str(source), followlinks=False):
        directory_path = Path(directory)
        subdirectories[:] = sorted(subdirectories)
        for name in list(subdirectories):
            path = directory_path / name
            if path.is_symlink():
                raise ValueError("Source contains a symbolic-link directory: {}".format(path))
        for name in sorted(filenames):
            source_file = directory_path / name
            if source_file.is_symlink() or not source_file.is_file():
                raise ValueError("Source contains a non-regular file: {}".format(source_file))
            relative = source_file.relative_to(source).as_posix()
            if not is_safe_relative_path(relative):
                raise ValueError("Unsupported update file path: {}".format(relative))
            size = source_file.stat().st_size
            if size > MAX_FILE_SIZE:
                raise ValueError("Update file exceeds 512 MiB: {}".format(relative))

            destination = files_directory.joinpath(*relative.split("/"))
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(str(source_file), str(destination))
            entries.append({
                "path": relative,
                "size": destination.stat().st_size,
                "sha256": sha256_file(destination),
            })

    manifest = {
        "version": version,
        "files": entries,
    }
    fd, temporary_name = tempfile.mkstemp(prefix=".manifest-", suffix=".tmp", dir=str(output))
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as destination:
            json.dump(manifest, destination, indent=2, sort_keys=True)
            destination.write("\n")
        os.replace(temporary_name, str(output / "manifest.json"))
    finally:
        if os.path.exists(temporary_name):
            os.remove(temporary_name)
    return len(entries)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", required=True, type=Path, help="Folder containing authorized patch files only")
    parser.add_argument("--output", required=True, type=Path, help="Server ClientUpdates directory")
    parser.add_argument("--version", required=True, help="Human-readable patch version")
    args = parser.parse_args()
    count = publish(args.source, args.output, args.version)
    print("Published version {} with {} file(s) to {}".format(args.version, count, args.output.resolve()))


if __name__ == "__main__":
    main()
