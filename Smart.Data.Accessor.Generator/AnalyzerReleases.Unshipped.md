; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
SDA0100 | Sql      | Error    | Failed to tokenize SQL
SDA0101 | Sql      | Warning  | SQL is empty
SDA0110 | Sql      | Warning  | SQL parameter does not match method parameters
SDA0111 | Sql      | Info     | Method parameter is unused in SQL
SDA0130 | Builder  | Warning  | Entity has no [Key] for Update/Delete builder
SDA0131 | Builder  | Error    | Builder method requires entity parameter
