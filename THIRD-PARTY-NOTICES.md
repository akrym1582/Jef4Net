# Third-party notices

## jef4j mapping data

- Upstream: https://github.com/hidekatsu-izuno/jef4j
- Version in upstream pom.xml: 0.14.2
- Commit: `40d13b0d92963c9b36993d1d742cdfc10c25b0f6`
- Retrieved: 2026-09-20 (UTC)
- Files: `src/test/resources/fujitsu_jef_mapping.json`,
  `fujitsu_ebcdic_lower_mapping.json`, `fujitsu_ebcdic_kana_mapping.json`,
  `fujitsu_ebcdic_ascii_mapping.json`, `hitachi_ebcdic_mapping.json`,
  `hitachi_ebcdik_mapping.json`, `hitachi_keis78_mapping.json`, and
  `hitachi_keis83_mapping.json`
- Data license: CC0 / public domain dedication. Upstream README spells this
  “CC-O (Public Domain 相当)” and explicitly applies it to `src/test/resources/*.json`.
  An unmodified README snapshot is retained in `data/jef4j/UPSTREAM-README.md`.
- Upstream author: Hidekatsu Izuno and contributors.

The eight JSON files are preserved verbatim. Generated `FujitsuTables.g.cs` and
`HitachiTables.g.cs` derive from these files. No Java serialized `.dat` assets are used. The conversion
implementation is written for .NET; upstream Apache-2.0 test expectations were
consulted to verify normal-profile selection and known conversion vectors.
The retained upstream README is covered by upstream's Apache-2.0 license.

Jef4Net's implementation is distributed under Apache-2.0; see LICENSE.

## Unicode JIS mapping data

- Files: `JIS0201.TXT`, `JIS0208.TXT`, `JIS0212.TXT`, `US-ASCII-QUOTES.TXT`
- Distribution source: Unicode Character Database mapping data
- Retrieved: 2026-09-21 (UTC)
- License: Unicode License v3 (https://www.unicode.org/license.txt)

The normalized snapshots in `data/unisys` retain the two-column mappings used by
CodeGen. JIS X 0201 contributes kana; JIS X 0208 and JIS X 0212 are transformed
to LETS-J byte regions. JISASCII quotes and the canonical `2020` space are explicit.

The same pinned upstream commit also supplies the unmodified NEC files
`nec_jis8_mapping.json`, `nec_ebcdik_mapping.json`,
`nec_jis8_ebcdik_mapping.json`, and `nec_jips_mapping.json`. They are covered by
the CC0 declaration above. `NecTables.g.cs` is generated from them; the Apache-2.0
Java NEC encoder/decoder were consulted for byte-shift and profile behavior, but
no Java code or serialized `.dat` file is included.
