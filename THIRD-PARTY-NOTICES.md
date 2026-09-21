# Third-party notices

Jef4Net's independently implemented .NET source code is distributed under the
[MIT License](LICENSE). This does not change the license terms of the
third-party data and documents identified below. In the source distribution and
NuGet package, the required license texts are available at the same relative
paths under `licenses/`.

## ICU IBM converter mapping data

- Upstream: https://github.com/unicode-org/icu-data
- Commit: `1c3d36e741bd648caaaba3a744267b23ca41bfc1`
- Retrieved: 2026-09-21 (UTC)
- Files: `data/icu/ibm/ibm-1390_P110-2003.ucm` and
  `data/icu/ibm/ibm-1399_P110-2003.ucm`
- License: Unicode License v3, including its additional third-party notices:
  [`licenses/Unicode-ICU.txt`](licenses/Unicode-ICU.txt)

The UCM snapshots are retained verbatim. CodeGen separates their SBCS 8482/5123
and common DBCS 16684 mappings into static tables. ICU is not a runtime
dependency. Precision indicators are preserved during parsing; normal tables
use round-trip mappings plus their explicitly directed decode-only or
encode-only mappings.

## jef4j mapping data and retained README

- Upstream: https://github.com/hidekatsu-izuno/jef4j
- Version in upstream pom.xml: 0.14.2
- Commit: `40d13b0d92963c9b36993d1d742cdfc10c25b0f6`
- Retrieved: 2026-09-20 (UTC)
- Mapping files: the Fujitsu, Hitachi, and NEC JSON files enumerated in
  `data/jef4j/provenance.json`
- Data license: CC0 / public domain dedication. The unmodified upstream README
  spells this “CC-O (Public Domain 相当)” and explicitly applies it to
  `src/test/resources/*.json`.
- Retained document: `data/jef4j/UPSTREAM-README.md`, covered by Apache-2.0;
  license text: [`licenses/Apache-2.0.txt`](licenses/Apache-2.0.txt)
- Upstream author: Hidekatsu Izuno and contributors.

The JSON files are preserved verbatim. Generated `FujitsuTables.g.cs`,
`HitachiTables.g.cs`, and `NecTables.g.cs` derive from them. No Java code or
serialized `.dat` assets are included. The independently implemented .NET
conversion code consulted upstream Apache-2.0 test expectations for
normal-profile selection and known conversion vectors; this does not make the
Jef4Net implementation Apache-2.0 licensed.

## Unicode JIS mapping data

- Files: `data/unisys/JIS0201.TXT`, `data/unisys/JIS0208.TXT`,
  `data/unisys/JIS0212.TXT`, and `data/unisys/US-ASCII-QUOTES.TXT`
- Distribution source: Unicode Character Database mapping data
- Retrieved: 2026-09-21 (UTC)
- License: Unicode License v3; license text and applicable notices:
  [`licenses/Unicode-ICU.txt`](licenses/Unicode-ICU.txt)

The normalized snapshots in `data/unisys` retain the two-column mappings used
by CodeGen. They are repository source data and are not packed in the NuGet
package. The generated runtime tables are packed and remain subject to the
applicable Unicode terms. JIS X 0201 contributes kana; JIS X 0208 and JIS X
0212 are transformed to LETS-J byte regions. JISASCII quotes and the canonical
`2020` space are explicit.

## Unisys JBIS specification reference

- Specification: Unisys *MultiLingual System (MLS) Administration, Operations,
  and Programming Guide*
- ClearPath MCP release: 7.0 (November 2001)
- Document: 8600 0288-305, sections 12 and 13
- Repository reference: `data/jbis/JBIS_Unisys_MLS_reference.md` and `.json`

The repository files are a structured implementation reference for the JBIS
byte layouts, shift controls, and Section 12 SBCS cells; they are not a copy of
the Unisys manual. JIS scalar mappings reuse the pinned Unicode mapping data
described above. Generated runtime tables contain only numeric mapping facts.
No Unisys manual or PDF is included in the source distribution or NuGet
package. The availability of numeric facts does not independently establish a
license to reproduce the manual or derivative prose.
