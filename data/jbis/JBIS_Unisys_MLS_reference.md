# JBIS / Unisys MLS reference

This repository snapshot records the byte layouts and SBCS cells used to generate the JBIS implementation. Its primary specification is Unisys *MultiLingual System (MLS) Administration, Operations, and Programming Guide*, ClearPath MCP 7.0, November 2001, document 8600 0288-305, sections 12–13.

The supplied [shared implementation reference](https://chatgpt.com/share/6ab131c7-4348-83ee-b53c-97f4fa4cbfbb?ogimg=plain) was normalized into this repository. The machine-readable companion is [`JBIS_Unisys_MLS_reference.json`](JBIS_Unisys_MLS_reference.json). JIS-to-Unicode scalar values are not duplicated there: CodeGen consumes the pinned Unicode `JIS0208.TXT` and `JIS0212.TXT` snapshots in `data/unisys`.

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
