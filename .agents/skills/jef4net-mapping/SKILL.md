---
name: jef4net-mapping
description: Jef4Netの上流マッピングJSON、選択規則、CodeGen、生成テーブルを更新・検証する。文字対応の修正や上流データ更新に使用する。
---

# Jef4Net マッピング保守

コマンドはリポジトリルートで実行する。
[READMEのマッピング仕様](../../../README.md)、
[出典情報](../../../data/jef4j/provenance.json)、
[MappingGenerator.cs](../../../src/Jef4Net.CodeGen/MappingGenerator.cs) を変更範囲に応じて確認する。

## データと選択規則

- `data/jef4j/` の4つのJSONは上流の原本として扱う。生成物や原本への手修正で
  変換結果だけを合わせず、上流更新なのか選択規則のバグなのかを特定する。
- 上流更新ではコミットを固定し、4ファイルの整合性とライセンス宣言を確認する。
  原本のバイト列を保存し、`provenance.json` のバージョン・SHA・取得日・SHA-256、
  `UPSTREAM-README.md`、`THIRD-PARTY-NOTICES.md` を取得内容に合わせる。
- 通常プロファイルは `hd` / `aj1` と `unmappable` を除外し、`sp` を保持する。
  `decode_only` / `encode_only` を尊重する。単方向マッピングを一律round-trip試験にしない。
- 現在の重複解決はJSON後勝ち。デコード重複は後続 `cjk_ci`、エンコード重複は
  `oneway` / `substitution` に関係するものを許容する。新しい競合はレコードと
  上流の根拠を調べ、検証器を緩めるだけで受け入れない。
- PUAは `Mapping.cs` の規則変換。上流データの更新でテーブルと衝突しないか確認する。
- `FujitsuTables.g.cs` は生成物。変更は入力または生成器へ加えて再生成する。
  ランタイムへのJSON依存やJavaシリアライズ資産を追加しない。

## 生成と試験

```sh
dotnet run --project src/Jef4Net.CodeGen -c Release -- \
  data/jef4j src/Jef4Net/Fujitsu/Internal/Generated/FujitsuTables.g.cs
dotnet test tests/Jef4Net.Tests -c Release --filter 'FullyQualifiedName~GeneratorTests|FullyQualifiedName~AllMappingsAndEveryCode'
```

再現性は生成先を一時ファイルに変えて確認する（POSIXシェルの例）。

```sh
jef_generated=$(mktemp)
dotnet run --project src/Jef4Net.CodeGen -c Release --no-build -- data/jef4j "$jef_generated"
cmp src/Jef4Net/Fujitsu/Internal/Generated/FujitsuTables.g.cs "$jef_generated"
rm "$jef_generated"
```

生成器と同じ実装を期待値として使わず、元JSONの対象レコードと意図した選択を
独立した回帰試験に残す。競合例や方向属性を変更するなら、既知ベクトルも明示する。
生成物の差分を確認した後、AGENTS.mdにある両ターゲットの全試験を実行する。
報告には上流コミット、変わった対応関係、再現性と試験結果を含める。
