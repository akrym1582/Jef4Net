#!/usr/bin/env python3
"""Validate license metadata and referenced notices in a built main .nupkg."""
import argparse
import re
import sys
from pathlib import Path, PurePosixPath
from zipfile import ZipFile
from xml.etree import ElementTree as ET

parser = argparse.ArgumentParser()
parser.add_argument("package", nargs="+", type=Path)
args = parser.parse_args()
packages = [p for p in args.package if p.suffix == ".nupkg" and not p.name.endswith(".symbols.nupkg")]
if len(packages) != 1:
    sys.exit(f"Expected exactly one main .nupkg (symbols packages are ignored), found: {packages}")
with ZipFile(packages[0]) as archive:
    names = set(archive.namelist())
    roots = {PurePosixPath(n).name: n for n in names if len(PurePosixPath(n).parts) == 1}
    for required in ("LICENSE", "THIRD-PARTY-NOTICES.md"):
        if roots.get(required) != required:
            sys.exit(f"Missing exact package-root path: {required}")
    nuspecs = [n for n in names if PurePosixPath(n).suffix == ".nuspec"]
    if len(nuspecs) != 1:
        sys.exit(f"Expected one .nuspec, found {nuspecs}")
    root = ET.fromstring(archive.read(nuspecs[0]))
    license_nodes = [e for e in root.iter() if e.tag.rsplit('}', 1)[-1] == 'license']
    if len(license_nodes) != 1 or license_nodes[0].attrib.get("type") != "expression" or (license_nodes[0].text or "").strip() != "MIT":
        sys.exit("The .nuspec license must be the MIT expression")
    notice = archive.read("THIRD-PARTY-NOTICES.md").decode("utf-8")
    refs = sorted(set(re.findall(r"\((licenses/[^)]+)\)", notice)))
    if not refs:
        sys.exit("No license paths are referenced by THIRD-PARTY-NOTICES.md")
    missing = [ref for ref in refs if ref not in names]
    if missing:
        sys.exit(f"Missing referenced license paths (case-sensitive): {missing}")
print(f"Verified package license metadata and {len(refs)} referenced license file(s): {packages[0]}")
