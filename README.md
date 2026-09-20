# Jef4Net

富士通 JEF / EBCDIC を `System.Text.Encoding` として扱う .NET ライブラリです。
**.NET Standard 2.1 / .NET 10** を対象とし、ランタイム依存パッケージはありません。

```csharp
using System.Text;
using Jef4Net.Fujitsu;

Encoding.RegisterProvider(FujitsuEncodingProvider.Instance);
Encoding encoding = Encoding.GetEncoding("x-Fujitsu-EBCDIC-Lower+JEF");
byte[] bytes = encoding.GetBytes("aあb海c");
string text = encoding.GetString(bytes);
// bytes: 81 28 A4 A2 29 82 28 B3 A4 29 83
```

移行時に変換不能文字や不正データを検出する場合は例外 fallback を指定してください。

```csharp
Encoding strict = Encoding.GetEncoding(
    "x-Fujitsu-JEF",
    EncoderFallback.ExceptionFallback,
    DecoderFallback.ExceptionFallback);
```

`StreamReader` / `StreamWriter`、配列、文字列、Span API に対応します。
分割処理には同じ `GetEncoder()` / `GetDecoder()` の戻り値を使い、最後に
`flush: true` で呼び出してください。`Convert` では消費数と出力数を確認し、
`completed` になるまで残りの入力を渡します。出力容量1でも処理できます。

## 対応文字セット

- `x-Fujitsu-JEF`
- `x-Fujitsu-JEF-Roundtrip`
- `x-Fujitsu-JEF-HanyoDenshi`
- `x-Fujitsu-EBCDIC-Lower`
- `x-Fujitsu-EBCDIC-Kana`
- `x-Fujitsu-EBCDIC-Ascii`
- `x-Fujitsu-EBCDIC-{Lower|Kana|Ascii}+JEF`（3形式）
- `x-Fujitsu-JEF+EBCDIC-{Lower|Kana|Ascii}`（3形式）
- `x-Fujitsu-EBCDIC-{Lower|Kana|Ascii}+JEF-HanyoDenshi`（3形式）
- `x-Fujitsu-JEF-HanyoDenshi+EBCDIC-{Lower|Kana|Ascii}`（3形式）

名前の大文字・小文字は区別せず、`x-Fujitsu-` を省略した別名も使用できます。
独自のコードページ番号は割り当てません。数値による provider 検索は `null` を返します。
`Encoding.CodePage` の値0は識別子として利用しないでください。

混在形式は `+` の左側から開始します。デコードは K (`28`)、K1 (`38`)、
K2 (`30 E2`)、A (`29`) を受け入れ、エンコードは K/A を使用します。
エンコード完了時には初期状態へ戻ります。EBCDICで表現可能な文字を優先します。
混在時の `28` / `29` / `30` / `38` はシフト構文として予約され、対応する
EBCDIC制御文字を通常文字としてエンコードする場合は fallback になります。

## マッピングと fallback

マッピングは jef4j 0.14.2 の固定コミットに由来します。出典とライセンスは
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) を参照してください。
ランタイムでJSON解析やファイル読み込みは行いません。

通常プロファイルの規則は次のとおりです。

- `hd` / `aj1` の異体字プロファイルは除外します。`sp` の付加文字は保持し、
  エンコードでは最長一致で扱います。
- `unmappable` は未定義として扱います。`decode_only` / `encode_only` は
  その方向だけで使用します。
- `oneway` / `compatible` / `substitution` / `cjk_ci` は通常プロファイルに含めます。
  `substitution` は上流の通常マッピングの属性です。独自の類似文字置換はしません。
- 上流の通常プロファイル試験と同じく、重複候補はJSON内で後の定義が優先です。
  デコードの重複は後続 `cjk_ci`、エンコードの重複は `oneway` または
  `substitution` が関わるものに限定して検証します。その他の競合は生成エラーです。
  例: `42BB` → `U+FA30`、`U+4FAE` → `C9EE`。
- JEF `80A1`～`A0FE`（各行の下位バイト `A1`～`FE`）は
  `U+E000`～`U+EC1D` へ規則変換します。`A1A1` は全角空白への片方向互換です。

Roundtripは通常JEFから `oneway` / `unmappable` を除き、方向属性とJSONの順序を
適用したあと、エンコード・デコードが互いに一致する10,868組だけを採用します。
PUAの3,102組も双方向で維持します。例外fallbackを指定した場合、採用済みの
**1文字対応単位**について `Encode(Decode(code)) == code` と
`Decode(Encode(Unicode列)) == Unicode列` を保証します。`A1A1` はRoundtripでは
変換不能です。置換が発生した入力、文字列連結によって最長一致の区切りが変わる場合、
混在形式のシフトバイトを含む元ファイル全体のバイト復元はこの保証に含みません。
上流jef4jのRoundtripと採用範囲が異なる可能性があります。

HanyoDenshiは固定JSONの `hd` をUnicode IVSのセレクタとして扱います。
例: JEF `41 A5` ⇄ `U+4E08 U+E0103`（`丈󠄃`）。通常レコードも含め、
`aj1` は含めません。`decode_only` / `encode_only` / `variant_only` とJSONの順序に従い、
セレクタなしの別名も上流の規則で扱います。片方向対応があるため、全件の往復性は
保証しません。既知のIVSは最長一致でエンコードし、未知のセレクタは基底文字を
処理したあと標準fallbackへ渡します。Unicode正規化や任意のIVDコレクション、
フォント描画は行いません。

```csharp
Encoding ivsEncoding = Encoding.GetEncoding(
    "x-Fujitsu-JEF-HanyoDenshi",
    EncoderFallback.ExceptionFallback,
    DecoderFallback.ExceptionFallback);
byte[] ivsBytes = ivsEncoding.GetBytes("\u4E08\U000E0103"); // 41 A5
```

既定のデコード置換は `U+FFFD` です。既定のエンコード置換は単体EBCDICでは
`?`、JEFと混在形式では全角空白です。混在形式でも通常の文字としてシフトを伴って
`40 40` を出力するため、文字としての置換内容が状態に依存しません。
任意の .NET replacement / exception / custom fallback を指定できます。
標準 `EncoderReplacementFallback` は未対応サロゲートペアに置換文字列を2回返します。
置換文字自体が変換不能の場合は `ArgumentException` を送出します。
例外の `Index` はその呼び出しの入力基準で、前チャンクの保留文字では負になる場合があります。

不完全なJEF文字、K2、サロゲートは flush まで保留します。不正なJEF後続バイトは
次の入力として再評価し、シフトでの復帰を可能にします。`Convert` は出力に収まらない
変換結果を内部に保留できるため、入力消費数が出力数より先行することがあります。
`GetByteCount` / `GetCharCount` は現在の状態を変更せずに計算します。
`Reset()` はシフトと保留中の入力・出力を破棄します。Encoder/Decoderは個別に状態を持ち、
同じインスタンスを複数スレッドから同時に呼び出す用途には使用しないでください。

## ビルド・試験・パッケージ

開発には .NET 10 SDK を使用します。

```sh
dotnet build -c Release
dotnet test -c Release
dotnet pack -c Release -o artifacts
```

ライブラリ自体は外部パッケージ不要です。テスト用パッケージは初回 restore が必要です。
生成済みテーブルを同梱しているため、通常のビルド時に上流へアクセスしません。
再生成は次のコマンドで行います。

```sh
dotnet run --project src/Jef4Net.CodeGen -c Release -- \
  data/jef4j src/Jef4Net/Fujitsu/Internal/Generated/FujitsuTables.g.cs
```

テストは全JSONの通常・Roundtrip・HanyoDenshiマッピング、JEF全65,536コード、PUA全領域、
既知ベクトル、全入力分割位置、1byte/1char入力、小容量出力、fallback、
状態の独立性、ストリーム統合を検証します。

パッケージの導入試験と簡易計測:

```sh
dotnet restore samples/PackageSmoke --source "$PWD/artifacts"
dotnet run --project samples/PackageSmoke -c Release --no-restore
dotnet run --project samples/PackageSmoke -c Release --no-restore -- --benchmark
```

.NET Standard 2.1 のアセンブリに対して同じ試験を実行する場合:

```sh
dotnet test tests/Jef4Net.Tests -c Release -p:LibraryTargetFramework=netstandard2.1
```

## 非対応範囲

AdobeJapan1、任意IVD、Roundtrip混在形式、利用者独自の外字テーブル、
富士通公式の「領域重視」変換、COPY句、PIC / COMP / COMP-3、
COBOLレコード構造、CSVやDBへの移行処理は対象外です。
上流JSONと既知ベクトルに対する互換性を試験しています。富士通の公式実装ではありません。

ライブラリ: Apache-2.0。マッピングデータ: 上流のCC0宣言に基づき利用。
