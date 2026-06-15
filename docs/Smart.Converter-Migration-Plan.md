# Smart.Converter シグネチャ変更への対応

## 1. 事象

`Usa.Smart.Converter` の `IObjectConverter` のシグネチャ変更により、`Smart.Data.Accessor` 本体のビルドが失敗した。

```
ExecuteEngine.cs(127): error CS8619: 'Func<object?, object?>' を 'Func<object, object>' に代入できない
ExecuteEngine.cs(170): error CS8603: Null 参照戻り値である可能性があります。
```

`Directory.Build.props` の `<WarningsAsErrors>nullable</WarningsAsErrors>` により、Null 許容性警告（CS8619 / CS8603）が**エラー化**してビルドが停止する。

## 2. 原因（Smart.Converter 側の変更）

`IObjectConverter` が以下のように変更された。

| メソッド | 旧 | 新（2.12.0） |
| --- | --- | --- |
| `Convert<T>` | `T Convert<T>(object value)` | `T? Convert<T>(object? value)` |
| `CreateConverter` | `Func<object, object>? CreateConverter(...)` | `Func<object?, object?>? CreateConverter(...)` |
| 全メソッド | 属性なし | `[RequiresDynamicCode]` / `[RequiresUnreferencedCode]` |

戻り値の Null 許容性が緩和された（`T?` / `Func<object?, object?>?`）＝ **「変換結果は null になり得る」ことが型で明示された**。

## 3. 深掘り調査：変換器は実際に null を返す

単に注釈を合わせる（`!` や `Unsafe.As` で握りつぶす）だけでは不十分と判断し、変換器が実際に null を返すか、また返った null を使う側が安全に扱えるかを確認した。

### 3.1 変換器は値型に対しても null を返す

`Smart.Converter` の文字列パース系変換器は、パース失敗時に null を返す。

```csharp
// GuidConverterFactory — 正しく使い分けている
targetType == typeof(Guid)   => ... ? result : default   // 非Nullable値型には default(Guid)
targetType == typeof(Guid?)  => ... ? result : null      // Nullable にのみ null

// BooleanConverterFactory — bool と bool? を同一分岐で処理し、使い分けていない
else if ((targetType == typeof(bool)) || (targetType == typeof(bool?)))
{
    if (sourceType == typeof(string))
        return static x => Boolean.TryParse((string)x, out var result) ? result : null;  // 非Nullable bool でも null
}
```

→ `string` → `bool`（非 Nullable）のパース失敗で、変換器は **null を返す**。

### 3.2 使う側は null を安全に扱えていなかった

| 経路 | null 受領時の挙動 | 結果 |
| --- | --- | --- |
| Emit/リフレクションマッパー（`ParserMapper.Map` / `ObjectResultMapperFactory` / `TupleResultMapperFactory`）| 値型に `(T)null` / `Unbox_Any` | **NullReferenceException** |
| 生成コード（`Convert<T>(source) ?? default!`）| `?? default!` で `default(T)` | 安全 |
| 参照型全般 | `Castclass` / `Unsafe.As` で null 通過 | 安全 |

→ 旧来の「変換器は非 null を返す」という暗黙の前提が、Smart.Converter の `object?` 化で崩れ、**Emit/リフレクションマッパー経路に潜在的な値型 NRE** が存在することが判明した。

## 4. 対応方針

> `ExecuteEngine` / `IResultMapperCreateContext` の変換器型を `Func<object, object?>` にして**「null を返し得る」ことを型で正直に表現**し、**コンシューム側（マッパー）で変換結果 null を `default` にフォールバック**する。

型を変えるだけでは値型 NRE は消えない（受け側で `default` を作れるのは型を知っているマッパー側だけ）。型変更とコンシューム側の null ハンドリングを必ずセットで行う。

### 波及を最小化できる理由（共変 / 反変）

- `CreateConverter`（`Func<object?, object?>?`）→ `GetConverter`（`Func<object, object?>?`）: 引数の**反変**で暗黙変換可。`Unsafe.As` 不要、そのまま `return` できる。
- `CreateHandler`（`Func<object, object>?`）→ `GetConverter`（`Func<object, object?>?`）: 戻り値の**共変**で暗黙変換可。

このため、`ITypeHandler.CreateParse` / `ResultParserAttribute.CreateParser`（公開抽象・実装多数）や handler 経路（`Convert<T>` の handler 引数 / `RuntimeHelper.CreateHandler` / 生成コードの `HandlerType`）は**一切変更不要**。公開 API の破壊的変更は `IResultMapperCreateContext.GetConverter` の戻り値型変更のみに限定される。

## 5. 実装

### 型を `Func<object, object?>` 化

| ファイル | 変更 |
| --- | --- |
| `IResultMapperCreateContext.cs` | `GetConverter` 戻り値 → `Func<object, object?>?`（**公開 IF・破壊的変更**） |
| `ExecuteEngine.cs` | `GetConverter` 実装を `Func<object, object?>?` 化。`return objectConverter.CreateConverter(...)`（反変で直接代入、`Unsafe.As` 撤去） |
| `TypeMapInfoHelper.cs` | `BuildConverterMap` の `map` → `Dictionary<int, Func<object, object?>>` |
| `SingleResultMapperFactory.cs` | `ParserMapper.parser` → `Func<object, object?>` |
| `ObjectResultMapperFactory.cs` | `converters` → `Func<object, object?>` |
| `TupleResultMapperFactory.cs` | `converters` → `Func<object, object?>` |

### コンシューム側で変換結果 null → default

- **`SingleResultMapperFactory.ParserMapper.Map`（手書き）**

```csharp
var value = record.GetValue(0);
if (value is DBNull)
{
    return default!;
}

var converted = parser(value);
return converted is null ? default! : UnsafeCastHelper.UnsafeCast<T>(converted);
```

- **`EmitExtensions.EmitValueConvertByField`（Emit 共通ヘルパー、Object/Tuple が使用）** に、変換器 Invoke 後の null チェック分岐を追加。null なら `EmitStackDefaultValue` で既定値を積み、型変換（`Unbox_Any` 等）をスキップして `next` へ。

```csharp
ilGenerator.Emit(OpCodes.Call, ConvertFunc);         // [Converted]
var hasValue = ilGenerator.DefineLabel();
ilGenerator.Emit(OpCodes.Dup);
ilGenerator.Emit(OpCodes.Brtrue_S, hasValue);
ilGenerator.Emit(OpCodes.Pop);
ilGenerator.EmitStackDefaultValue(type, valueTypeLocals);  // [Default]
ilGenerator.Emit(OpCodes.Br, next);                  // skip Unbox/Cast
ilGenerator.MarkLabel(hasValue);                     // [Converted]
```

これにより、DBNull（NULL 値）と変換失敗（不正値）がいずれも `default` になり、生成コード経路（`Convert<T>(source) ?? default!`）と挙動が統一される。

### 変更しなかったもの（共変/反変で吸収）

- `ITypeHandler.CreateParse` / `ResultParserAttribute.CreateParser`（公開）と各実装、テスト、README
- handler 経路: `ExecuteEngine.Convert<T>` の handler 引数 / `ExecuteEngine.CreateHandler` / `RuntimeHelper.CreateHandler` / `SourceBuilder.HandlerType`（生成コード）
- `ExecuteEngine.Convert<T>` の `objectConverter.Convert<T>(source) ?? default!`（値変換のフォールバックとして妥当なため維持）

## 6. 検証

- ソリューション全体・全 TFM（net8.0 / net9.0 / net10.0）を `dotnet build -c Release` で **0 警告 0 エラー**。
- テスト **283 / 283 成功**。変換失敗（`string`→`bool` の `"Invalid"`）で NRE せず `default(false)` になることを以下の新規テストで実証。
  - `SingleResultMapperFactoryTest.TestMapSingleConvertFailureReturnsDefault`（手書き経路）
  - `ObjectResultMapperFactoryTest.TestMapPropertyConvertFailureReturnsDefault`（Emit プロパティ経路）
  - `ObjectResultMapperFactoryTest.TestMapConstructorConvertFailureReturnsDefault`（Emit コンストラクタ引数経路）
  - Tuple マッパーも同一の `EmitValueConvertByField` を同じ文脈（プロパティ/コンストラクタ）で使用するため上記で代表。

## 7. 補足（AOT / トリミング属性）

Smart.Converter が全 API に付与した `[RequiresDynamicCode]` / `[RequiresUnreferencedCode]` は、現状のビルド（トリミング解析未有効）では顕在化しない。将来 `IsTrimmable` / `EnableTrimAnalyzer` を有効化する際は、既存方針（`ExecuteEngineConfig.cs` / `ObjectResultMapperFactory.cs` の `RequiresDynamicCode` / `UnconditionalSuppressMessage`）に倣って対応する。今回の対応スコープ外。
