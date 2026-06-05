# Builder プロバイダー別プロジェクト分割の検討

対象: `Smart.Data.Accessor.Builders`（runtime 属性）/ `Smart.Data.Accessor.Builders.Generator`（generator）を、**プロバイダー（ANSI / SqlServer / MySql / Postgres）毎のプロジェクト・NuGet に分割**する方法の検討。

**本書は方針案。コードは未変更。** ユーザー指定の命名・配置方針（§4）を反映済み。

---

## 1. 背景・目的

- 現状、runtime 属性も generator も **全 4 プロバイダー同梱の 2 プロジェクト**。consumer が `Smart.Data.Accessor.Builders` を参照すると 4 プロバイダー全ての属性＋generator が入る（未使用 provider は無害だが冗長）。
- Phase 7 で当初プロバイダー別に分割していたが、**ユーザー判断で簡素化のため 2 プロジェクトに統合**した（技術ブロッカーではなく方針判断）。
- その後の **Builder generator 作り替え**（`BUILDER_GENERATOR_RESTRUCTURE.md`）で各 provider が自前の `Kind`/`Model`/`Transform`/`Execute` を持つ構造になり、**分割の前提が整った**。
- ゴール: consumer が必要な provider だけを参照できるようにする（依存・配布の分離）。

---

## 2. 現状構成（要点）

- **runtime `Smart.Data.Accessor.Builders`**（packable・net8/9/10）: `Attributes/` に ANSI 属性（`Smart.Data.Accessor.Attributes` 名前空間・flat）＋ `SqlServer/`・`MySql/`・`Postgres/` サブフォルダ（名前空間 `Smart.Data.Accessor.Attributes.{Provider}`）。`PackBuildOutputs` で generator DLL ＋ `SourceGenerateHelper.dll` を `analyzers/dotnet/cs` に同梱。
- **generator `Smart.Data.Accessor.Builders.Generator`**（netstandard2.0・`IsPackable=false`）: 共有メカニクス（`Engine/`＋`Models/`）＋ ANSI（root）＋ `Providers/{Provider}*`（各 Generator/Dialect/Models）。`GeneratorShared` をリンク。AnalyzerReleases 1 セット（SDA1001-1006）。`GlobalSuppressions.cs` で provider namespace の IDE0130 を抑制。
- **共有ソースの既存パターン**: `Smart.Data.Accessor.GeneratorShared` は **csproj 無しフォルダ**を `<Compile Include>` でリンク（DLL 化しない＝analyzer の DLL 依存を回避）。

> 注: ANSI 属性（`[Insert]` 等）は既に `Smart.Data.Accessor.Attributes`（flat）。**provider 属性だけ `.{Provider}` サブ名前空間**になっている。

---

## 3. 分割の論点

| # | 論点 | 内容 |
| --- | --- | --- |
| L1 | **共有メカニクスの配布** | `Engine/`＋`Models/`（base）は全 provider generator が必要。**リンクソース**（推奨）か共有 DLL か。 |
| L2 | **packaging** | provider 毎に「自前 generator DLL ＋ `SourceGenerateHelper.dll`」を `analyzers/dotnet/cs` へ pack する `PackBuildOutputs` を複製。 |
| L3 | **属性の名前空間 flat 化** | 【ユーザー指定】provider 属性も **`Smart.Data.Accessor.Attributes`（flat・サブフォルダ無し）** へ。`Sql*`/`MySql*`/`Pg*` 接頭辞で非衝突。各 runtime プロジェクトは `<RootNamespace>Smart.Data.Accessor</RootNamespace>`。 |
| L4 | **FAWMN metadata 名の変更** | L3 により provider 属性の FQN が `…Attributes.{Provider}.{Provider}Insert` → **`…Attributes.{Provider}Insert`**（例 `Smart.Data.Accessor.Attributes.SqlInsertAttribute`）。generator の `Targets` の `Ns` を更新（**一度きりの確定変更**。生成出力の SQL 自体は不変）。 |
| L5 | **診断（AnalyzerReleases）** | `BuilderDiagnostics`（SDA1001-1006）を各 provider generator にリンク → **各アセンブリが同 6 ルールを宣言** → 各 generator に AnalyzerReleases（RS2008）。共有診断変更時は全 generator 分を同時更新（B3 方針と整合）。 |
| L6 | **IDE0130 抑制の解消** | 分割で各 provider generator のフォルダと namespace が一致 → **`GlobalSuppressions.cs` の IDE0130 抑制が不要**に（副次クリーンアップ）。 |
| L7 | **テスト** | 作り替え後は内部型を手組みするテストが無い（end-to-end のみ）。`GeneratorTestHelper` は generator を public クラスで `new` するだけ → テストは **各 generator プロジェクトを参照**するのみ。 |

---

## 4. 分割後の構造案（ユーザー指定方針を反映）

### 4.1 runtime（属性・packable NuGet）

各プロジェクトは **`<RootNamespace>Smart.Data.Accessor</RootNamespace>`**、属性は **`Smart.Data.Accessor.Attributes`（flat）** に配置（provider サブフォルダ無し）。

| パッケージ | 属性（`Smart.Data.Accessor.Attributes` 名前空間） | 同梱 generator | 依存 |
| --- | --- | --- | --- |
| `Smart.Data.Accessor.Builders` | `QueryBuilderAttribute`／`Insert`…`Truncate`(ANSI)／`Limit`/`Offset` | ANSI generator | `Smart.Data.Accessor` |
| `Smart.Data.Accessor.Builders.SqlServer` | `SqlInsert`…`SqlMerge` | SqlServer generator | `Smart.Data.Accessor.Builders` |
| `Smart.Data.Accessor.Builders.MySql` | `MySqlInsert`…`MySqlInsertIgnore` | MySql generator | `Smart.Data.Accessor.Builders` |
| `Smart.Data.Accessor.Builders.Postgres` | `PgInsert`…`PgUpsert` | Postgres generator | `Smart.Data.Accessor.Builders` |

- consumer は `using Smart.Data.Accessor.Attributes;` だけで参照中パッケージの全属性が見える（接頭辞で区別）。
- 依存連鎖: `…Builders.SqlServer` → `…Builders`（`QueryBuilderAttribute`・`[Limit]`/`[Offset]`）→ `Smart.Data.Accessor`（`BuilderContext` 等）。

### 4.2 generator（analyzer・`IsPackable=false`・各 runtime に同梱）

【ユーザー指定】プロジェクト名 **`Smart.Data.Accessor.Generator.{Provider}`**、名前空間も同じ。**直下に IIncrementalGenerator / ModelBuilder / SourceBuilder（＝core と同じ 3 層）**、Models は **`Smart.Data.Accessor.Generator.{Provider}.Models`**。

```
Smart.Data.Accessor.Generator.SqlServer/            (namespace Smart.Data.Accessor.Generator.SqlServer)
  ├─ QueryBuilderGenerator.cs   : IIncrementalGenerator（配線。Initialize → 共有 Scan に ModelBuilder、共有 Output に SourceBuilder を渡す）
  ├─ ModelBuilder.cs            : transform（MethodResolver で共通解決 → Kind 判定 → 自前 Model 構築＋種別診断）
  ├─ SourceBuilder.cs           : emit（SqlEmit で SQL 組立。EmitMerge 等の provider 固有も）
  ├─ Dialect.cs                 : SqlDialect 実装（bracket / OFFSET-FETCH）
  └─ Models/                    (namespace Smart.Data.Accessor.Generator.SqlServer.Models)
        └─ BuilderModels.cs     : Kind enum ＋ {Insert..Merge}Model（共有 base 派生）
```

同形で `Smart.Data.Accessor.Generator.MySql` / `.Postgres` / `.Ansi`（ANSI、§8 Q2）。

> 現状の単一クラス（`{Provider}QueryBuilderGenerator` に transform＋emit をインライン）を、**core 同様の 3 層（Generator/ModelBuilder/SourceBuilder）に分割**する（[[project_generator_3layer_split]] と整合）。クラス名は namespace が provider を表すため接頭辞無し（`QueryBuilderGenerator`/`ModelBuilder`/`SourceBuilder`）を提案（§8 Q3）。

### 4.3 共有ソース（フォルダ・csproj 無し・各 generator に `<Compile Include>`）

- `Smart.Data.Accessor.GeneratorShared`（既存・core とも共有）— 変更なし。
- **`Smart.Data.Accessor.Builders.GeneratorShared`（新規フォルダ）** — 現 `Engine/`＋`Models/`(base) を移設。`MethodResolver`/`ResolvedMethod`/`SqlEmit`/`BuilderClassScanner`/`BuilderOutput`/`SqlDialect`/`BuilderDiagnostics`/`BuilderMapping` ＋ `BuilderClassModel`/`BuilderMethodModel`/`BuilderColumn`/`BuilderValueParam` ＋ `Polyfill.cs`。名前空間は `Smart.Data.Accessor.Builders.GeneratorShared`（§8 Q4）。

> `Assembly.cs`（`[assembly: CLSCompliant(false)]`）と AnalyzerReleases は各 generator が自前保持。`GlobalSuppressions.cs` は L6 で廃止。

---

## 5. 共有コードの扱い（L1）

- **案 A: リンクソース（推奨）**。各 generator DLL が共有メカニクスをコンパイル同梱＝自己完結。analyzer 間 DLL 依存なし（既存 `GeneratorShared` と同パターン）。短所＝各 DLL に複製コンパイル（軽微）／`SourceGenerateHelper.dll` を各パッケージ同梱／AnalyzerReleases 4 セット。
- **案 B: 共有 analyzer DLL（非推奨）**。Roslyn の analyzer 依存 DLL ロードが不安定。既存方針（ソース共有）に反するため採らない。

---

## 6. トレードオフ

**利点**: 必要 provider だけ参照／provider 毎の独立バージョニング／作り替えの provider 独立を配布へ反映／IDE0130 抑制解消／`using Smart.Data.Accessor.Attributes;` 一本化。

**コスト**: プロジェクト数増（runtime 4＋generator 4＋共有ソース 2 フォルダ）／共有メカニクスの複製コンパイル＋`SourceGenerateHelper.dll` 各同梱／AnalyzerReleases 4 セット／複数 provider 利用時は複数参照。現状維持でも未使用 provider は無害なので、**「配布分離」対「構成簡素さ」の判断**。

---

## 7. 移行手順（挙動・生成 SQL 不変で進める）

1. **属性 flat 化**: provider 属性を `Smart.Data.Accessor.Attributes.{Provider}` → `Smart.Data.Accessor.Attributes` へ（サブフォルダ撤去）。generator `Targets` の `Ns` を更新（`…Attributes.{Provider}.{Provider}` → `…Attributes.{Provider}`）。テスト/サンプルの `using` 更新。clean build＋テスト green を確認（この段は単一プロジェクトのまま）。
2. **generator 3 層化＋共有ソース化**: 各 provider の transform/emit を `ModelBuilder`/`SourceBuilder` に分割。`Engine/`＋`Models/`(base) を `Smart.Data.Accessor.Builders.GeneratorShared/` フォルダへ移設し現プロジェクトにリンク。挙動不変を確認。
3. **generator 分割**: `Smart.Data.Accessor.Generator.{Provider}` を新規作成（共有ソース＋GeneratorShared をリンク、AnalyzerReleases 複製、`<RootNamespace>` 既定）。ANSI は `Smart.Data.Accessor.Generator.Ansi`（Q2）。`GlobalSuppressions.cs` 廃止。
4. **runtime 分割**: `Smart.Data.Accessor.Builders.{Provider}` を新規作成（`<RootNamespace>Smart.Data.Accessor</RootNamespace>`、`Builders` 依存、対応 generator を analyzer 同梱、`PackBuildOutputs` 複製）。
5. **配線更新**: slnx 登録、`GeneratorTestHelper`（generator プロジェクト参照）、サンプル/テストの参照、spec §8 の構成記述。
6. **検証**: 各段階で clean build 0/0 ＋ Generator.Tests 158 / Tests 67 green。生成出力不変、`BuilderIncrementalCacheTests` green。

---

## 8. 確認事項（着手前）

- **Q1. 分割可否**: 分割する（配布分離を取る）か、現状 2 パッケージ維持か。
- **Q2. ANSI の generator 名/配置**: `Smart.Data.Accessor.Generator.Ansi`（provider と同形）＋ core `Smart.Data.Accessor.Builders` に同梱、で良いか。
- **Q3. generator クラス名**: 各 provider namespace 内で接頭辞無し（`QueryBuilderGenerator`/`ModelBuilder`/`SourceBuilder`）にするか、`{Provider}…` を残すか（public FQN 契約）。
- **Q4. 共有メカニクス名前空間**: 新フォルダ `Smart.Data.Accessor.Builders.GeneratorShared`（名前空間同名）で良いか。
- **Q5. runtime パッケージ名**: `Smart.Data.Accessor.Builders.{Provider}` で良いか（generator 側は `Smart.Data.Accessor.Generator.{Provider}` と非対称になる点の可否）。
- **Q6. 範囲**: 今回は分割のみ（挙動・生成不変）で良いか。

---

## 9. 確定（ユーザー回答 2026-06-05）

- **Q1**: 分割を進める。
- **Q2**: 標準 Builder に **"Ansi" の名称は付けない**。標準は `Smart.Data.Accessor.Builders`（runtime）/ `Smart.Data.Accessor.Builders.Generator`（generator）のまま。内部 `Ansi*`（`AnsiKind`/`AnsiInsertModel`/`AnsiSqlDialect`）は中立名へ改名（`Kind`/`InsertModel`/`StandardSqlDialect` 等）。
- **Q3**: provider クラス名は **provider 接頭辞を残す**（`SqlServerQueryBuilderGenerator`/`SqlServerModelBuilder`/`SqlServerSourceBuilder`、Models も `SqlServerInsertModel` 等）。
- **Q4/Q5**: runtime = `Smart.Data.Accessor.Builders.{Provider}`、**generator = runtime ＋ `.Generator`** = `Smart.Data.Accessor.Builders.{Provider}.Generator`（§4.2 の `Smart.Data.Accessor.Generator.{Provider}` は**無効**）。namespace も同名、Models は `…Generator.Models`。

### 確定後の最終構造

**runtime**（packable・`<RootNamespace>Smart.Data.Accessor</RootNamespace>`・属性は `Smart.Data.Accessor.Attributes` flat）
- `Smart.Data.Accessor.Builders`（標準: `QueryBuilderAttribute`／`Insert`…`Truncate`／`Limit`/`Offset`）
- `Smart.Data.Accessor.Builders.SqlServer`（`Sql*`）／ `.MySql`（`MySql*`）／ `.Postgres`（`Pg*`）

**generator**（analyzer・3 層 = `*Generator`/`*ModelBuilder`/`*SourceBuilder` ＋ `*Dialect`・Models は `.Generator.Models`）
- `Smart.Data.Accessor.Builders.Generator`（標準: `QueryBuilderGenerator`/`ModelBuilder`/`SourceBuilder`/`StandardSqlDialect`、Models `Kind`/`InsertModel`…）
- `Smart.Data.Accessor.Builders.{Provider}.Generator`（`{Provider}QueryBuilderGenerator`/`{Provider}ModelBuilder`/`{Provider}SourceBuilder`/`{Provider}Dialect`、Models `{Provider}Kind`/`{Provider}InsertModel`…）

**共有リンクソース**: `Smart.Data.Accessor.GeneratorShared`（既存）＋ `Smart.Data.Accessor.Builders.GeneratorShared`（新規: `Engine/`＋`Models/`(base)、namespace `Smart.Data.Accessor.Builders.GeneratorShared`）

### 実装フェーズ
- **Phase A（現 2 プロジェクト内のリファクタ・挙動不変）**: A1 属性 flat 化＋`Targets` 更新／A2 標準 `Ansi*` 改名／A3 provider・標準を 3 層化（ModelBuilder/SourceBuilder 抽出）・Models を `.Generator.Models` へ／A4 `Engine/`＋`Models/`(base) を `Smart.Data.Accessor.Builders.GeneratorShared/` へ移しリンク。各段で 158/67 green。
- **Phase B（プロジェクト分割）**: B1 generator 4 分割（`…Builders.{Provider}.Generator`、共有リンク・AnalyzerReleases 複製・`GlobalSuppressions` 廃止）／B2 runtime 4 分割（`…Builders.{Provider}`、`RootNamespace`・packaging 複製）／B3 slnx・テスト・サンプル・spec 配線。各段で green。

---

## 10. 実施結果（2026-06-05 完了）

全フェーズ完了。**clean build 0/0、Generator.Tests 158、Tests 67 green**（生成 SQL は不変）。

### 最終プロジェクト構成
- runtime（packable、`<RootNamespace>Smart.Data.Accessor</RootNamespace>`、属性は `Smart.Data.Accessor.Attributes` flat）: `Smart.Data.Accessor.Builders`（標準＋ベース `QueryBuilderAttribute`＋`[Limit]`/`[Offset]`）／`Smart.Data.Accessor.Builders.SqlServer`（`Sql*`）／`.MySql`（`MySql*`）／`.Postgres`（`Pg*`）。provider runtime は `Builders` に依存し、自分の generator を analyzer 同梱（`PackBuildOutputs`）。
- generator（analyzer、`IsPackable=false`、3 層）: `Smart.Data.Accessor.Builders.Generator`（標準: `QueryBuilderGenerator`/`StandardModelBuilder`/`StandardSourceBuilder`/`StandardSqlDialect`、Models `Smart.Data.Accessor.Builders.Generator.Models`）／`Smart.Data.Accessor.Builders.{Provider}.Generator`（`{Provider}QueryBuilderGenerator`/`{Provider}ModelBuilder`/`{Provider}SourceBuilder`/`{Provider}Dialect`、Models `…Generator.Models`）。
- 共有リンクソース（csproj 無し）: `Smart.Data.Accessor.GeneratorShared`（既存・core とも共有）＋ **`Smart.Data.Accessor.Builders.GeneratorShared`**（`Engine/`＝`MethodResolver`/`SqlEmit`/`BuilderClassScanner`/`BuilderOutput`/`SqlDialect`/`BuilderDiagnostics`/`BuilderMapping`／`Models/`＝`BuilderClassModel`/`BuilderMethodModel`/`BuilderColumn`/`BuilderValueParam`／`Polyfill.cs`）。各 generator が `<Compile Include>` でリンク（案 A）。
- **副次クリーンアップ**: provider が自前プロジェクトに分かれフォルダ＝namespace 一致 → `GlobalSuppressions.cs`（IDE0130 抑制）廃止。
- AnalyzerReleases は各 generator が自前保持（`BuilderDiagnostics` SDA1001-1006 をリンク＝全 generator が宣言、4＋1 セット）。
- テスト: `GeneratorTestHelper` は generator namespace 不変のため `using`/`new` は不変。テスト csproj に generator 4＋runtime 4 の `ProjectReference` を追加（生成器はプレーン参照＝手動 driver、analyzer 非アタッチ）。サンプル/Benchmark/AOT は provider builder 未使用で無改変。
