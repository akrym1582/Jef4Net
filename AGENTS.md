# Jef4Net 開発ガイド

## 対象と参照先

Jef4Netは富士通JEF・EBCDICを `System.Text.Encoding` として提供するライブラリ。
ライブラリは `netstandard2.1` / `net10.0`、CodeGenとテストは `net10.0` を対象とする。
開発には .NET 10 SDKを使用する。

- 現在の仕様、対応形式、制限、利用例: [README.md](README.md)
- 出典・ライセンス: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)
- 上流の固定コミット・取得日・ハッシュ: [data/jef4j/provenance.json](data/jef4j/provenance.json)
- 初期実装時の検証履歴: [docs/implementation-validation.md](docs/implementation-validation.md)

初期実装の検証履歴は当時の記録であり、現在のテスト成功を保証するものではない。
既存の対象外機能を追加する場合は、その依頼の範囲に合わせて仕様・テスト・READMEを更新する。

## コードの配置と設計

| 場所 | 役割 |
|---|---|
| `src/Jef4Net/Fujitsu/` | Encoding、状態付きEncoder/Decoder、名前解決Provider |
| `src/Jef4Net/Fujitsu/Internal/` | Spanを扱う変換Core、状態、マッピング参照 |
| `src/Jef4Net/Fujitsu/Internal/Generated/` | CodeGenの生成物。直接編集しない |
| `src/Jef4Net.CodeGen/` | JSON検証と決定的な静的テーブル生成 |
| `data/jef4j/` | 無改変の上流データと出典情報 |
| `tests/Jef4Net.Tests/` | 全マッピング、分割変換、fallback、生成器の試験 |
| `samples/PackageSmoke/` | プロジェクト参照を使わないNuGet導入試験・簡易計測 |

- 変換規則はCoreに置き、Encodingアダプターに重複実装しない。
- countとwriteは共通ロジックを使い、count呼び出しでEncoder/Decoderの状態を変えない。
- Encoder/Decoderごとに独立した状態を保ち、共有テーブルは不変にする。
- 通常の変換経路で文字ごとのallocation、JSON解析、ファイルI/Oを追加しない。
- 公開APIは最小限にし、追加時はXML documentationと両ターゲットの互換性を確認する。
- マッピングの曖昧さは上流レコードと方向属性を調べて解決し、推測で文字を置き換えない。

## 作業別スキル

該当する変更を行うときに、次のスキルを読む。文書だけの変更では不要。

- JSON取込み、マッピングの選択規則、CodeGen、生成テーブル:
  [jef4net-mapping](.agents/skills/jef4net-mapping/SKILL.md)
- Encoding API、状態機械、fallback、分割変換、性能の修正:
  [jef4net-encoding](.agents/skills/jef4net-encoding/SKILL.md)

## 検証

以下はリポジトリルートで実行する。

```sh
dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-restore
dotnet test tests/Jef4Net.Tests -c Release --no-restore -p:LibraryTargetFramework=netstandard2.1
```

まず変更に対応する試験を実行し、変換処理・マッピング・公開APIを変更した場合は
両ターゲットで全試験を行う。テスト実行ホスト自体はどちらも .NET 10。
生成器を変更した場合は再生成とバイト一致も確認する。
文書・作業指針だけの変更ではリンクやスキル定義の検証でよく、全変換試験の再実行は不要。

パッケージ構成を変更した場合は `dotnet pack -c Release -o artifacts` と
READMEのPackageSmoke手順で確認する。ローカルで同じバージョンを再作成した場合は、
試験用の新しい `NUGET_PACKAGES` ディレクトリを使い、以前のパッケージのキャッシュを避ける。
バージョンを変更するときはPackageSmokeのPackageReferenceも揃える。

環境がホームへの書込みを制限する場合は、`DOTNET_CLI_HOME` と `NUGET_PACKAGES` を
書込み可能な場所へ設定する。MSBuildやコンパイラーサーバーの制約がある場合は
`-m:1 -p:UseSharedCompilation=false` を利用できる。
VSTestのローカル通信が環境に遮断された場合は、テスト失敗とは分けて報告する。

完了報告には、変更した挙動、実行した検証と結果、残る制約を記載する。
ローカルでの成功とリモートCIの実行結果は区別する。
