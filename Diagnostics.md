# Diagnostics

## Class

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SDA0001 | ❌ Error | `[DataAccessor]` class is not declared `partial` | Declare the class as `partial` |
| SDA0002 | ❌ Error | `[DataAccessor]` class is a nested type | Move the class to the top level |
| SDA0003 | ❌ Error | `[DataAccessor]` class is generic | Remove the type parameters from the class |
| SDA0004 | ❌ Error | `[Inject]` `Name` is declared more than once in the class | Give each `[Inject]` a unique `Name` |
| SDA0005 | ❌ Error | `[Inject]` `Name` conflicts with an existing field, property, or method parameter | Rename the `[Inject]` declaration |
| SDA0006 | ⚠️ Warning | `[Inject]` declares a type that may not resolve from `IServiceProvider`, and the runtime throws if it is unregistered | Register the type, or correct the `[Inject]` type |
| SDA0007 | ℹ️ Info | `[Inject]` `Name` is referenced neither in SQL nor in user code | Remove the unused `[Inject]` declaration |
| SDA0008 | ⚠️ Warning | `[Provider]` has an empty name, so `IDbProviderSelector.GetProvider` receives an empty string | Give `[Provider]` a name, or remove the attribute |
| SDA0009 | ℹ️ Info | `[Provider]` is set but the accessor has no Pattern B method, so the name is never used | Remove `[Provider]`, or add a Pattern B method |
| SDA0010 | ❌ Error | `[ExecuteConfig]` target type does not have `[AccessorProfile]` | Add `[AccessorProfile]` to the target type |
| SDA0011 | ❌ Error | `[AccessorProfile]` class also has `[ExecuteConfig]`, creating a circular reference | Remove `[ExecuteConfig]` from the profile class |
| SDA0012 | ⚠️ Warning | `[Naming]` specifies an undefined `NamingConvention` value and is treated as `None` | Specify a defined `NamingConvention` value |

## Method structure

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SDA0101 | ❌ Error | `[DataAccessor]` method is not a partial declaration | Declare the method as `partial` |
| SDA0102 | ❌ Error | A partial implementation is already present in source, so the generator cannot emit a second one | Remove the hand-written implementation |
| SDA0103 | ❌ Error | More than one execution-kind attribute is present; they are mutually exclusive | Leave a single execution-kind attribute |
| SDA0104 | ❌ Error | `[Procedure]` is combined with `[DirectSql]`, so the command source is ambiguous | Specify either `[Procedure]` or `[DirectSql]` |
| SDA0105 | ❌ Error | A QueryBuilder attribute is combined with `[Procedure]` or `[DirectSql]`, so the SQL source is ambiguous | Leave a single command-source attribute |
| SDA0106 | ❌ Error | `[MethodName]` is declared on multiple methods and would collide in SQL-file lookup | Give each method a unique `[MethodName]` |
| SDA0107 | ❌ Error | `[Sql]` is combined with another command-source attribute | Leave a single command-source attribute |
| SDA0108 | ❌ Error | A command-source attribute is present but the execution-kind attribute is missing | Add `[Execute]`, `[ExecuteScalar]`, `[ExecuteReader]`, `[Query]` or `[QueryFirst]` |
| SDA0109 | ❌ Error | `[ReaderBehavior]` is used on a method that is not `[ExecuteReader]` | Remove `[ReaderBehavior]`, or switch the method to `[ExecuteReader]` |

## Parameter

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SDA0201 | ❌ Error | Multiple parameters or properties share the same `[Name]` | Give each member a unique `[Name]` |
| SDA0202 | ❌ Error | `[DirectSql]` requires the first parameter (after conn/tx/CancellationToken) to be `string` | Make the first parameter a `string` command text |
| SDA0203 | ⚠️ Warning | `[Procedure]` has an empty stored procedure name | Give `[Procedure]` a stored procedure name |
| SDA0204 | ❌ Error | async `[Procedure]` uses an `out`/`ref` parameter | Switch to a synchronous method, or aggregate into a POCO |
| SDA0205 | ❌ Error | Parameter has both `[DbType(DbType)]` and `[DbType<TEnum>]` | Leave a single `[DbType]` attribute |
| SDA0206 | ⚠️ Warning | `TEnum` is not in the provider enum whitelist, so the provider-specific `DbType` assignment is skipped | Use a whitelisted provider enum |
| SDA0207 | ❌ Error | `[Direction]` conflicts with the parameter modifier | Align `[Direction]` with the `out`/`ref` modifier |
| SDA0208 | ❌ Error | Parameter has `[Direction]` but the method is not `[Procedure]` / `[Execute]` / `[DirectSql]` | Remove `[Direction]`, or change the execution kind |
| SDA0209 | ❌ Error | `[Direction(ReturnValue)]` is not supported; the stored procedure RETURN value maps to the scalar return value of the method | Remove `[Direction(ReturnValue)]` and use the return value |
| SDA0210 | ❌ Error | The command-text string parameter on a `[DirectSql]` method is annotated with `[Direction]` | Remove `[Direction]` from the command-text parameter |
| SDA0211 | ⚠️ Warning | `[Sql]` has an empty SQL text | Give `[Sql]` a SQL text |

## Return / mapping

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SDA0301 | ❌ Error | Return type is not supported | Use a supported return type |
| SDA0302 | ❌ Error | `[Execute]` return type is not `int` / `void` / `Task<int>` / `Task` / `ValueTask<int>` / `ValueTask` | Change the return type to one of the supported shapes |
| SDA0303 | ❌ Error | `[ExecuteReader]` return type is not `IDataReader` / `DbDataReader` or their async wrappers | Change the return type to a data reader shape |
| SDA0304 | ℹ️ Info | `[ExecuteReader]` returns a reader that owns its command, and its connection for Pattern B | Dispose the returned reader with `using` |
| SDA0305 | ⚠️ Warning | `IAsyncEnumerable<T>` method has no `CancellationToken` annotated with `[EnumeratorCancellation]` | Add a `CancellationToken` parameter with `[EnumeratorCancellation]` |
| SDA0306 | ℹ️ Info | Entity record is mapped via its primary constructor (positional binding) | No action needed; informational |
| SDA0307 | ℹ️ Info | Property is a non-nullable reference type, so DB NULL falls through as `default!` | Make the property nullable, or guarantee NOT NULL |
| SDA0308 | ❌ Error | Converter declares a `TClr` that does not match the property type | Align `TClr` with the property type |
| SDA0309 | ❌ Error | Converter type does not implement `IValueConverter<TDb, TClr>` | Implement `IValueConverter<TDb, TClr>` |
| SDA0310 | ❌ Error | Converter type does not provide a static implementation of `FromDb`/`ToDb` | Add the static `FromDb`/`ToDb` implementation |
| SDA0311 | ⚠️ Warning | Property has multiple `[TypeHandler<>]` attributes; only one converter is honored | Leave a single `[TypeHandler<>]` |
| SDA0312 | ❌ Error | Query element type has no mappable column (public settable/init property or record primary-constructor parameter) | Add a mappable member to the element type |

## SQL file resolution

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SDA0401 | ❌ Error | Neither a SQL file nor a Builder is specified; an additional file is expected | Add the SQL file, or specify a QueryBuilder attribute |
| SDA0402 | ❌ Error | Multiple SQL files resolve to the same logical name | Rename one of the SQL files |
| SDA0403 | ❌ Error | `[DirectSql]` method has a corresponding SQL file | Remove the SQL file, or drop `[DirectSql]` |
| SDA0404 | ❌ Error | `[Procedure]` method has a corresponding SQL file | Remove the SQL file, or drop `[Procedure]` |
| SDA0405 | ❌ Error | Both a SQL file and a QueryBuilder attribute are present, so resolution is ambiguous | Remove the SQL file, or drop the QueryBuilder attribute |
| SDA0406 | ❌ Error | `[Sql]` method has a corresponding SQL file | Remove the SQL file, or drop `[Sql]` |

## 2-way SQL

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SDA0501 | ❌ Error | SQL could not be tokenized | Fix the SQL reported in `detail` |
| SDA0502 | ⚠️ Warning | SQL is empty | Provide the SQL text |
| SDA0503 | ❌ Error | A SQL comment is not closed | Close the comment |
| SDA0504 | ❌ Error | A SQL string literal quote is not closed | Close the quote |
| SDA0505 | ❌ Error | SQL pragma is not `!helper` or `!using` | Use a supported pragma |
| SDA0506 | ❌ Error | A `/*% %/` code block opens a brace that is never closed | Balance the braces across the code blocks |
| SDA0507 | ❌ Error | A `/*% %/` code block has a closing brace with no matching opening brace | Balance the braces across the code blocks |
| SDA0508 | ⚠️ Warning | SQL parameter is not declared as a method parameter | Add the method parameter, or correct the SQL |
| SDA0509 | ℹ️ Info | Method parameter is declared but never referenced in SQL | Remove the parameter, or reference it in SQL |
| SDA0510 | ⚠️ Warning | `/*@ x.y */` references a property that is not declared on the parameter | Correct the property name, or add the property |

## Query builder

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SDA1001 | ❌ Error | A QueryBuilder attribute is on a method whose class is not `partial` | Declare the containing class as `partial` |
| SDA1002 | ❌ Error | More than one QueryBuilder attribute is present | Leave a single QueryBuilder attribute |
| SDA1003 | ❌ Error | QueryBuilder attribute specifies neither an entity type nor a table name | Specify the entity type with `typeof(T)`, or set `Table` |
| SDA1004 | ❌ Error | QueryBuilder needs an entity type to determine the column list | Specify the entity type with `typeof(T)` |
| SDA1005 | ⚠️ Warning | Entity has no property marked `[Key]`, so the builder cannot build its WHERE/ON clause | Mark the key property with `[Key]` |
| SDA1006 | ⚠️ Warning | `[TypeMap]` declares a `DbType` that conflicts with `[TypeHandler<>]`; `[TypeHandler]` takes precedence | Remove the conflicting `[TypeMap]` `DbType` |
