; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
SDA1001 | Usage    | Error    | QueryBuilder attribute container class must be partial
SDA1002 | Builder  | Error    | Multiple QueryBuilder attributes on one method
SDA1003 | Builder  | Error    | QueryBuilder attribute needs an entity type or a table name
SDA1004 | Builder  | Error    | Select/SelectSingle columns cannot be determined
SDA1005 | Builder  | Warning  | Entity has no [Key] for Update/Delete/SelectSingle builder
SDA1006 | Mapping  | Warning  | [TypeMap] DbType conflicts with [TypeHandler]
