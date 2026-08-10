# Third-Party Generator Guide (provider-specific QueryBuilder)

Audience: developers who want to add a **QueryBuilder generator for their own database provider**
to Smart.Data.Accessor (e.g. an Oracle flavour with `[OraInsert]`/`[OraSelect]` ...). No changes to
the core generator are needed — the extension consists of **two new projects: an attribute package
and a generator**.

The reference implementations are the three bundled providers
(`Smart.Data.Accessor.Builders.Postgres` / `.SqlServer` / `.MySql`). The code fragments in this
guide use the Postgres implementation as the template. Reading the actual Postgres sources first
(about ten files in total) and then coming back to this guide is the fastest path.

> **Base conventions — read this first**
> This guide builds on the ecosystem-wide conventions in
> [`../../Generator0-Helper/docs/generator-guide.md`](../../Generator0-Helper/docs/generator-guide.md)
> (project layout, pipeline unitisation, model naming and ordering, diagnostics, testing, section
> naming) and only adds what is specific to the QueryBuilder extension point.
>
> Where the two overlap — §2.1 csproj, §3 design principles, §4 Step 4 (models) / Step 6
> (diagnostics) / Step 7 (tests), §6 pitfalls — **the ecosystem guide is the source of truth** and
> carries rules this document does not yet restate, notably:
>
> - Splitting diagnostics (`RegisterSourceOutput`) from code emission
>   (`RegisterImplementationSourceOutput` over a `SelectMany`-unitised provider)
> - Marker attributes are hand-written in the runtime library, never emitted with
>   `RegisterPostInitializationOutput`
> - Model naming (`{Xxx}Model` for primary models; the `Model` suffix is forbidden on
>   transform-internal carriers), property ordering, and the 12-property section-comment threshold
> - Severity policy: generation impossible → `Error`, generation continues but differs from intent
>   → `Warning`
> - Three-layer split is decided by testability, not by line count

---

## 1. Architecture: how work is split with the core

A Smart.Data.Accessor method is defined by two axes:

- **A-group (execution kind)** — `[Execute]` / `[ExecuteScalar]` / `[Query]` / `[QueryFirst]` /
  `[ExecuteReader]`. This is the **generation marker and it is mandatory** (a method with only a
  B-group attribute is an SDA0108 error).
- **B-group (command source)** — where the SQL comes from: a `.sql` file (default), `[Sql]`,
  `[DirectSql]`, `[Procedure]`, or a **QueryBuilder attribute (the extension point of this guide)**.

The split of responsibilities: **the core generates the whole method body** (connection, command,
execution, row mapping), and the provider generator only generates the helper that assembles the
SQL text and binds the parameters.

```
User code
  [DataAccessor] partial class FooAccessor
      [Query] [PgSelect(typeof(Entity))] partial IReadOnlyList<Entity> List(...);
                     │
      ┌──────────────┴───────────────────────────────┐
      │ Core generator (Smart.Data.Accessor.Generator)│ ← untouched
      │   generates the List() body and delegates SQL │
      │   assembly to List__QueryBuilder(ref ctx, ...)│
      └──────────────┬───────────────────────────────┘
                     │ joined by naming convention (validated at compile time)
      ┌──────────────┴───────────────────────────────┐
      │ Provider generator (what you build)           │
      │   generates                                   │
      │   private static void List__QueryBuilder(     │
      │       ref BuilderContext context, <values>)   │
      │   into the same partial class                 │
      └──────────────────────────────────────────────┘
```

### The contract (all you need to honour)

1. Derive your attributes from **`QueryBuilderAttribute`**. The core treats a method carrying a
   `QueryBuilderAttribute`-derived attribute as `SqlSource.QueryBuilder` and emits a call to
   `{MethodName}__QueryBuilder`.
2. Your generator emits
   `private static void {MethodName}__QueryBuilder(ref BuilderContext context, <value params>)`
   into the same partial class (the signature opening and `var cmd = context.Command;` come from
   the shared `SqlEmit`).
3. Exclusivity and consistency are enforced by core diagnostics: the A-group requirement
   (SDA0108), QueryBuilder × `[Procedure]`/`[DirectSql]` (SDA0105), × `[Sql]` (SDA0107), a
   coexisting `.sql` file (SDA0405), duplicated QueryBuilder attributes (SDA1002).
   **Do not re-implement these on the provider side.**

---

## 2. Deliverables: two projects

| Project | TFM | Role |
| --- | --- | --- |
| `<Your>.Builders.Xxx` | `net8.0;net9.0;net10.0` (`IsAotCompatible`) | The **attribute package** users reference. Ships the generator dll inside the NuGet |
| `<Your>.Builders.Xxx.Generator` | `netstandard2.0` | The **generator itself** (distributed as a Roslyn analyzer) |

### 2.1 Generator csproj (essentials)

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
  <!-- Shared generator source (linked, not a DLL): Helpers/ = attribute readers shared with the
       core generator, Builders/ = the QueryBuilder engine shared across builder generators. -->
  <Compile Include="..\Smart.Data.Accessor.Shared\**\*.cs" Link="Shared\%(RecursiveDir)%(Filename)%(Extension)" />
</ItemGroup>

```

Notes:

- Generators are **netstandard2.0 only** (they must run inside the .NET Framework host of Visual
  Studio). Modern C# syntax is fine, but the BCL surface is netstandard2.0.
- Shared pieces come in as **linked source** (`Smart.Data.Accessor.Shared/**`), not as a DLL, so
  version skew between generators is structurally impossible. A third party building outside this
  repository copies that folder into its own tree (keeping the `Smart.Data.Accessor.Shared.*`
  namespaces is fine).
- `SourceGenerateHelper` (SGH) is the equatable-model foundation (`EquatableArray<T>`,
  `DiagnosticInfo`, `LocationInfo`, `SourceBuilder`, ...). `GeneratePathProperty=true` supports the
  packaging step below and analyzer debugging.

### 2.2 Attribute package csproj (essentials)

```xml
<ItemGroup>
  <ProjectReference Include="..\Smart.Data.Accessor\Smart.Data.Accessor.csproj" />
  <ProjectReference Include="..\<Your>.Builders.Xxx.Generator\<Your>.Builders.Xxx.Generator.csproj"
                    PrivateAssets="all" ReferenceOutputAssembly="false" OutputItemType="Analyzer" />
</ItemGroup>

<!-- Bundle the generator dll and the SGH dll into the NuGet as analyzers -->
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

With `OutputItemType="Analyzer"` the generator runs both for consumers of the package and for the
attribute project's own build.

---

## 3. Design principles (model-driven)

`IIncrementalGenerator` caches by **value equality of the pipeline inputs**. The iron rules that
keep the cache alive:

1. **Never let Roslyn symbols flow through the pipeline.** Use `ISymbol` / `SyntaxNode` /
   `Compilation` only inside the transform (the `ForAttributeWithMetadataName` delegate) and pass
   nothing but an **equatable record model** to `RegisterSourceOutput`. Symbols are per-Compilation
   instances; letting one in silently disables caching and regenerates everything on every keystroke.
2. **Collections must be `EquatableArray<T>`** (SGH). `List<T>`/arrays compare by reference.
   ⚠ `default(EquatableArray<T>)` throws NRE on enumeration and the whole generator dies silently
   as CS8785. Always construct explicitly (`new EquatableArray<T>(items.ToArray())`) or keep the
   field nullable.
3. **Models are owned internally per generator — never shared across generators.** What may be
   shared is the *mechanics* (scanning, resolution, emit primitives) and member-info return values
   (`TypeMapInfo` / `ColumnAttributeInfo` etc.). Sharing models lets provider-specific concerns
   (e.g. Postgres `RETURNING`) leak into a shared type — this codebase went through exactly that
   failure once and was deliberately restructured so each provider owns its Model/Transform/Emit.
4. **Emit (SourceBuilder) is a pure, symbol-free function of the model.** That makes it unit
   testable with hand-built models.
5. **Diagnostics travel on the model as `DiagnosticInfo`** (SGH); they are reported in the output
   stage, not from the transform.

---

## 4. Implementation steps

### Step 1 — define the attributes (attribute package)

```csharp
namespace Smart.Data.Accessor.Attributes;   // flat namespace (the convention for all attributes)

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class OraInsertAttribute : QueryBuilderAttribute   // ← deriving is the contract
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

- The namespace stays **flat** (`Smart.Data.Accessor.Attributes`); the provider prefix goes on the
  class name (`Pg*` / `Ora*` ...), following the bundled providers.
- Provider-specific options are named properties (e.g. Postgres has `Returning`).

### Step 2 — the generator wiring (one file, ~25 lines)

```csharp
[Generator]
public sealed class OracleQueryBuilderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ClassScanner.DataAccessorAttributeName,          // scan from [DataAccessor] classes
                static (node, _) => node is ClassDeclarationSyntax,
                static (context, cancellation) => OracleModelBuilder.Build(context, cancellation))
            .WithTrackingName(ClassScanner.TrackingName);

        context.RegisterSourceOutput(provider, static (productionContext, model) =>
            SourceOutput.Emit(productionContext, model.Namespace, model.ClassName, model.Accessibility,
                model.Methods, model.Diagnostics, OracleSourceBuilder.EmitMethod, ".Oracle"));
    }
}
```

- The trigger attribute is **`[DataAccessor]` on the class**, not the per-method builder attribute,
  because output is one file per class.
- The last argument of `SourceOutput.Emit` is the **hint-name suffix** (`".Oracle"`). The core
  generator and other providers emit into the same class, so **without a unique suffix the hint
  names collide** and one output silently wins. Always pass a distinct value.

### Step 3 — the ModelBuilder (transform)

Follow the Postgres shape verbatim:

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
            var resolution = MethodResolver.Resolve(in scan, matched.Method, matched.Attribute, matched.Naming, diagnostics, matched.Location);
            // resolution → per-kind Build delegate creates the model; provider-specific validation
            // and diagnostics also live here.
        }
        // return the equatable OracleClassModel
    }
}
```

- **`ClassScanner`** covers partial validation, method enumeration and attribute matching;
  **`MethodResolver`** covers method-shape resolution (connection argument, value parameters,
  return type) plus the shared diagnostics (SDA1001–1006). What you write yourself is only the
  "attribute → per-kind model" conversion and **provider-specific validation**.
- Kinds are not an enum but an **attribute-name → build-delegate table** (adding a kind is one
  table row).

### Step 4 — the Models (internal, equatable)

Put `OracleClassModel` / `OracleMethodModel` plus per-kind models (`OracleInsertModel` ...) under
`Models/`, one record per file. All of them are `internal sealed record`; collections are
`EquatableArray<T>`; positions are `LocationInfo?`. When a model enum would shadow a Roslyn type
(e.g. `RefKind`, `Accessibility`), prefix it so the two do not clash in a file that also touches
symbols — the core, for instance, names its own ref-kind enum `ParameterRefKind`. Copying the
Postgres `Models/` folder and renaming is the fastest reliable start.

### Step 5 — the SourceBuilder (emit)

```csharp
internal static class OracleSourceBuilder
{
    public static void EmitMethod(SourceBuilder builder, OracleMethodModel method)
    {
        SqlEmit.OpenMethod(builder, method.MethodName, method.ValueParams);
        //   → emits {Method}__QueryBuilder(ref BuilderContext context, <values>) and `var cmd = context.Command;`

        // assemble the SQL text — the provider dialect lives here
        SqlEmit.EmitCommandText(builder, sql);          // CommandText assignment for static SQL
        SqlEmit.EmitColumnParameter(builder, ...);      // entity-column parameter binding
        SqlEmit.EmitValueParamBinding(builder, ...);    // value-argument binding

        SqlEmit.CloseMethod(builder);
    }
}
```

- Dialect concerns (`LIMIT`/`OFFSET`, `RETURNING`, identity retrieval, `MERGE`, ...) are confined
  to this layer.
- Always bind parameters through the shared `SqlEmit` so `[DbType]` / `[TypeHandler<>]` /
  enum-underlying / `[AnsiString]` qualifiers apply with exactly the core rules.

### Step 6 — diagnostics

- Shared diagnostics (SDA1001–1006: non-partial container, duplicated builder attributes,
  unresolvable entity, ...) live in `BuilderDiagnostics` (shared source) and are raised by
  `ClassScanner`/`MethodResolver` automatically.
- **Provider-specific diagnostics use your own ID prefix** (e.g. `ORA0001`). The `SDA` band is
  managed by the core and bundled providers — do not use it.
- This repository does **not** use the AnalyzerReleases ledger (the RS2008-family release
  tracking): diagnostics are reported from the incremental source generators, not from
  `DiagnosticAnalyzer` types, so RS2008 never triggers and no ledger files are needed. If you add
  `DiagnosticAnalyzer`-based rules and want release tracking, add your own
  `AnalyzerReleases.Shipped/Unshipped.md` as `<AdditionalFiles>`.

### Step 7 — tests

| Layer | Vehicle | What it can / cannot verify |
| --- | --- | --- |
| Harness | `GeneratorTestHelper`-style (`CSharpGeneratorDriver` running core + your generator together) | Generated-text shapes and diagnostic IDs. **Cannot detect CS errors in the generated code nor run against a DB** |
| Functional | Mock connections (`Usa.Smart.Mock.Data`) | That the generated code actually compiles and runs; `cmd.CommandText` and parameter contents |
| Real-DB smoke | A temporary project against the real provider | That the dialect SQL is accepted by the actual database (**mandatory** — nothing else catches this) |
| AOT | A `PublishAot=true` smoke | AOT compatibility of the generated code (normally green as long as the emit is static calls only) |

### Step 8 — verification gates (repository conventions)

1. `dotnet build -c Release --no-incremental` with **0 warnings / 0 errors** (incremental builds
   hide IDE-series analyzer warnings).
2. Harness + functional tests green.
3. Real-DB smoke.
4. If the change can affect performance, confirm with a `DapperComparisonBenchmark`-style run that
   the **ratio vs hand-written and the Alloc Ratio (=1.00) do not regress**.

---

## 5. Shared-parts catalogue

| Part | Location | Role |
| --- | --- | --- |
| `ClassScanner` / `ClassScan` / `MatchedMethod` | `Shared/Builders/` | Scanning `[DataAccessor]` classes, enumerating method×attribute matches, partial validation |
| `MethodResolver` / `MethodResolution` | 〃 | Method-shape resolution (connection argument, value params, return type) and shared diagnostics |
| `MappingResolver` / `ColumnBinding` / `ParameterBinding` | 〃 | Entity-column resolution (`[Name]`/`[Key]`/`[DatabaseManaged]`/`[Ignore]`, `[TypeMap]`, converter binding) |
| `SqlEmit` | 〃 | Opening/closing `__QueryBuilder`, `CommandText`, parameter-binding emit (`QueryBuilderMethodSuffix` lives here) |
| `SourceOutput` | 〃 | File header, partial-class shell, diagnostic reporting, hint name (with suffix) |
| `BuilderDiagnostics` | 〃 | Shared diagnostics SDA1001–1006 |
| `CodeExpressionHelper` | `Shared/Helpers/` | C# string literalisation (`StringLiteral`), converter-call expressions |
| `ColumnAttributeHelper` / `MappingAttributeHelper` / `ConverterScopeHelper` | 〃 | Reading column attributes, `[TypeMap]`, `[TypeHandler<>]` scopes (identical rules to the core generator) |
| `NamingAttributeHelper` / `NameConverter` / `NamingConvention` | 〃 | `[Naming]` resolution (method → class → assembly, like `[BindPrefix]`) and the default-name conversion (snake_case etc.) applied by `ColumnAttributeHelper` / `MethodResolver` |
| `WellKnownTypeNames` | `Shared/` | Constants for type-name checks |
| `EquatableArray<T>` / `DiagnosticInfo` / `LocationInfo` / `SourceBuilder` / `InheritsFrom` etc. | NuGet `SourceGenerateHelper` | Equatable-model foundation, diagnostic transport, indented output, general Roslyn extensions |

---

## 6. Pitfalls (only the ones actually hit)

1. **`default(EquatableArray<T>)`** — NRE on enumeration → the generator dies as CS8785 and "all
   output silently disappears". When the symptom is "no generated files / a flood of CS8795",
   suspect this first.
2. **An `ISymbol` sneaking into a model** — no exception, but caching dies; it shows up as the IDE
   getting sluggish on large solutions. Keep model fields to strings/primitives/`EquatableArray`/
   `LocationInfo`.
3. **Forgetting the hint-name suffix** — multiple generators emit into the same class; outputs collide.
4. **netstandard2.0** — mind the missing BCL pieces (e.g. no `NotNullWhen` on out params; prefer
   nullable return values).
5. **Zero-warning policy** — analyzer/IDE warnings only show in full on a `--no-incremental`
   clean build.
6. **Unbalanced `BeginScope`/`EndScope`** in emit — CS errors in generated code are invisible to
   the harness and only surface in the functional test project. After writing emit code, build the
   functional tests first.
7. **Line endings** — this repository keeps CRLF in the working tree (.editorconfig) and LF in
   commits (.gitattributes). Bulk edits should preserve EOLs (`File.ReadAllText`/`WriteAllText`).

---

## 7. Pre-release checklist

- [ ] Attributes derive from `QueryBuilderAttribute`, flat namespace, provider-prefixed class names
- [ ] `{Method}__QueryBuilder(ref BuilderContext context, ...)` generated through the shared `SqlEmit`
- [ ] Models are internal, equatable, symbol-free (`WithTrackingName` wired)
- [ ] Own diagnostic prefix (gap-free sequential numbering within each band)
- [ ] Green across the four layers: harness / functional (Mock) / real DB / AOT
- [ ] `--no-incremental` clean build with 0 warnings
- [ ] NuGet ships the generator dll + `SourceGenerateHelper.dll` under `analyzers/dotnet/cs`;
      verified by consuming the package from a fresh project
- [ ] Performance check (ratio vs hand-written and Alloc Ratio do not regress)

## 8. Reading order for the reference implementation

1. `Smart.Data.Accessor.Builders.Postgres.Generator/PostgresQueryBuilderGenerator.cs` (wiring, 25 lines)
2. `PostgresModelBuilder.cs` (transform + delegate table) → `Models/` (model granularity)
3. `PostgresSourceBuilder.cs` (dialect emit; `RETURNING` is a good example of a provider-specific concern)
4. `Smart.Data.Accessor.Shared/Builders/` (the shared mechanics themselves)
5. For contrast: the same files in SqlServer (`OUTPUT` clause, `MERGE`) and MySql
   (`ON DUPLICATE KEY UPDATE` / `REPLACE` / `INSERT IGNORE`)
