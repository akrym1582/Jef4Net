# v0.1 実装・検証記録

検証日: 2026-09-20 / .NET SDK 10.0.401 / Linux x64。
ユーザー指定に合わせ、対象フレームワークをnetstandard2.1とnet10.0とした。

| フェーズ | 実装と確認結果 |
|---|---|
| 1 | 4つのCC0 JSONを固定コミットから無改変で保存。provenance.jsonにSHA-256を記録。CodeGenで範囲、Unicode、方向属性、重複・競合を検証。再生成物のバイト一致を確認。 |
| 2 | 単体JEF・3種EBCDIC、PUA全3,102文字、Unicode付加文字列、count/write共通Coreを実装。JSONから独立に期待値を構築し、全マッピングとJEF全65,536コードを試験。 |
| 3 | 6混在形式、K/K1/K2/A、初期状態へのflush復帰を実装。既知ベクトル、シフト途中の分割、fallbackを試験。 |
| 4 | Encoding・Encoder・Decoder・Provider、配列・文字列・Span、ストリームを実装。消費数、生成数、completed、Reset、状態独立性、出力不足、count非破壊性を試験。 |
| 5 | 全入力分割位置、出力容量1/2/3/7、容量0からの継続、1byte/1char入力、ランダム不正バイト列、サロゲート分割を試験。両ターゲットで46件成功。通常Span変換のヒープ割り当て0を確認。 |
| 6 | README、XML API docs、第三者通知、NuGet metadata、SDK組込みSource Link、CIを整備。Release build/pack、パッケージ参照のみの別プロジェクトでサンプル成功。パッケージにJSON/Java/.datがないことを確認。 |

## 実行した主要コマンド

```sh
dotnet restore
dotnet build -c Release --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test -c Release --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test tests/Jef4Net.Tests -c Release --no-restore -m:1 \
  -p:UseSharedCompilation=false -p:LibraryTargetFramework=netstandard2.1
dotnet run --project src/Jef4Net.CodeGen -c Release -- \
  data/jef4j /tmp/jef4net-generated.cs
cmp src/Jef4Net/Fujitsu/Internal/Generated/FujitsuTables.g.cs /tmp/jef4net-generated.cs
dotnet pack -c Release --no-build --no-restore -m:1 -o artifacts
dotnet restore samples/PackageSmoke --source "$PWD/artifacts"
dotnet run --project samples/PackageSmoke -c Release --no-restore
```

この実行環境ではSDKのホームとNuGetキャッシュを/tmp以下へ設定した。
VSTestのローカルソケット通信には環境の実行権限拡張を使用した。
CI設定は追加済みだが、リモートへのpushやGitHub Actions実行は行っていない。
NuGetへの公開も行っていない。

## 設計上の具体化

- JEFの65536要素のushort ID表から、最大2 Unicode scalarを保持するエントリへ参照する。
  逆引きは静的なソート済みキー配列で、巨大な辞書の実行時生成は行わない。
- 通常プロファイルの競合解決は上流テストと同じJSON後勝ち。
  69件のデコード重複はCJK互換文字を優先し、エンコードの重複は
  oneway/substitutionに関連する定義のみ許容する。
- Encoder/Decoderは変換済み出力を少量保留できるため、1byte/1char出力にも対応する。
  出力がない呼び出しでも入力を消費することがある。消費した情報は次回まで保持する。
- 既定の混在エンコード置換は全角空白を通常の状態機械で出力する。
  EBCDIC状態で直接40 40を出力する方式ではなく、K/Aを伴って文字の意味を保つ。
- .NETのEncoderReplacementFallbackは未対応サロゲートペアを2回置換する。
  独自に1回へまとめず、その標準契約を維持する。
- 上流互換性は固定JSON全件と上流テストの期待値で検証した。
  Java実装を起動する差分テストや富士通実機との比較は実施していない。
- 通常ビルドは生成済みC#テーブルを使用する。JSON変更時の再生成とCIの差分検出で同期を管理する。
