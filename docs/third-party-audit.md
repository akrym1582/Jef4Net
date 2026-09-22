# Third-party provenance and distribution audit (2026-09-22)

This is an evidence inventory, not legal advice. The audit compared current
`main` (`c5971948b447b7ea0268a717fd9ac85356ce3a1e`), repository history, generator
inputs, generated outputs, project dependencies, and package configuration.
Unknown provenance is reported as unknown rather than inferred from a similar
public file.

## Results inventory

| Target | Origin | Applicable terms | In NuGet | Status | Action |
|---|---|---|---|---|---|
| Jef4Net C# source | Repository contributors; author list includes the maintainer, GitHub Actions, and Copilot bot commits | Repository MIT declaration | Yes | Declaration consistent with `.nuspec` | Keep `LICENSE` and MIT expression; authorship metadata alone was not used to decide ownership |
| 12 jef4j JSON files | jef4j 0.14.2 commit `40d13b0…`, `src/test/resources/*.json` | Upstream says mapping files are “CC-O (Public Domain 相当)” | Derived static tables only | Source paths and hashes verified; scope includes all 12 inputs | Preserve hashes, pinned README, and qualified wording |
| jef4j README | Same pinned commit | Apache-2.0 | No | Byte-identical retained document | Keep Apache text in source and package because the notice references it |
| jef4j Java/tests | Same repository | Apache-2.0 | No files copied | History/name/comment review found no Java or serialized `.dat`; tests do share mapping facts/vectors. Independent-expression status cannot be legally concluded from that alone | Do not assert more than the evidence; review any future copied expression separately |
| ICU IBM UCM files | `unicode-org/icu-data` commit `1c3d36e…` | Unicode License v3 plus included ICU notices; UCM IBM copyright header | Derived static tables only | Exact upstream paths/hashes recorded | Keep `licenses/Unicode-ICU.txt`; scope it to ICU data |
| JIS0201/0208/0212 snapshots | Claimed Unicode files, but introducing commit has no URL/raw bytes/headers/tool | **Unresolved**; Unicode-3.0 not inferred | Derived LETS-J/JBIS tables | Local hashes known; upstream hash, version date, notice, and transformation unknown | Do not change values or add related data until provenance/rights review is complete |
| `US-ASCII-QUOTES.TXT` | Two LETS-J overrides of unknown authorship/source | **Unresolved** | Derived LETS-J table | No evidence that it is a Unicode file or shares Unicode terms | Retain compatibility values; require primary source and terms before changes |
| JBIS structured reference | Unisys manual 8600 0288-305, MCP 7.0, sections 12–13 (as recorded initially) | **Unresolved** | Derived table | Original URL and page/table locators unavailable; shared AI conversation is not primary evidence | Verify against an authorized manual and add per-section page/table locators before extending |
| NuGet runtime dependencies | None | N/A | No dependency groups with runtime packages | Project inspection confirmed analyzer is private build-time only | Continue package inspection in CI |
| Test/build dependencies | Microsoft.NET.Test.Sdk, xUnit, runner, StyleCop analyzer | Their own package terms | Not runtime package content | Declared only for test/build or private analyzer use | Normal dependency maintenance |
| PDFs/images/fonts/binary/serialized data | Repository-wide filename/type scan | N/A | No | None found; generated `.g.cs` files are text and trace to inputs above | Repeat inventory when adding assets |

## JIS investigation and known difference

The four files first appear together in commit `0f1bed09bac9a1982fa2773a85d0b415675f41a0`.
That commit supplies already-reduced files and labels them as Unicode data, but
contains neither an acquisition URL nor original headers, hashes, or a script.
No earlier Git object explains their creation. Consequently the recorded
2026-09-21 retrieval date is a historical claim, not independently reproducible
evidence.

The stored `JIS0208.TXT` record `0x2140 → U+FF3C` differs from the Unicode
official-table value `U+005C` identified in the audit request. No repository
record establishes a deliberate LETS-J/JBIS profile override, a separate
source, or a mechanical transform. Changing it might break an intended vendor
profile, so this audit deliberately makes **no mapping changes**. Likewise,
`US-ASCII-QUOTES.TXT` contains bespoke-looking quote overrides but no author or
terms. `data/unisys/provenance.json` records these facts and nulls rather than
inventing exact source fields.

Network retrieval from `unicode.org` was blocked by the execution environment
(HTTP proxy 403), but that limitation is not the reason for the unresolved
finding: even a current similarly named file would not prove which bytes and
terms the 2026 introducing commit actually used.

## Code and history review boundaries

The review searched file types, copyright/license/SPDX/NOTICE strings,
generator inputs, dependencies, and all Git author identities. It compared the
pinned jef4j tree and README with the retained inputs. It did not make a legal
determination from authorship or similarity, and it cannot prove an absence of
all conceptual influence. No third-party PDFs, images, fonts, binary blobs,
Java sources, or serialized `.dat` assets are present in the working tree.

## Unresolved items and release impact

1. Identify the exact acquired versions and stable URLs for the three JIS
   tables, obtain raw hashes/notices, and reconstruct the normalization.
2. Establish the source, author, terms, and rationale for both quote overrides.
3. Resolve `0x2140` using primary LETS-J/JBIS evidence; if intentional, manage it
   as a documented overlay instead of silently modifying an upstream table.
4. Locate an authorized copy of Unisys document 8600 0288-305 and add page/table
   locators for every byte layout, mixed profile, and SBCS map in the JSON.
5. Have a qualified human decide whether unresolved JIS/JBIS rights permit the
   next release. Until then, do not add mappings based on these unknown sources.

## Proposed next-release note

> Package licensing metadata remains MIT. Since 0.8.1, the package includes
> `LICENSE`, `THIRD-PARTY-NOTICES.md`, and referenced third-party license texts.
> CI now verifies the license expression, exact package paths, notice references,
> provenance hashes, and generated tables. Mapping values were not changed.
> Provenance and permission for the historical JIS/LETS-J snapshots and detailed
> Unisys JBIS locators remain under maintainer review.
