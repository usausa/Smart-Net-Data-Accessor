# リファクタ候補一覧（型名 const 化 / 非ドメイン static メソッド分離）

対象は Source Generator 群（`Smart.Data.Accessor.Generator` / `Smart.Data.Accessor.Builders.Generator` / 共有 `Smart.Data.Accessor.GeneratorShared`）。**本書は候補のまとめで、コードはまだ変更していません。** 着手範囲の合意後に実装します。

## 凡例・前提

- **判定リテラル**＝`symbol.ToDisplayString() == "..."` / `fq is "..."` / `case "...":` 等、**型名で分岐するための文字列**。→ Part A の対象。
- **emit リテラル**＝`builder.Append("global::System...")` や `?? "global::System.Data.DbType.Object"` 等、**生成コードとして出力する文字列**。→ Part A の**対象外**（コード生成テンプレートであり「クラス名判定」ではない）。
- 既存の受け皿:
  - `Smart.Data.Accessor.Generator/Helpers/AccessorSymbolExtensions.cs` … **現状空**（"v1 skeleton" コメント）。core 専用 Roslyn 拡張用。
  - `Smart.Data.Accessor.Builders.Generator/Helpers/BuilderSymbolExtensions.cs` … **現状空**。Builder 専用。
  - `Smart.Data.Accessor.GeneratorShared/*` … **linked source**（両ジェネレータにコンパイル時取り込み）。`TypeAnalysisHelper`/`ConverterScopeHelper`/`MappingAttributeHelper`/`ColumnAttributeHelper`/`CodeExpressionHelper` が既存。**core/Builder 共通の汎用処理はここが最適。**

---

## Part A — 型名判定リテラルの const 化

### 既存 const（参考・対応済み）

`AccessorModelBuilder.cs:19-49` に属性 FQN 群＋`CancellationTokenTypeName`（`System.Threading.CancellationToken`）/`EnumeratorCancellationAttributeName`/`QueryBuilderMethodSuffix` が const 済み。`BuilderModelBuilder.cs:15` にも `CancellationTokenTypeName` が**別途**定義（＝重複）。

### 未 const 化の判定リテラル（候補）

すべて `Smart.Data.Accessor.Generator/AccessorModelBuilder.cs`（特記なき限り）。

| # | カテゴリ | リテラル | 箇所 | 用途 | 提案 const 名 |
| --- | --- | --- | --- | --- | --- |
| A1 | Task/ValueTask | `System.Threading.Tasks.Task` | 1342 | 戻り値 shape 判定 | `TaskTypeName` |
| A2 | 〃 | `System.Threading.Tasks.ValueTask` | 1346 | 〃 | `ValueTaskTypeName` |
| A3 | 〃 | `System.Threading.Tasks.Task<TResult>` / `...Task<T>` | 1355 | 〃 | `TaskOfTTypeName`（2 表記） |
| A4 | 〃 | `System.Threading.Tasks.ValueTask<TResult>` / `...ValueTask<T>` | 1375 | 〃 | `ValueTaskOfTTypeName`（2 表記） |
| A5 | enumerable | `System.Collections.Generic.IAsyncEnumerable<T>` | 1395 | 〃 | `AsyncEnumerableTypeName` |
| A6 | 〃 | `System.Collections.Generic.IEnumerable<T>` | 1403 | 〃 | `EnumerableTypeName` |
| A7 | list shape | `...List<T>` / `IList<T>` / `IReadOnlyList<T>` / `IReadOnlyCollection<T>` / `ICollection<T>` | 1476-1480 | BufferList 判定 | 個別 const（5 個） |
| A8 | 廃止型 | `System.Memory<T>` / `System.ReadOnlyMemory<T>` / `System.Collections.Immutable.ImmutableArray<T>` / `System.Collections.Generic.HashSet<T>` | 1447-1450 | SDA0301 判定 | 個別 const（4 個） |
| A9 | 廃止型 | `System.Tuple<` / `System.ValueTuple<`（`StartsWith`） | 1456-1457 | SDA0301 判定 | `TupleTypeNamePrefix` / `ValueTupleTypeNamePrefix` |
| A10 | ADO 接続 | `System.Data.Common.DbConnection` | 1145 / **Builder 373** | conn 判定（**重複**） | **共有** `DbConnectionTypeName` |
| A11 | ADO Tx | `System.Data.Common.DbTransaction` | 1248 / **Builder 375** | tx 判定（**重複**） | **共有** `DbTransactionTypeName` |
| A12 | ADO reader | `System.Data.Common.DbDataReader` | 1223,1229 | reader 判定 | `DbDataReaderTypeName` |
| A13 | ADO reader | `System.Data.IDataReader` | 1223,1236 | reader 判定 | `DataReaderInterfaceTypeName` |
| A14 | scalar | `System.Guid` | 1688, 1758 | reader/DbType 判定 | `GuidTypeName` |
| A15 | scalar | `System.DateTimeOffset` / `System.TimeSpan` / `byte[]` | 1759-1761 | DbType 推論キー | 個別 const（3 個） |
| A16 | namespace | `System` / `System.`（`StartsWith`） | 1715 | POCO 判定（BCL 除外） | `SystemNamespace` / `SystemNamespacePrefix` |
| A17 | provider enum | `System.Data.DbType` / `System.Data.SqlDbType` / `MySql.Data.MySqlClient.MySqlDbType` / `MySqlConnector.MySqlDbType` / `NpgsqlTypes.NpgsqlDbType` / `Oracle.ManagedDataAccess.Client.OracleDbType` | 1270-1298 | whitelist `switch` キー | 個別 const（6 個・**判定キーのみ**。`global::...Parameter` 値は emit なので対象外） |

（任意・低優先）`m.Modifiers.Any(m => m.Text == "partial")`（55 付近）の `"partial"`、`providerPropertyName = "DbType"` 等は型名判定ではないため通常は据え置き推奨。

### 配置案（要決定）

- **案 A:（推奨）共有 const 集約** … `GeneratorShared/WellKnownTypeNames.cs`（`internal static class WellKnownTypeNames`）に BCL/フレームワーク型 FQN を集約。**A10/A11 と `CancellationTokenTypeName` の core/Builder 重複を解消**できる。core 専用（戻り値 shape 等）も「フレームワーク型名」として同居可。
- **案 B: ローカル const** … `AccessorModelBuilder` の既存 const 群に追記。最小変更だが Builder 側重複は残る。

> 提案: **A10/A11/CancellationToken など重複するものは案 A（共有）**、core 専用の戻り値 shape 系は案 A/B どちらでも可。プロバイダ whitelist（A17）はキーと値（emit）が対なので、`TryGetProviderDbTypeMapping` 内のテーブル化（`(enumKey, paramType, prop, route)` 配列）に併せて整理する手もある。

---

## Part B — 非ドメイン static メソッドの分離（検討）

### 分類

| 種別 | 説明 | 方針 |
| --- | --- | --- |
| (a) 汎用文字列操作 | ドメイン非依存の `string`/`char` 処理 | Helper へ分離 |
| (b) Roslyn 汎用 | `ISymbol`/`ITypeSymbol` の汎用照会（継承走査・record ctor 等） | 拡張メソッドへ分離 |
| (c) ドメイン処理 | 戻り値 shape 分類・DbType マッピング・属性意味論など本ジェネレータ固有 | **据え置き** |

### 分離候補

| # | メソッド | 箇所 | 種別 | 重複 | 提案先 |
| --- | --- | --- | --- | --- | --- |
| B1 | base 型走査（`for current = type; current != null; current = current.BaseType`） | core `IsDbConnectionType`:1141 ほか / **Builder `InheritsFrom`:379** | (b) | **core×3＋Builder** | `GeneratorShared` 新規 `SymbolExtensions.InheritsFrom(this ITypeSymbol, string baseFqn)` |
| B2 | `IsDbConnectionType` / `IsDbTransactionType` / `IsReaderType` | 1141 / 1244 / 1220 | (b) | **Builder に同等あり** | `SymbolExtensions.IsDbConnection()/IsDbTransaction()/IsReader()`（B1 を利用） |
| B3 | `IsCancellationToken` 相当（`ToDisplayString()==CancellationTokenTypeName`） | core inline:743 / **Builder `IsCancellationToken`:371** | (b) | **重複** | `SymbolExtensions.IsCancellationToken()` |
| B4 | `TryGetRecordPrimaryConstructor` | 1599 | (b) record 検出 | — | `SymbolExtensions.TryGetRecordPrimaryConstructor()`（または `AccessorSymbolExtensions`） |
| B5 | `ReferencesIdentifier`（whole-word 検索）＋ `IsIdentifierChar` | 1182 / 1203 | (a) | — | `GeneratorShared` 新規 `StringHelper.ContainsWholeWordIdentifier()` / `IsIdentifierChar()` |
| B6 | `ExtractRoot`（先頭 `.` で分割） | core:2151 / **`NodeEmitter.ExtractRoot`:273** | (a) | **重複×2** | `StringHelper.ExtractRoot()` |
| B7 | `Escape`（C# 文字列リテラルエスケープ） | `NodeEmitter`:279 | (a) | **`CodeExpressionHelper.StringLiteral` と機能重複** | `CodeExpressionHelper` に集約（または `StringHelper`） |
| B8（任意） | `IsLikelyResolvableInjectType` | 1205 | (b) DI 解決可否ヒューリスティック | — | borderline（DI 寄り）。`AccessorSymbolExtensions` 候補だが据え置きも可 |
| B9（任意） | `HasUserDeclaredFieldOrProperty` | 1164 | (b) メンバ照会 | — | borderline。据え置き推奨 |

### 据え置き（ドメイン処理・移動しない）

`ClassifyReturn` / `IsListLike` / `IsDisallowedReturnType` / `IsValidExecuteReturn` / `ClassifyColumnType`(`GetReaderMethod`) / `InferDbTypeExpr` / `TryGetProviderDbTypeMapping` / `IsPocoParameter` / `BuildColumnInfos` / `BuildPocoProperties` / `BuildSqlEmitCode` / `IsAsyncShape` / `IsReaderShape` / `IsQueryBuilderAttribute` / `HasDataMethodAttribute` / `ResolveBindMarker` / `BuildSqlMap` / `ToLegacyDirection` など。— いずれも本ジェネレータの**仕様（戻り値 shape・DbType・属性意味論）**を表すため分離しない。

### 効果（重複解消）

- `CancellationTokenTypeName` const … core/Builder の 2 重定義 → 共有 1 箇所（Part A 連動）。
- base 型走査ロジック（B1/B2/B3）… core と Builder で実質同一実装 → 共有拡張 1 箇所。
- `ExtractRoot`（B6）… `AccessorModelBuilder` と `NodeEmitter` の 2 重 → 1 箇所。
- `Escape`/`StringLiteral`（B7）… 2 実装 → 1 箇所。

---

## 推奨実装方針（提案・要承認）

1. **Part A**: `GeneratorShared/WellKnownTypeNames.cs` を新設し、重複型（A10/A11/CancellationToken/A12/A13）＋ core 専用の戻り値 shape 系（A1-A9, A14-A16）を集約。プロバイダ whitelist（A17）は `TryGetProviderDbTypeMapping` のテーブル化と併せて整理。
2. **Part B**: `GeneratorShared/SymbolExtensions.cs`（B1-B4）＋ `GeneratorShared/StringHelper.cs`（B5/B6）を新設、`Escape`→`CodeExpressionHelper`（B7）へ集約。空の `AccessorSymbolExtensions`/`BuilderSymbolExtensions` は core/Builder 専用が出れば活用、無ければ削除も検討。
3. 各段階で **clean build 0/0 ＋ Generator.Tests 130 / Tests 67 green** を確認。

## 着手前の確認事項

- **Q1. Part A の配置**: 共有 `WellKnownTypeNames`（案 A・重複解消）で良いか／ローカル const（案 B）が良いか。
- **Q2. Part B の範囲**: B1-B7（重複解消・効果大）まで実施で良いか。任意の B8/B9 は含めるか。
- **Q3. 拡張メソッド化の可否**: `IsDbConnectionType(p.Type)` → `p.Type.IsDbConnection()` のように**呼び出し側も拡張メソッドへ書き換え**て良いか（ドメイン的に問題ないか）。
- **Q4. 空 skeleton の扱い**: `AccessorSymbolExtensions`/`BuilderSymbolExtensions` は埋める／削除どちらにするか。
