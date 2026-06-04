namespace Smart.Data.Accessor.GeneratorShared;

using Microsoft.CodeAnalysis;

// Shared (linked source) reader for an entity property's column-mapping attributes ([Name] / [Key] /
// [DatabaseManaged] / [Ignore]). Both generators enumerate entity properties and build their OWN
// equatable Model from the result (core ColumnInfo for result mapping; Builder EntityColumn for
// INSERT/UPDATE) — only the per-property attribute reading is shared, not the Model. The result is a
// small value-equatable Info (named XxxInfo, not XxxModel).
internal static class ColumnAttributeHelper
{
    private const string NameAttributeFq = "Smart.Data.Accessor.Attributes.NameAttribute";
    private const string IgnoreAttributeFq = "Smart.Data.Accessor.Attributes.IgnoreAttribute";
    private const string KeyAttributeFq = "Smart.Data.Accessor.Attributes.KeyAttribute";
    private const string DatabaseManagedAttributeFq = "Smart.Data.Accessor.Attributes.DatabaseManagedAttribute";

    // Reads the column-mapping attributes of an entity property. ColumnName falls back to the property
    // name when [Name] is absent. (Consumers apply their own member-eligibility filter — accessibility,
    // static, get/set — before/after this.)
    public static ColumnAttributeInfo Read(IPropertySymbol prop)
    {
        string? name = null;
        var isKey = false;
        var isDatabaseManaged = false;
        var isIgnored = false;
        foreach (var attr in prop.GetAttributes())
        {
            switch (attr.AttributeClass?.ToDisplayString())
            {
                case NameAttributeFq when (attr.ConstructorArguments.Length > 0) && (attr.ConstructorArguments[0].Value is string nm):
                    name = nm;
                    break;
                case KeyAttributeFq:
                    isKey = true;
                    break;
                case DatabaseManagedAttributeFq:
                    isDatabaseManaged = true;
                    break;
                case IgnoreAttributeFq:
                    isIgnored = true;
                    break;
            }
        }
        return new ColumnAttributeInfo(name ?? prop.Name, isKey, isDatabaseManaged, isIgnored);
    }
}

// Equatable result of column-mapping attribute reading (an Info shared as a Model member, not a
// generator Mapping Model).
internal readonly record struct ColumnAttributeInfo(string ColumnName, bool IsKey, bool IsDatabaseManaged, bool IsIgnored);
