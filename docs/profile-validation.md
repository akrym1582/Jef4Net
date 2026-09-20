# Roundtrip / HanyoDenshi 実装時の検証

対象: Jef4Net 0.2.0、.NET Standard 2.1 / .NET 10。
入力データは `data/jef4j/provenance.json` の固定コミット
`40d13b0d92963c9b36993d1d742cdfc10c25b0f6`。JSON原本は変更していない。

Roundtripの候補選択では `oneway` / `unmappable` と `hd` / `aj1` を除外し、
方向属性とJSONの後勝ち規則を適用する。候補はデコード10,890コード、
エンコード10,886Unicode列。両方向が一致しないコード起点22件、
Unicode列起点18件を除いた10,868組を採用した。PUAは別途3,102組を規則変換する。
`A1A1 → U+3000 → 4040` は通常JEFの片方向互換例で、Roundtripから除外した。
上流のRoundtrip実装と完全な採用範囲一致は保証しない。

HanyoDenshiでは固定JSONの `hd` 3,799レコードを方向属性に従って扱う。
デコードは `hd` がある場合にセレクタ付き列を採用し、エンコードでは
`variant_only` でないレコードのセレクタなし別名と、セレクタ付き列を別々に選ぶ。
`41A5 → U+4E08 U+E0103`、`42BB → U+FA30`、
`U+585A → 47C9` を既知ベクトルとして検証した。補助平面基底文字と
セレクタをともに含む `41A6 ↔ U+2000B U+E0101` も分割試験に用いた。

試験は元JSONから生成器と独立に期待表を組み立て、新プロファイルの全65,536
JEFコード、採用エンコード列、PUA、混在6形式、各UTF-16・バイト分割位置、
小容量出力とfallbackを確認する。通常JEF・EBCDICの全件試験も継続する。

2026-09-20のローカル検証: Release buildは警告0・エラー0。
.NET 10参照で73件、.NET Standard 2.1参照で73件の試験が成功した。
CodeGenの再生成結果はバイト一致。0.2.0のnupkgとsnupkgを作成し、
別の`PackageSmoke`プロジェクトが作成したnupkgだけを参照して新旧の
既知ベクトルを処理した。nupkgにはJSON・Javaファイルを含まない。
リモートCIはこの記録では実行していない。
