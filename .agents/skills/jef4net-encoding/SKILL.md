---
name: jef4net-encoding
description: Jef4NetのEncoding API、エンコーダー・デコーダーの状態機械、fallback、チャンク境界、変換性能を修正・検証する。マッピング原本だけの更新には使用しない。
---

# Jef4Net 変換処理の保守

コマンドはリポジトリルートで実行する。
[Coreと状態](../../../src/Jef4Net/Fujitsu/Internal/)、
[試験](../../../tests/Jef4Net.Tests/)、[現在の仕様](../../../README.md) を参照する。

## 不具合の切り分け

対象のエンコーディング名、入力のUTF-16またはバイト列、fallback、flush、
入力分割位置、出力容量を特定する。一括変換で再現しない場合も、
`Convert` の消費数・出力数・completedと次回の出力を記録する。
配列・文字列・Spanのどの入口で起きるか確認し、変換規則の修正はCoreへ置く。

## 維持する契約

- 初期状態は名前の `+` の左側。K/K1/K2/Aをデコードし、K/Aでエンコードする。
  エンコードのflushでは初期状態へ戻る。混在形式の予約バイトとEBCDIC優先規則はREADMEに従う。
- JEFリード、K2の `30 | E2`、UTF-16サロゲート、`sp` を持つ文字の最長一致を
  チャンク境界で失わない。入力不足と出力不足を区別する。
- 小さい出力バッファでは入力消費が出力に先行する場合がある。
  消費した情報を保留し、再呼出し時に欠落や重複がないことを確認する。
  `flush: false` では不完全入力を保留したままcompletedになり得る。
- countは状態のコピーで計算する。出力不足のGetBytes/GetCharsで状態を壊さない。
  Resetは保留入力・保留出力・fallback・シフト状態を初期化する。
- .NETのfallback契約を使う。標準EncoderReplacementFallbackの未対応サロゲートペアは
  置換文字列を2回返す。置換文字自体が変換不能な場合も有限に終了させる。
  例外Indexは呼出しの入力基準で、前チャンクの入力では負になり得る。
- 通常のSpan変換経路で文字ごとのヒープ割り当てを増やさない。
  テーブルやEncodingインスタンスにEncoder/Decoderの可変状態を共有させない。

## 変更に合った検証

まず再現例を既存の `EncodingTests`、`FallbackAndStateTests`、`BoundaryTests` の
適切な場所に追加する。変更した境界について、一括結果と全分割位置、1byte/1char入力、
出力容量0/1/必要量の直前、flushの再呼出しを比較する。
既存のEncode/Decode試験ヘルパーで結果と進行を検証し、消費数・完了フラグが
不具合の原因なら個別のConvert呼出しも直接検証する。

```sh
dotnet test tests/Jef4Net.Tests -c Release --filter 'FullyQualifiedName~FallbackAndStateTests|FullyQualifiedName~BoundaryTests|FullyQualifiedName~StreamsAndAllSplits'
```

修正後はAGENTS.mdの両ターゲットで全試験を実行する。
性能の変更では既存のallocation試験を使い、必要ならREADMEのPackageSmoke簡易計測も行う。
APIの挙動を変更した場合はREADMEとXML documentationを合わせ、変更前後の具体例を報告する。
