# Third-party notices

Jef4Net's .NET source code is distributed under the [MIT License](LICENSE).
That license does not replace terms that may apply to third-party data or
retained documents. The following inventory distinguishes confirmed terms from
items whose provenance is still unresolved. License paths are package-relative
and are included in both the source tree and the NuGet package.

## ICU IBM converter mapping data

- **Used files / generated output:** `data/icu/ibm/ibm-1390_P110-2003.ucm`
  and `ibm-1399_P110-2003.ucm`; generated `IbmTables.g.cs`.
- **Upstream:** https://github.com/unicode-org/icu-data at commit
  `1c3d36e741bd648caaaba3a744267b23ca41bfc1` (retrieved 2026-09-21 UTC).
- **Use and transformation:** verbatim UCM snapshots; CodeGen selects SBCS and
  common DBCS mappings and emits static tables. ICU is not a runtime dependency.
- **Terms and notice:** Unicode License v3 and the ICU third-party notices in
  [`licenses/Unicode-ICU.txt`](licenses/Unicode-ICU.txt). The UCM headers state
  “Copyright (C) 1995-2003, International Business Machines Corporation and
  others. All Rights Reserved.”

`licenses/Unicode-ICU.txt` is included for these ICU snapshots. Its presence
must not be read as evidence that it governs the unresolved JIS snapshots below.

## jef4j mapping data and retained README

- **Used files / generated output:** the 12 Fujitsu, Hitachi, and NEC JSON files
  enumerated in `data/jef4j/provenance.json`; generated `FujitsuTables.g.cs`,
  `HitachiTables.g.cs`, and `NecTables.g.cs`.
- **Upstream:** https://github.com/hidekatsu-izuno/jef4j, version 0.14.2,
  commit `40d13b0d92963c9b36993d1d742cdfc10c25b0f6` (retrieved 2026-09-20 UTC).
- **Use and transformation:** JSON snapshots are byte-identical to
  `src/test/resources/*.json`; CodeGen selects direction/profile records and
  emits static lookup tables. No upstream Java or serialized `.dat` is shipped.
- **Terms:** the pinned upstream README says mapping files are distributed as
  “CC-O (Public Domain 相当)”; its wording covers `src/test/resources` mapping
  files, including all 12 used JSON files. This notice reports that upstream
  statement without making an independent legal characterization.
- **Retained prose:** `data/jef4j/UPSTREAM-README.md` is an unmodified copy of
  the Apache-2.0 upstream README. Apache license text:
  [`licenses/Apache-2.0.txt`](licenses/Apache-2.0.txt). Upstream author:
  Hidekatsu Izuno and contributors.

A history/source comparison found no copied Java source or serialized asset.
Some implementation behavior and tests use mapping facts and compatibility
vectors also present upstream. That evidence is insufficient to assert that all
.NET implementation expression was created without reference to upstream code;
the audit does not change Jef4Net's MIT declaration or claim a legal conclusion.

## JIS snapshots used by LETS-J and JBIS — provenance unresolved

- **Used files / generated output:** `data/unisys/JIS0201.TXT`, `JIS0208.TXT`,
  `JIS0212.TXT`, and `US-ASCII-QUOTES.TXT`; generated `UnisysTables.g.cs` and
  `JbisTables.g.cs`.
- **Recorded claim:** the introducing commit
  `0f1bed09bac9a1982fa2773a85d0b415675f41a0` called these Unicode mapping data
  retrieved 2026-09-21. It recorded no exact URL, raw file, raw hash, upstream
  header/version date, or reproducible normalization procedure.
- **Known transformation/difference:** the snapshots contain reduced two-column
  records. `JIS0208.TXT` maps JIS `0x2140` to `U+FF3C`, whereas the Unicode
  official table identified for comparison maps it to `U+005C`. History does
  not establish whether this is a LETS-J/JBIS profile override, a different
  source, or an undocumented edit, so the compatibility value is unchanged.
  `US-ASCII-QUOTES.TXT` labels itself a LETS-J override and has no recorded
  upstream author, terms, or source.
- **Terms and notice:** unresolved. In particular, Unicode License v3 and its
  copyright notice are **not asserted** for these files. Exact local hashes and
  all unknown fields are recorded in `data/unisys/provenance.json`.

Until an authoritative source and applicable permission are documented, these
snapshots and new data derived from the same unknown source should not be added
or refreshed. Generated tables mean these values are present in NuGet even
though the text snapshots themselves are not packed. A maintainer must resolve
rights and compatibility before a release that relies on a new or replaced JIS
source. This is an identified review issue, not a conclusion that distribution
is permitted or prohibited.

## Unisys JBIS specification reference — locators unresolved

- **Reference:** Unisys *MultiLingual System (MLS) Administration, Operations,
  and Programming Guide*, document 8600 0288-305, ClearPath MCP 7.0, November
  2001, sections 12 and 13.
- **Used files / generated output:** the independently formatted inventory
  `data/jbis/JBIS_Unisys_MLS_reference.md` and structured numeric records in its
  `.json` companion; generated `JbisTables.g.cs`.
- **Transformation:** numeric SBCS cells, byte ranges, and shift values were
  structured; no manual PDF, figures, table headings, or explanatory prose is
  included.
- **Locator and terms status:** a stable original URL and page/table numbers
  were not available in repository history or the materials reviewed. The
  former ChatGPT shared conversation is only a non-authoritative research lead,
  not a primary source. Permission applicable to the extracted material has not
  been independently established.

The repository and NuGet do not include the manual/PDF. Before adding values or
claiming specification conformance, a maintainer must inspect an authorized
copy, record a stable source plus page/table locators for every JSON section,
and review the applicable terms.
