# サードパーティ Generator 作成ガイド（プロバイダ別 QueryBuilder）

対象読者：Smart.Data.Accessor に **独自 DB プロバイダ向けの QueryBuilder ジェネレータ**（例：Oracle 版の
`[OraInsert]`/`[OraSelect]` …）を追加したい開発者。本体（コア）ジェネレータの改造は不要で、
**属性パッケージ＋ジェネレータの 2 プロジェクト**を新設するだけで拡張できる。

参照実装は同梱の 3 プロバイダ（`Smart.Data.Accessor.Builders.Postgres` / `.SqlServer` / `.MySql`）。
本ガイドのコード断片は Postgres 実装を雛形にしている。まず Postgres の実物（合計 10 ファイル程度）を
一読してから本ガイドに戻ると理解が速い。

---

## 1. 全体アーキテクチャ：コアとの役割分担

Smart.Data.Accessor のメソッドは 2 軸で決まる。

- **A 群（実行種別）** … `[Execute]` / `[ExecuteScalar]` / `[Query]` / `[QueryFirst]` / `[ExecuteReader]`。
  **生成マーカーであり必須**（B 群だけのメソッドは SDA0108 Error）。
- **B 群（コマンドソース）** … SQL をどこから得るか。`.sql` ファイル（既定）／`[Sql]`／`[DirectSql]`／
  `[Procedure]`／**QueryBuilder 属性（本ガイドの拡張点）**。

役割分担は次のとおり。**コアがメソッド本体（接続・コマンド・実行・マッピング）を全部生成**し、
プロバイダジェネレータは「SQL 文とパラメータ束縛を組み立てるヘルパー」だけを生成する。

```
ユーザーコード
  [DataAccessor] partial class FooAccessor
      [Query] [PgSelect(typeof(Entity))] partial IReadOnlyList<Entity> List(...);
                     │
      ┌──────────────┴───────────────────────────────┐
      │ コア Generator（Smart.Data.Accessor.Generator）│ ← 触らない
      │   List() 本体を生成し、SQL 構築を              │
      │   List__QueryBuilder(ref context, ...) に委譲  │
      └──────────────┬───────────────────────────────┘
                     │ 命名規約で結合（コンパイル時に整合が検証される）
      ┌──────────────┴───────────────────────────────┐
      │ プロバイダ Generator（あなたが作る）           │
      │   private static void List__QueryBuilder(     │
      │       ref BuilderContext context, <値引数>)    │
      │   を同じ partial class に生成                  │
      └──────────────────────────────────────────────┘
```

### 結合契約（これだけ守れば繋がる）

1. 属性は **`QueryBuilderAttribute` 派生**にする。コアは「`QueryBuilderAttribute` を継承した属性が付いた
   メソッド」を `SqlSource.QueryBuilder` と判定し、`{メソッド名}__QueryBuilder` の呼び出しを emit する。
2. プロバイダジェネレータは同じ partial class に
   `private static void {メソッド名}__QueryBuilder(ref BuilderContext context, <値引数>)` を生成する
   （シグネチャの開始と `var cmd = context.Command;` は共有 `SqlEmit` が出す）。
3. 排他・整合はコア側の診断が守る：A 群必須（SDA0108）、QueryBuilder×`[Procedure]`/`[DirectSql]`
   （SDA0105）、×`[Sql]`（SDA0107）、`.sql` ファイル併存（SDA0405）、QueryBuilder 属性の重複（SDA1002）。
   **プロバイダ側で再実装しない**こと。

---

## 2. 成果物：2 プロジェクト構成

| プロジェクト | TFM | 役割 |
| --- | --- | --- |
| `<Your>.Builders.Xxx` | `net8.0;net9.0;net10.0`（`IsAotCompatible`） | 利用者が参照する**属性パッケージ**。NuGet にジェネレータ dll を同梱する |
| `<Your>.Builders.Xxx.Generator` | `netstandard2.0` | **ジェネレータ本体**（Roslyn アナライザとして配布） |

### 2.1 ジェネレータ側 csproj（要点）

```xml
<PropertyGroup>
  <TargetFramework>netstandard2.0</TargetFramework>
  <IsRoslynComponent>true</IsRoslynComponent>
  <IsPackable>false</IsPackable>
  <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="5.6.0" PrivateAssets="all" />
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="5.6.0" PrivateAssets="all" />
  <PackageReference Include="SourceGenerateHelper" Version="1.16.0" GeneratePathProperty="true" PrivateAssets="all" />
</ItemGroup>

<ItemGroup>
  <!-- 共有ソース（DLL ではなくリンク）。Helpers=コアと共通の属性読み取り等、Builders=QueryBuilder 共通エンジン -->
  <Compile Include="..\Smart.Data.Accessor.Shared\**\*.cs" Link="Shared\%(RecursiveDir)%(Filename)%(Extension)" />
</ItemGroup>

```

ポイント：

- ジェネレータは **netstandard2.0 固定**（VS 内の .NET Framework ホストでも動くため）。
  C# 最新構文は使えるが、BCL は netstandard2.0 の範囲。
- 共有部品は **linked source**（`Smart.Data.Accessor.Shared/**`）。DLL 共有ではないので、
  ジェネレータ間のバージョン不整合が構造的に起きない。サードパーティがリポジトリ外で作る場合は
  このフォルダをコピーして同梱する（namespace は `Smart.Data.Accessor.Shared.*` のままでよい）。
- `SourceGenerateHelper`（SGH）は equatable モデルの基盤（`EquatableArray<T>` / `DiagnosticInfo` /
  `LocationInfo` / `SourceBuilder` 等）。`GeneratePathProperty=true` は後述のパッケージングとデバッグ用。

### 2.2 属性パッケージ側 csproj（要点）

```xml
<ItemGroup>
  <ProjectReference Include="..\Smart.Data.Accessor\Smart.Data.Accessor.csproj" />
  <ProjectReference Include="..\<Your>.Builders.Xxx.Generator\<Your>.Builders.Xxx.Generator.csproj"
                    PrivateAssets="all" ReferenceOutputAssembly="false" OutputItemType="Analyzer" />
</ItemGroup>

<!-- NuGet へジェネレータ dll と SGH dll を analyzers として同梱 -->
<PropertyGroup>
  <TargetsForTfmSpecificContentInPackage>$(TargetsForTfmSpecificContentInPackage);PackBuildOutputs</TargetsForTfmSpecificContentInPackage>
  <NoWarn>$(NoWarn);NU5118;NU5129</NoWarn>
</PropertyGroup>
<Target Name="PackBuildOutputs" DependsOnTargets="SatelliteDllsProjectOutputGroup;DebugSymbolsProjectOutputGroup">
  <ItemGroup>
    <TfmSpecificPackageFile Include="..\<Your>.Builders.Xxx.Generator\bin\$(Configuration)\netstandard2.0\<Your>.Builders.Xxx.Generator.dll"
                            Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
    <TfmSpecificPackageFile Include="$(PKGSourceGenerateHelper)\lib\netstandard2.0\SourceGenerateHelper.dll"
                            Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
  </ItemGroup>
</Target>
```

`OutputItemType="Analyzer"` により、パッケージを参照したプロジェクトでも・この属性プロジェクト自身の
ビルドでも、ジェネレータがアナライザとして走る。

---

## 3. 設計原則（Model 駆動）

`IIncrementalGenerator` は**入力値の値等価性でキャッシュ**する。これを壊さないための鉄則：

1. **パイプラインに Roslyn シンボルを流さない。** `ISymbol` / `SyntaxNode` / `Compilation` は
   transform（`ForAttributeWithMetadataName` のデリゲート）内で使い切り、**equatable な record Model**
   だけを `RegisterSourceOutput` に渡す。シンボルは Compilation 毎に別インスタンスになるため、
   混入するとキャッシュが全く効かず毎キー入力で全再生成になる。
2. **コレクションは `EquatableArray<T>`（SGH）。** `List<T>`/配列は参照等価なので不可。
   ⚠ `default(EquatableArray<T>)` は列挙で NRE → ジェネレータ全体が CS8785 で沈黙死する。
   必ず明示コンストラクタ（`new EquatableArray<T>(items.ToArray())`）か nullable で持つ。
3. **Model は各ジェネレータが internal に所有し、ジェネレータ間で共有しない。**
   共有してよいのは「メカニクス」（走査・解決・emit の部品）と、メンバ情報を返す
   `XxxInfo` 戻り値（`TypeMapInfo` / `ColumnAttributeInfo` 等）だけ。Model を共有すると
   プロバイダ間の仕様差（例：Postgres の `RETURNING`）が共有型を汚染していく（過去に一度
   過剰共通化で失敗し、意図的に「各プロバイダが Model/Transform/Emit を自前所有」へ作り替えた経緯がある）。
4. **emit（SourceBuilder）はシンボル非依存の純関数**にする。入力は Model のみ。
   こうすると Model を手組みした単体テストで emit を直接検証できる。
5. **診断は `DiagnosticInfo`（SGH）で Model に載せて運ぶ。** transform 内で
   `Diagnostic.Create` して即 report する API は無い（RegisterSourceOutput 側でまとめて報告される）。

---

## 4. 実装手順

### Step 1 — 属性を定義する（属性パッケージ側）

```csharp
namespace Smart.Data.Accessor.Attributes;   // flat namespace（全属性共通の規約）

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class OraInsertAttribute : QueryBuilderAttribute   // ← 派生が結合契約
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public OraInsertAttribute()
    {
    }

    public OraInsertAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
```

- namespace は `Smart.Data.Accessor.Attributes` に**フラット**、クラス名にプロバイダ接頭辞
  （`Pg*` / `Ora*` …）を付けるのが同梱プロバイダの規約。
- プロバイダ固有オプションは名前付きプロパティで（例：Postgres は `Returning`）。

### Step 2 — ジェネレータの配線（1 ファイル・約 25 行）

```csharp
[Generator]
public sealed class OracleQueryBuilderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ClassScanner.DataAccessorAttributeName,          // [DataAccessor] クラスを起点に走査
                static (node, _) => node is ClassDeclarationSyntax,
                static (context, cancellation) => OracleModelBuilder.Build(context, cancellation))
            .WithTrackingName(ClassScanner.TrackingName);

        context.RegisterSourceOutput(provider, static (productionContext, model) =>
            SourceOutput.Emit(productionContext, model.Namespace, model.ClassName, model.Accessibility,
                model.Methods, model.Diagnostics, OracleSourceBuilder.EmitMethod, ".Oracle"));
    }
}
```

- 起点属性は**メソッドの builder 属性ではなく `[DataAccessor]`（クラス）**。クラス単位で 1 ファイル
  生成するため。
- `SourceOutput.Emit` の最終引数は **hint 名サフィックス**（`".Oracle"`）。コア Generator や他プロバイダと
  同じクラスへ同時に生成するので、**サフィックス無しだと hint 名が衝突**して片方が消える。必ず固有値を渡す。

### Step 3 — ModelBuilder（transform）

Postgres の形をそのまま踏襲する：

```csharp
internal static class OracleModelBuilder
{
    private const string Ns = "Smart.Data.Accessor.Attributes.Ora";

    private delegate OracleMethodModel? BuildMethod(MethodResolution resolution, MatchedMethod matched, List<DiagnosticInfo> diagnostics);

    private static readonly (string Attribute, BuildMethod Build)[] Targets =
    [
        (Ns + "InsertAttribute", BuildInsert),
        (Ns + "SelectAttribute", BuildSelect),
        // ...
    ];

    public static OracleClassModel Build(GeneratorAttributeSyntaxContext context, CancellationToken cancellation)
    {
        var scan = ClassScanner.ResolveClass(context);
        var diagnostics = new List<DiagnosticInfo>();
        var methods = new List<OracleMethodModel>();
        foreach (var (matched, build) in ClassScanner.EnumerateMethods(scan, Targets, diagnostics))
        {
            cancellation.ThrowIfCancellationRequested();
            var resolution = MethodResolver.Resolve(in scan, matched.Method, matched.Attribute, diagnostics, matched.Location);
            // resolution → 種別毎の Build デリゲートで Model 化。プロバイダ固有の検証・診断もここで。
        }
        // OracleClassModel(equatable) を返す
    }
}
```

- **`ClassScanner`** が partial 検証・メソッド列挙・属性マッチを、**`MethodResolver`** が
  メソッド形状（接続引数・値パラメータ・戻り値等）の解決と共通診断（SDA1001〜1006）を肩代わりする。
  自前で書くのは「属性 → 種別 Model の変換」と**プロバイダ固有の検証**だけ。
- 種別は enum で持たず、**属性名 → Build デリゲートの対応表**で分岐する（種別追加が表 1 行で済む）。

### Step 4 — Models（internal・equatable）

`Models/` に `OracleClassModel` / `OracleMethodModel` ＋種別毎の Model（`OracleInsertModel` …）を
**1 record = 1 ファイル**で置く。全て `internal sealed record`、コレクションは `EquatableArray<T>`、
位置情報は `LocationInfo?`。Model の enum が Roslyn 型（`RefKind` / `Accessibility` 等）と名前衝突
する場合は接頭辞を付ける（コアは自前の ref-kind enum を `ParameterRefKind` としている）＝symbol を
併用するファイルでの衝突を避ける。Postgres の `Models/` をコピーして改名するのが最速で確実。

### Step 5 — SourceBuilder（emit）

```csharp
internal static class OracleSourceBuilder
{
    public static void EmitMethod(SourceBuilder builder, OracleMethodModel method)
    {
        SqlEmit.OpenMethod(builder, method.MethodName, method.ValueParams);
        //   → {Method}__QueryBuilder(ref BuilderContext context, <値引数>) と `var cmd = context.Command;` を出す

        // SQL 文字列の組み立て（プロバイダ方言はここが本体）
        SqlEmit.EmitCommandText(builder, sql);          // 静的 SQL の CommandText 代入
        SqlEmit.EmitColumnParameter(builder, ...);      // エンティティ列のパラメータ束縛
        SqlEmit.EmitValueParamBinding(builder, ...);    // 値引数の束縛

        SqlEmit.CloseMethod(builder);
    }
}
```

- 方言（`LIMIT`/`OFFSET`、`RETURNING`、identity 取得、`MERGE` …）はこの層に閉じ込める。
- パラメータ束縛は必ず共有 `SqlEmit` 経由にする。`[DbType]`/`[TypeHandler<>]`/enum underlying/
  `[AnsiString]` 等の型修飾がコアと同じ規則で適用される。

### Step 6 — 診断

- 共有診断（SDA1001〜1006：partial でない、builder 属性重複、エンティティ解決不能等）は
  `BuilderDiagnostics`（共有ソース）にあり、`ClassScanner`/`MethodResolver` が自動で出す。
- **プロバイダ固有の診断はサードパーティ独自の ID プレフィックス**（例：`ORA0001`）で自前定義する。
  `SDA` 帯はコア/同梱プロバイダが採番管理しているため使わない。
- 本リポジトリは AnalyzerReleases 台帳（RS2008 系のリリース追跡）を**不採用**にしている。
  診断はインクリメンタル Source Generator から報告し `DiagnosticAnalyzer` 型を持たないため、
  RS2008 はそもそも発火せず台帳ファイルも不要。`DiagnosticAnalyzer` ベースの規則を追加して
  リリース追跡を使いたい場合は各自で `AnalyzerReleases.Shipped/Unshipped.md` を
  `<AdditionalFiles>` に載せればよい。

### Step 7 — テスト

| レイヤ | 手段 | 検証できること / できないこと |
| --- | --- | --- |
| ハーネス | `GeneratorTestHelper` 流（`CSharpGeneratorDriver` にコア＋自作の 2 ジェネレータを載せる） | 生成テキストの形・診断 ID。**生成コードの CS エラーと DB 実行は検査できない** |
| 機能テスト | Mock 接続（`Usa.Smart.Mock.Data`）で実行 | 生成コードが実際にコンパイル・実行できること、`cmd.CommandText`・パラメータ束縛の中身 |
| 実 DB スモーク | テンポラリ project ＋実プロバイダ | 方言 SQL が本物の DB で通ること（**必須**。ハーネスでは絶対に取れない） |
| AOT | `PublishAot=true` の smoke | 生成コードの AOT 互換（emit が静的呼び出しだけなら通常グリーン） |

### Step 8 — 検証ゲート（リポジトリ規約）

1. `dotnet build -c Release --no-incremental` で **0 warning / 0 error**（incremental は IDE 系
   アナライザ警告を隠すため不可）。
2. ハーネス＋機能テスト green。
3. 実 DB スモーク。
4. 性能に効く変更をしたら `DapperComparisonBenchmark` 相当で**対直書き Ratio と Alloc Ratio(=1.00) が
   悪化していないこと**を確認する。

---

## 5. 共有部品カタログ

| 部品 | 場所 | 役割 |
| --- | --- | --- |
| `ClassScanner` / `ClassScan` / `MatchedMethod` | `Shared/Builders/` | `[DataAccessor]` クラスの走査、対象メソッド×属性の列挙、partial 検証 |
| `MethodResolver` / `MethodResolution` | 〃 | メソッド形状の解決（接続引数・値パラメータ・戻り値）と共通診断 |
| `MappingResolver` / `ColumnBinding` / `ParameterBinding` | 〃 | エンティティ列の解決（`[Name]`/`[Key]`/`[DatabaseManaged]`/`[Ignore]`、`[TypeMap]`、converter 束縛） |
| `SqlEmit` | 〃 | `__QueryBuilder` の開閉、`CommandText`、パラメータ束縛 emit（`QueryBuilderMethodSuffix` 定数もここ） |
| `SourceOutput` | 〃 | ファイルヘッダ・partial class の外殻・診断報告・hint 名（サフィックス付き） |
| `BuilderDiagnostics` | 〃 | 共有診断 SDA1001〜1006 |
| `CodeExpressionHelper` | `Shared/Helpers/` | C# 文字列リテラル化（`StringLiteral`）、converter 呼び出し式 |
| `ColumnAttributeHelper` / `MappingAttributeHelper` / `ConverterScopeHelper` | 〃 | 列属性・`[TypeMap]`・`[TypeHandler<>]` スコープの読み取り（コア Generator と同一規則） |
| `WellKnownTypeNames` | `Shared/` | 型名判定の定数 |
| `EquatableArray<T>` / `DiagnosticInfo` / `LocationInfo` / `SourceBuilder` / `InheritsFrom` ほか | NuGet `SourceGenerateHelper` | equatable モデル基盤・診断運搬・インデント付き出力・Roslyn 汎用拡張 |

---

## 6. 落とし穴（実際に踏んだものだけ）

1. **`default(EquatableArray<T>)`** — 列挙で NRE → ジェネレータが CS8785 で「全生成が黙って消える」。
   症状が「生成物が無い／CS8795 が大量」のときはまずこれを疑う。
2. **Model への `ISymbol` 混入** — 例外は出ないがキャッシュが死に、大規模ソリューションで
   IDE が重くなる形で顕在化する。Model のフィールドは string/プリミティブ/`EquatableArray`/
   `LocationInfo` だけにする。
3. **hint 名サフィックスの付け忘れ** — 同一クラスに複数ジェネレータが出力するため衝突する。
4. **netstandard2.0** — `out var` の attribute 付き nullable 注釈等、BCL 側の欠けに注意
   （`NotNullWhen` 等は使えないので nullable 戻り値スタイルにする）。
5. **0 warning 運用** — analyzer/IDE 系警告は `--no-incremental` の clean build でのみ全量出る。
6. **struct/ブロックの閉じ忘れ**（`SourceBuilder.BeginScope`/`EndScope` の対応） — 生成コードの
   CS エラーはハーネスで検出されないため、機能テストまで進んで初めて分かる。emit を書いたら
   まず機能テストプロジェクトでビルドすること。
7. **行末** — working tree は CRLF（.editorconfig）、commit は LF（.gitattributes）。一括置換は
   `File.ReadAllText/WriteAllText` 系で EOL を保存する。

---

## 7. 出荷前チェックリスト

- [ ] 属性は `QueryBuilderAttribute` 派生・flat namespace・クラス名にプロバイダ接頭辞
- [ ] `{Method}__QueryBuilder(ref BuilderContext context, ...)` 契約は共有 `SqlEmit` 経由で生成
- [ ] Model は internal・equatable・シンボル非混入（`WithTrackingName` 済み）
- [ ] 診断は独自プレフィックス（帯域内は欠番なしの連番）
- [ ] ハーネス／機能（Mock）／実 DB／AOT の 4 層で green
- [ ] `--no-incremental` clean build 0 warning
- [ ] NuGet に `analyzers/dotnet/cs` としてジェネレータ dll＋`SourceGenerateHelper.dll` を同梱、
      新規プロジェクトで PackageReference して生成が走ることを確認
- [ ] 性能検証（対直書き Ratio・Alloc Ratio が基準から悪化しないこと）

## 8. 参照実装の読み順

1. `Smart.Data.Accessor.Builders.Postgres.Generator/PostgresQueryBuilderGenerator.cs`（配線・25 行）
2. 同 `PostgresModelBuilder.cs`（transform と対応表）→ `Models/`（equatable Model の粒度）
3. 同 `PostgresSourceBuilder.cs`（方言 emit。`RETURNING` がプロバイダ固有処理の好例）
4. `Smart.Data.Accessor.Shared/Builders/`（共有メカニクスの実体）
5. 差分学習として SqlServer（`OUTPUT` 句・`MERGE`）・MySql（`ON DUPLICATE KEY UPDATE` /
   `REPLACE` / `INSERT IGNORE`）の同名ファイル
