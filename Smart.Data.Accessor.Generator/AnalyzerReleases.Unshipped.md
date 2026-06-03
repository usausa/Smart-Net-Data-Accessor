; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
SDA0100 | Sql      | Error    | Failed to tokenize SQL
SDA0101 | Sql      | Warning  | SQL is empty
SDA0102 | Sql      | Error    | SQL comment is not closed
SDA0103 | Sql      | Error    | SQL quote is not closed
SDA0104 | Sql      | Error    | Unknown SQL pragma
SDA0105 | Sql      | Error    | SQL code block has an unclosed brace
SDA0106 | Sql      | Error    | SQL code block has an unmatched closing brace
SDA0110 | Sql      | Warning  | SQL parameter does not match method parameters
SDA0111 | Sql      | Info     | Method parameter is unused in SQL
SDA0112 | Sql      | Warning  | SQL property accessor does not match a property
SDA0127 | Usage    | Warning  | [DirectSql] passes raw SQL; SQL injection is the caller's responsibility
SDA0128 | Usage    | Error    | [DirectSql] method first parameter must be string
SDA0129 | Usage    | Error    | [DirectSql] method must not have a corresponding SQL file
SDA0132 | Usage    | Error    | Duplicate [Name] on parameters or properties
SDA0133 | Mapping  | Info     | Record entity is mapped via primary constructor
SDA0134 | Usage    | Error    | [Execute] return type is not int/void/Task<int>/Task
SDA0136 | Usage    | Error    | Multiple execution-kind attributes on the same method
SDA0140 | Mapping  | Info     | Non-nullable reference property may receive DB NULL
SDA0142 | Mapping  | Error    | Converter TClr does not match property type
SDA0143 | Mapping  | Error    | Converter type does not implement IValueConverter<,>
SDA0144 | Mapping  | Error    | Converter type missing static abstract implementation
SDA0145 | Mapping  | Warning  | Multiple [TypeHandler] on same property
SDA0146 | Mapping  | Error    | [ExecuteConfig] target is not an [AccessorProfile]
SDA0147 | Mapping  | Error    | [AccessorProfile] class also has [ExecuteConfig] (circular)
SDA0152 | Builder  | Error    | Both SQL file and a QueryBuilder attribute are present (ambiguous)
SDA0157 | Builder  | Error    | QueryBuilder attribute combined with [Procedure] / [DirectSql]
SDA0158 | Builder  | Error    | [Procedure] combined with [DirectSql] (ambiguous)
SDA0170 | Usage    | Error    | [DataAccessor] class must not be nested
SDA0171 | Usage    | Error    | [DataAccessor] class must not be generic
SDA0172 | Usage    | Error    | Partial method implementation already exists
SDA0173 | Usage    | Error    | SQL file name collision
SDA0180 | Usage    | Error    | [Inject] Name is duplicated within the class
SDA0181 | Usage    | Warning  | [Inject] Type may not resolve from IServiceProvider
SDA0182 | Usage    | Info     | [Inject] declaration is not referenced in SQL or code
SDA0183 | Usage    | Warning  | [Provider] name is empty
SDA0185 | Usage    | Error    | [MethodName] is duplicated within the class
SDA0184 | Usage    | Info     | [Provider] has no effect on Pattern A only accessor
SDA0188 | Usage    | Error    | [Inject] Name conflicts with another member or method parameter
SDA0190 | Usage    | Error    | [Procedure] method must not have a corresponding SQL file
SDA0191 | Usage    | Error    | async [Procedure] cannot use out/ref parameters
SDA0192 | Usage    | Warning  | [Procedure] stored procedure name is empty
SDA0193 | Usage    | Error    | [ExecuteReader] return type is not a reader
SDA0194 | Usage    | Info     | [ExecuteReader] result must be disposed by the caller
SDA0195 | Usage    | Error    | [Direction] conflicts with the parameter modifier
SDA0197 | Usage    | Error    | [Direction] used on unsupported method kind
SDA0198 | Usage    | Warning  | IAsyncEnumerable method requires [EnumeratorCancellation] CancellationToken
SDA0200 | Usage    | Error    | [Direction(ReturnValue)] is not supported
SDA0201 | Usage    | Error    | [Direction] not allowed on [DirectSql] command-text parameter
SDA0203 | Usage    | Error    | Conflicting [DbType] / [DbType<TEnum>] on same parameter
SDA0204 | Usage    | Warning  | [DbType<TEnum>] TEnum is not in the provider enum whitelist
