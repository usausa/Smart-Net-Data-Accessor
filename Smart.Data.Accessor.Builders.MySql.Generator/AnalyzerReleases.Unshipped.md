; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
SDA0130 | Builder  | Warning  | Entity has no [Key] for Update/Delete/SelectSingle builder
SDA0148 | Mapping  | Warning  | [TypeMap] DbType conflicts with [TypeHandler]
SDB0002 | Usage    | Error    | Invalid Builder container
SDB0004 | Builder  | Error    | QueryBuilder attribute needs an entity type or a table name
SDB0005 | Builder  | Error    | Select/SelectSingle columns cannot be determined
SDB0006 | Builder  | Error    | Multiple QueryBuilder attributes on one method
