# Jef4Net

富士通 JEF、日立 KEIS、NEC JIPS、IBM Japanese Host、Unisys LETS-J など、日本のメインフレーム文字コードを `System.Text.Encoding` として扱う .NET ライブラリです。
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

日立KEISは専用providerを登録します。

```csharp
using Jef4Net.Hitachi;

Encoding.RegisterProvider(HitachiEncodingProvider.Instance);
Encoding keis = Encoding.GetEncoding(
    "x-Hitachi-EBCDIK+KEIS83",
    EncoderFallback.ExceptionFallback,
    DecoderFallback.ExceptionFallback);
```

NEC JIPSも専用providerを登録します。

```csharp
using Jef4Net.Nec;

Encoding.RegisterProvider(NecEncodingProvider.Instance);
Encoding jips = Encoding.GetEncoding(
    "x-NEC-EBCDIK+JIPSE-HanyoDenshi",
    EncoderFallback.ExceptionFallback,
    DecoderFallback.ExceptionFallback);
```

IBM Japanese Hostも専用providerを登録します。

```csharp
using Jef4Net.Ibm;

Encoding.RegisterProvider(IbmEncodingProvider.Instance);
Encoding ibm = Encoding.GetEncoding("x-IBM-1390");
byte[] ibmBytes = ibm.GetBytes("ABCあいう漢字");
string ibmText = ibm.GetString(ibmBytes);
```

Unisys LETS-Jも専用providerを登録します。

```csharp
using Jef4Net.Unisys;

Encoding.RegisterProvider(UnisysEncodingProvider.Instance);
Encoding letsj = Encoding.GetEncoding("x-Unisys-LETSJ",
    EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
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
- `x-Hitachi-EBCDIC` / `x-Hitachi-EBCDIK`
- `x-Hitachi-KEIS78` / `x-Hitachi-KEIS83`
- KEIS名に任意で `-ShiftSpaceSingle` を付加した形式
- `x-Hitachi-{EBCDIC|EBCDIK}+{KEIS名}` と、左右を反転した形式
- `x-NEC-JIS8` / `x-NEC-EBCDIK`
- `x-NEC-JIPSJ` / `x-NEC-JIPSE`（任意で `-HanyoDenshi`）
- `x-NEC-JIS8+JIPSJ` / `x-NEC-JIPSJ+JIS8`（JIPS名に任意で `-HanyoDenshi`）
- `x-NEC-EBCDIK+JIPSE` / `x-NEC-JIPSE+EBCDIK`（同上）
- `x-IBM-8482` / `x-IBM-5123` / `x-IBM-16684`
- `x-IBM-8482+16684` / `x-IBM-16684+8482`
- `x-IBM-5123+16684` / `x-IBM-16684+5123`
- `x-IBM-1390`（8482+16684）/ `x-IBM-1399`（5123+16684）
- `x-Unisys-LETSJ` / `x-Unisys-LETSJ-Kanji`

名前の大文字・小文字は区別せず、`x-Fujitsu-` を省略した別名も使用できます。
独自のコードページ番号は割り当てません。Fujitsu/Hitachi/NEC/Unisys providerの数値検索は `null` を返します。
`Encoding.CodePage` の値0は識別子として利用しないでください。

IBM名は `x-IBM-` を含む正式名を受け付けます。1390/1399および各component CCSIDは
数値によるprovider検索にも対応します。混在形式は左側を初期状態とし、`0E` (SO) で
DBCS、`0F` (SI) でSBCSへ切り替えます。エンコード時はSBCSを優先し、flush時は初期状態へ
戻します。16684は補助平面、2 scalar sequence、およびICU定義のPUAを含みます。
互換入力名の11684も受理しますが、公開名と文書ではIBMのCCSID 16684を使用します。

日立名は `x-Hitachi-` を含む正式名だけを受け付けます。混在形式は左側から開始し、
`0A 42` でKEIS、`0A 41` でSBCSへ切り替えます。エンコードのflushでは初期状態へ
戻ります。単体形式ではこのシフト構文を解釈しません。KEISの `4040` は通常
`U+3000`、`ShiftSpaceSingle` では半角空白2文字へデコードします（エンコード時に
半角空白2文字を自動合成はしません）。`81A1`～`A0FE` は `U+E000`～`U+EBBF`
の3,008文字へ規則変換します。
固定した日立マッピングJSONにはHanyoDenshi/IVSデータがないため、日立名の
`-HanyoDenshi` プロファイルは受け付けません。

NEC名は `x-NEC-` を含む正式名だけを受け付け、混在形式の左側を初期状態とします。
J形式は `1A 70` / `1A 71`、E形式は `3F 75` / `3F 76` でJIPS/SBCSへ切り替え、
flush時は初期状態へ戻ります。シフトは混在形式だけで解釈します。JIPS(E)は固定上流の
専用JIS8/EBCDIKバイト表を各漢字バイトに適用します。G0 `7421`～`7E7E` とG1
`E0A1`～`FEFE` の合計3,948枠を `U+E000`～`U+EF6B`（連続範囲間に隙間あり）へ
規則変換します。通常プロファイルはIVSを抑制し、HanyoDenshiは上流の `hd` を出力します。
この対応は固定jef4jデータ相当のベータであり、上流G1/G2は部分対応です。特にJIPS(E)外字は
上流規則との一致のみ確認対象で、実機未検証です。NEC内部コード、AdobeJapan1、JIPS
Roundtrip、任意外字表、COBOLレコード処理は対象外です。

Unisys名は正式名だけを受け付けます。Mixed LETS-JのSBCSは `00`～`7F` の
JISASCII（`27` → `U+2019`、`60` → `U+2018`、`5C` → `U+005C`、
`7E` → `U+007E`）と `A1`～`DF` のJIS X 0201半角カナです。DBCSは
JIS X 0208-1990（両バイトに`80`を加算）とJIS X 0212（第1バイトだけに
`80`を加算）で、全角空白はcanonicalな `2020` です。`A1A1` は別名として
受理しません。`21`～`7E`, `A1`～`FE` の利用者定義領域はfallbackに渡します。
`93` と後続バイトは偶数ならDBCS、奇数ならSBCSへのシフトです。エンコーダーは
`9370` / `93F1` のみを生成し、末尾にSBCS復帰を追加しません。Kanji形式は常時
DBCSでシフトを解釈しません。本対応は公開Unisys MLS仕様、Unicode JIS mapping、
公開実装との比較に基づき、実ClearPath検証は未完了です。特に `27` / `60` と
`F4A5` ⇄ `U+51DC` / `F4A6` ⇄ `U+7199` は実製品未確認です。

混在形式は `+` の左側から開始します。デコードは K (`28`)、K1 (`38`)、
K2 (`30 E2`)、A (`29`) を受け入れ、エンコードは K/A を使用します。
エンコード完了時には初期状態へ戻ります。EBCDICで表現可能な文字を優先します。
混在時の `28` / `29` / `30` / `38` はシフト構文として予約され、対応する
EBCDIC制御文字を通常文字としてエンコードする場合は fallback になります。

## マッピングと fallback

Fujitsu/Hitachi/NECマッピングは jef4j 0.14.2、IBMマッピングは固定したICU UCMに由来します。出典とライセンスは
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
dotnet run --project src/Jef4Net.CodeGen -c Release -- \
  data/jef4j src/Jef4Net/Hitachi/Internal/Generated/HitachiTables.g.cs hitachi
dotnet run --project src/Jef4Net.CodeGen -c Release -- \
  data/jef4j src/Jef4Net/Nec/Internal/Generated/NecTables.g.cs nec
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

AdobeJapan1、KEIS拡張文字セット3、KEIS2004全体、KEIS Roundtrip、任意IVD、
利用者独自の外字テーブル、
富士通公式の「領域重視」変換、COPY句、PIC / COMP / COMP-3、
COBOLレコード構造、CSVやDBへの移行処理は対象外です。
上流JSONと既知ベクトルに対する互換性を試験しています。富士通の公式実装ではありません。
日立KEISについても固定した上流JSON相当のベータ対応であり、実機では未検証です。
PUA割り当ては外字の字形復元を保証しません。

ライブラリ: Apache-2.0。マッピングデータ: 上流のCC0宣言に基づき利用。
