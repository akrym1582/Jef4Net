#!/usr/bin/env python3
"""Verify pinned third-party source snapshots without network access."""
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def check(path, expected):
    actual = digest(path)
    if actual != expected:
        errors.append(f"{path.relative_to(ROOT)}: expected {expected}, got {actual}")

unisys = json.loads((ROOT / "data/unisys/provenance.json").read_text())
for item in unisys["mappings"]:
    check(ROOT / "data/unisys" / item["file"], item["normalized_sha256"])

jef = json.loads((ROOT / "data/jef4j/provenance.json").read_text())
for name, expected in jef["sha256"].items():
    check(ROOT / "data/jef4j" / name, expected)

icu = json.loads((ROOT / "data/icu/ibm/provenance.json").read_text())
for item in icu["files"]:
    check(ROOT / "data/icu/ibm" / Path(item["file"]).name, item["sha256"])

if errors:
    print("\n".join(errors), file=sys.stderr)
    sys.exit(1)
print(f"Verified {len(unisys['mappings']) + len(jef['sha256']) + len(icu['files'])} provenance hashes.")
