# Roundtrip・IVS基盤・HanyoDenshi 対応計画

作成日: 2026-09-20。v0.2.0の実装計画・作業記録。
Roundtrip、IVS基盤、HanyoDenshiは実装済み。現在の利用仕様は
[README](../README.md)、実装時の検証結果は[profile-validation](profile-validation.md)を参照。
以下は実装前の設計と完了条件を保存したもの。

## 1. 目的と対象

通常JEFの既存動作を維持しながら、可逆な文字対応を選択するRoundtripと、
異体字をUnicode列として扱うHanyoDenshiを追加する。
ライブラリはnetstandard2.1 / net10.0、開発・試験は.NET 10 SDKを維持する。

追加する名前は次の8形式とする。

- `x-Fujitsu-JEF-Roundtrip`
- `x-Fujitsu-JEF-HanyoDenshi`
- `x-Fujitsu-EBCDIC-{Lower|Kana|Ascii}+JEF-HanyoDenshi`（3形式）
- `x-Fujitsu-JEF-HanyoDenshi+EBCDIC-{Lower|Kana|Ascii}`（3形式）

既存と同じく大文字・小文字を無視し、`x-Fujitsu-` を省略した別名も提供する。
数値コードページは追加しない。通常JEFと既存6混在形式の名前・出力は変更しない。
公開のプロファイル登録APIは追加せず、まずEncodingProviderによる名前解決で提供する。

今回はAdobeJapan1、任意IVDコレクション、利用者独自外字テーブル、フォント描画、
RoundtripとHanyoDenshiの複合プロファイルを対象外とする。
Roundtripの混在形式も対象外とし、EBCDICの片方向対応やシフトの正規化に関する
保証を定義してから別途扱う。PUAの既存3,102文字は新プロファイルでも維持する。

## 2. 現状調査

仕様入力は引き続き `data/jef4j/provenance.json` に固定した
jef4j 0.14.2 / `40d13b0d92963c9b36993d1d742cdfc10c25b0f6` のJSONを使う。
今回の機能追加のために上流データを更新する必要はない。

- JSONには `hd` を持つ3,799レコードが既にある。これはレコード数であり、
  追加対応文字数や双方向変換可能数ではない。
- 現CodeGenは `hd` / `aj1` を除外している。プロファイル別の選択処理が必要。
- 現在のキーは最大2 Unicode scalarを保持し、デコーダーの出力保留はUTF-16最大4単位。
  固定JSONでは `sp` と `hd` を同時に持つレコードはなく、今回の列長に対応可能。
- Encoderは1 scalarのprefixとhigh surrogateを別々に保留する。
  `sp` 用に作られた最長一致をIVSにも適用し、fallback由来の文字と実入力の状態を検証する。
- Configurationにはプロファイルがなく、全変換経路が通常JEFを参照している。

## 3. Roundtripの仕様案

### 保証する範囲

例外fallbackで変換可能な、採用済みのJEF文字コードとUnicode列の対応について、
`Encode(Decode(code)) == code` と `Decode(Encode(sequence)) == sequence` を保証する。
replacement fallbackが発生した入力に可逆性は保証しない。
既定fallbackの種類は既存APIとの一貫性を保ち、移行向けの例では例外fallbackを指定する。

これは文字対応単位の保証である。文字列を連結した際の最長一致による区切りの変化や、
任意の元ファイルのバイト完全復元までを意味しない。連結による曖昧性も試験・記録し、
保証範囲をREADMEに明記する。

### 選別方法

1. 通常プロファイルの対象レコードから `oneway` / `unmappable` を除外する。
2. `decode_only` / `encode_only` と既存の候補優先順位を適用し、候補の両方向表を作る。
3. 両方向が一致する `(JEF code, Unicode sequence)` の組だけを最終表に残す。
   不一致を別の候補へ推測で付け替えない。
4. PUA規則変換を含め、生成後の全組で往復不変条件を検証する。

単純に `oneway` だけを除いた候補表を今回調査したところ、PUAを除く
10,890デコード候補・10,886エンコード候補に対して、コード起点22件・Unicode列起点18件の
往復不一致があった。これはJSONからの静的分析であり、Java実装の実行結果ではない。
例として、通常JEFで許容する `A1A1 → U+3000 → 4040` はRoundtripでは除外対象になる。

上流のRoundtripと採用範囲が異なる可能性があるため、Phase 1で差分を列挙する。
この計画では「検証済みの可逆な対応」を優先し、上流完全互換とは称さない。
仕様上の差分がこの方針に収まらない場合のみ、その具体例と代替案を提示して判断する。

## 4. HanyoDenshiとIVSの仕様案

### プロファイル選択

HanyoDenshiでは `aj1` を除外し、通常レコードと `hd` を持つレコードを対象にする。
デコード列はUnicode本体に `sp` または `hd` を付加する。
方向属性を適用し、同じJEFコードに複数候補がある場合は固定JSONの順序と上流の
HanyoDenshi試験を根拠に選択規則を確定する。単に `hd` を常に優先しない。

エンコードは完全なIVS列と、上流が許容するセレクタなしの対応を分けて生成する。
`variant_only` のレコードからセレクタなしの別名を生成しない。
`oneway` や `encode_only` があるため、HanyoDenshi全件の可逆性は保証しない。
Unicode正規化や互換漢字の自動統合は行わない。

固定JSONと上流試験から確認すべき代表例:

| 入力 | 期待する扱い |
|---|---|
| JEF `41A5` | HanyoDenshiでは `U+4E08 U+E0103`。通常JEFの出力は維持 |
| `U+4E08 U+E0103` | HanyoDenshiで `41A5` へ変換 |
| JEF `42BB` | `hd` レコードがencode_onlyである点を反映。IVSを無条件に出力しない |
| `U+585A` と `U+585A U+E0105` | variant_onlyによるセレクタなし候補への混入を防止。単体の `U+585A` は上流試験の `47C9` を確認 |
| JEF `4040`、PUA境界 | 新プロファイルでも既存の空白・PUA変換を維持 |

### IVSを扱う共通処理

- プロファイルごとに単体・2 scalar列・prefixの検索を切り替える。
- 同じ基底文字から始まる候補は最長一致し、非flush時は後続scalarの可能性を保留する。
- 未知のセレクタは黙って削除しない。既存の最長一致と同じく、基底文字の
  単体対応を処理してから未対応セレクタを標準fallbackへ渡す。
  一度出力された基底文字まで巻き戻すトランザクション動作は提供しない。
- 孤立セレクタ、不正なサロゲート、途中で終わるセレクタも標準fallbackで処理する。
  未対応セレクタのsurrogate pairには標準EncoderReplacementFallbackの契約を適用する。
- IVSを構成する基底文字とセレクタが両方補助平面の場合、UTF-16全4単位の
  各境界で分割しても動作するようにする。
- 汎用IVS基盤という名前でも、全IVSを認識したり全IVDコレクションを同梱したりはしない。
  このリリースでは固定JSONのHanyoDenshi対応だけを使用する。

## 5. 内部設計と変更箇所

| 箇所 | 変更 |
|---|---|
| CodeGen | 共通のJSON検証から、Normal / Roundtrip / HanyoDenshiの選択処理を分離。候補の採用・除外理由を検証可能にする |
| 生成テーブル | プロファイル別のID表・Unicode列エントリ・逆引き・prefixを生成。EBCDIC表は共用 |
| Configuration | internalなJEFプロファイル識別子を追加。初期シフト・EBCDIC種別とは独立に保持 |
| Mapping | Encode / Decode / IsPrefixに同じプロファイルを適用。PUAは共通の規則変換 |
| EncoderCore | プロファイル別の最長一致とUTF-16保留、fallback・実入力の境界を扱う |
| DecoderCore | 最大4 UTF-16単位を容量1から排出し、既存シフト処理と共存させる |
| Encoding / Provider | 新しい名前の解決、GetMaxByteCount/GetMaxCharCountの上限、Cloneとfallback指定を検証 |
| README / CI / PackageSmoke | 新しい対応名、保証範囲、利用例、パッケージ試験を追加 |

今回の上限は2 scalar / 4 UTF-16単位とし、CodeGenで上限超過を明示的に拒否する。
将来の3 scalar以上の列を無言で切り捨てない。公開APIを変えず内部表現を交換できる構造を保つ。
通常経路で文字ごとのallocationを発生させず、ランタイムで巨大な辞書やJSONを構築しない。

## 6. 実装順序と完了条件

### Phase 1: 選択規則と独立した期待値の確定

- 固定JSONと同じコミットの上流テストから、Roundtripの不一致例とHanyoDenshiの
  方向属性・衝突・variant_only・裸の基底文字の選択を調べる。
- 採用・除外の根拠を小さな既知ベクトルと、全件検証用の独立した期待値に残す。
- Roundtripの上流との差分と、文字列連結時の保証限界を記録する。

完了条件: 全属性の扱いと候補競合の解決が説明でき、未解決の競合を推測で採用していない。
生成器そのものを期待値の計算器として再利用しない。

### Phase 2: プロファイル別CodeGenとRoundtrip

- 共通のプロファイル識別とテーブル選択を導入し、既存Normalの回帰試験を先に通す。
- Roundtrip表と単体Encoding名を追加する。
- 除外コード・非対応Unicode列はreplacement / exception fallbackへ渡す。

完了条件: 全65,536コード、全採用Unicode列、PUA全件で採否と往復性を検証。
通常JEFでは許容するがRoundtripでは拒否する例を固定。生成物の再生成がバイト一致。

### Phase 3: IVS共通処理と単体HanyoDenshi

- HanyoDenshiの生成表、逆引き、prefixを追加する。
- 基底文字＋セレクタの最長一致と各surrogateの保留を実装する。
- 未対応IVS、孤立セレクタ、flushで途切れた列、再帰fallbackを検証する。

完了条件: 対象JSON全件と既知ベクトルが一致し、2～4 UTF-16単位の出力・入力を
全分割位置と出力容量0/1/2/3/4で処理できる。既存sp対応を壊していない。

### Phase 4: HanyoDenshi混在6形式とEncoding統合

- EBCDICの3種×初期状態2種の名前解決と変換を追加する。
- IVSの前後でのK/K1/K2/A、flush復帰、fallback後のシフト状態を確認する。
- 配列・文字列・Span、StreamReader/StreamWriter、Clone、Reset、count非破壊性を検証する。

完了条件: 入力消費数、出力数、completed、次回呼出しの結果が正しく、
容量不足・空入力flush・Reset・共有Encodingからの独立インスタンスで欠落や混線がない。

### Phase 5: 総合検証・文書・パッケージ

- 両ターゲットの全試験、通常変換のallocation試験、生成再現性を確認する。
- 新しいプロファイルのREADME例と保証の説明を追加する。
- PackageSmokeにRoundtripの拒否例とHanyoDenshiのIVS往復例を追加する。
- v0.2として出す場合はライブラリとPackageSmokeのバージョンを同期する。

完了条件: Release build / test / packが成功し、作成したパッケージだけを参照する
別プロジェクトで新APIが動く。パッケージにJSONやJava資産を含めない。
リモートCIの結果は実際に実行した場合だけ報告する。

## 7. 必須試験の補足

- 新旧プロファイルで同じ入力に異なる結果が出ることを名前別に検証する。
- HanyoDenshiのセレクタなし、既知IVS、未知IVS、連続セレクタ、孤立セレクタ。
- 補助平面基底文字と補助平面セレクタを組み合わせた全UTF-16境界。
- IVS、sp列、通常文字、PUAを連結したときの最長一致と分割不変性。
- replacementがIVSを含む場合、空replacement、長いreplacement、変換不能replacement。
- countを複数回呼んだ後のwrite結果、最大出力長の上限、出力不足後の再呼出し。
- 認識できないプロファイル名や今回は未対応の組合せは引き続きnull。
- 通常JEFの全マッピングと既存6混在形式を回帰試験する。

主要コマンドは [AGENTS.md](../AGENTS.md) の両ターゲット試験と
[README.md](../README.md) のCodeGen・PackageSmoke手順を使用する。

## 8. 実装単位と残る判断

変更は「仕様・期待値」「共通テーブル基盤＋Roundtrip」「IVS＋単体HanyoDenshi」
「混在統合＋パッケージ」の順にレビュー可能な単位へ分ける。

実装前の追加資料は原則不要。既存の固定JSONと上流テストで着手できる。
工数上の主な不確実性は、Roundtrip候補の不一致の整理と、IVSの先読み中にfallbackや
チャンク境界が重なる場合の状態管理にある。Phase 1終了時に件数と影響を報告する。

実装後は恒久的な仕様をREADME、検証結果をdocsの記録へ移し、
本計画書は実装済みの案内へ更新するか、作業完了時に削除する。
