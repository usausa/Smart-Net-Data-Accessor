; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
SDA0001 | Usage    | Error    | DataAccessor class must be partial
SDA0002 | Usage    | Error    | [DataAccessor] class must not be nested
SDA0003 | Usage    | Error    | [DataAccessor] class must not be generic
SDA0010 | Usage    | Error    | [Inject] Name is duplicated within the class
SDA0011 | Usage    | Error    | [Inject] Name conflicts with another member or method parameter
SDA0012 | Usage    | Warning  | [Inject] Type may not resolve from IServiceProvider
SDA0013 | Usage    | Info     | [Inject] declaration is not referenced in SQL or code
SDA0014 | Usage    | Warning  | [Provider] name is empty
SDA0015 | Usage    | Info     | [Provider] has no effect on Pattern A only accessor
SDA0016 | Mapping  | Error    | [ExecuteConfig] target is not an [AccessorProfile]
SDA0017 | Mapping  | Error    | [AccessorProfile] class also has [ExecuteConfig] (circular)
SDA0101 | Usage    | Error    | DataAccessor method must be a partial declaration
SDA0102 | Usage    | Error    | Partial method implementation already exists
SDA0103 | Usage    | Error    | Multiple execution-kind attributes on the same method
SDA0104 | Builder  | Error    | [Procedure] combined with [DirectSql] (ambiguous)
SDA0105 | Builder  | Error    | QueryBuilder attribute combined with [Procedure] / [DirectSql]
SDA0106 | Usage    | Error    | [MethodName] is duplicated within the class
SDA0201 | Usage    | Error    | Duplicate [Name] on parameters or properties
SDA0203 | Usage    | Error    | [DirectSql] method first parameter must be string
SDA0204 | Usage    | Warning  | [Procedure] stored procedure name is empty
SDA0205 | Usage    | Error    | async [Procedure] cannot use out/ref parameters
SDA0206 | Usage    | Error    | Conflicting [DbType] / [DbType<TEnum>] on same parameter
SDA0207 | Usage    | Warning  | [DbType<TEnum>] TEnum is not in the provider enum whitelist
SDA0208 | Usage    | Error    | [Direction] conflicts with the parameter modifier
SDA0209 | Usage    | Error    | [Direction] used on unsupported method kind
SDA0210 | Usage    | Error    | [Direction(ReturnValue)] is not supported
SDA0211 | Usage    | Error    | [Direction] not allowed on [DirectSql] command-text parameter
SDA0301 | Usage    | Error    | Unsupported return type
SDA0302 | Usage    | Error    | [Execute] return type is not int/void/Task<int>/Task
SDA0303 | Usage    | Error    | [ExecuteReader] return type is not a reader
SDA0304 | Usage    | Info     | [ExecuteReader] result must be disposed by the caller
SDA0305 | Usage    | Warning  | IAsyncEnumerable method requires [EnumeratorCancellation] CancellationToken
SDA0306 | Mapping  | Info     | Record entity is mapped via primary constructor
SDA0307 | Mapping  | Info     | Non-nullable reference property may receive DB NULL
SDA0308 | Mapping  | Error    | Converter TClr does not match property type
SDA0309 | Mapping  | Error    | Converter type does not implement IValueConverter<,>
SDA0310 | Mapping  | Error    | Converter type missing static FromDb/ToDb implementation
SDA0311 | Mapping  | Warning  | Multiple [TypeHandler] on same property
SDA0401 | Usage    | Error    | SQL file not found & Builder not specified
SDA0402 | Usage    | Error    | SQL file name collision
SDA0403 | Usage    | Error    | [DirectSql] method must not have a corresponding SQL file
SDA0404 | Usage    | Error    | [Procedure] method must not have a corresponding SQL file
SDA0405 | Builder  | Error    | Both SQL file and a QueryBuilder attribute are present (ambiguous)
SDA0501 | Sql      | Error    | Failed to tokenize SQL
SDA0502 | Sql      | Warning  | SQL is empty
SDA0503 | Sql      | Error    | SQL comment is not closed
SDA0504 | Sql      | Error    | SQL quote is not closed
SDA0505 | Sql      | Error    | Unknown SQL pragma
SDA0506 | Sql      | Error    | SQL code block has an unclosed brace
SDA0507 | Sql      | Error    | SQL code block has an unmatched closing brace
SDA0508 | Sql      | Warning  | SQL parameter does not match method parameters
SDA0509 | Sql      | Info     | Method parameter is unused in SQL
SDA0510 | Sql      | Warning  | SQL property accessor does not match a property
