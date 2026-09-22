# JBIS / Unisys MLS reference

This repository snapshot records the byte layouts and SBCS cells used to generate the JBIS implementation. Its primary specification is Unisys *MultiLingual System (MLS) Administration, Operations, and Programming Guide*, ClearPath MCP 7.0, November 2001, document 8600 0288-305, sections 12–13.

The machine-readable companion is [`JBIS_Unisys_MLS_reference.json`](JBIS_Unisys_MLS_reference.json). JIS-to-Unicode scalar values are not duplicated there: CodeGen consumes the JIS snapshots in `data/unisys`, whose provenance remains unresolved.

Repository history contains no stable original-manual URL and no page or table
numbers for the extracted values. Sections 12 and 13 are therefore the narrowest
currently verified locators. A ChatGPT shared conversation used during the
initial implementation is retained in the JSON only as a non-authoritative
research lead; it is not treated as a source. Before adding or correcting
values, inspect an authorized copy of document 8600 0288-305 and record the
page/table locator for each JSON section. No manual prose, headings, figures, or
PDF are intentionally reproduced here.

## DBCS layouts

| CCS | JIS X 0208 | JIS X 0212 | Custom |
|---|---|---|---|
| JBIS7 | `21–7E / 21–7E` | `A1–FE / 41–9E` | `41–9E / A1–FE` |
| JBIS8 | `A1–FE / A1–FE` | `A1–FE / 41–9E` | `41–9E / A1–FE` |

Custom cells deliberately have no Unicode mapping and use `DecoderFallback`.

## Mixed profiles

| Encoding | SBCS table | SDO | EDO | DBCS |
|---|---|---:|---:|---|
| JISASCIIJBIS7 | JISASCII | `9E` | `9F` | JBIS7 |
| JapanEBCDICJBIS8 | JapanEBCDIC | `2B` | `2C` | JBIS8 |
| JapanV24JBIS8 | JapanV24 | `2B` | `2C` | JBIS8 |

The JSON contains each defined SBCS byte as a hexadecimal byte-to-Unicode-scalar pair; absent cells are undefined. It is retained as reviewable source data, while runtime conversion uses generated static tables only.
