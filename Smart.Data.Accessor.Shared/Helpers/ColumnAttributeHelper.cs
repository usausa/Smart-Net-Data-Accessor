namespace Smart.Data.Accessor.Shared.Helpers;

using Microsoft.CodeAnalysis;

// Shared (linked source) reader for an entity property's column-mapping attributes ([Name] / [Key] /
// [DatabaseManaged] / [Ignore]). Both generators enumerate entity properties and build their OWN
// equatable Model from the result (core ColumnInfo for result mapping; Builder EntityColumn for
// INSERT/UPDATE) — only the per-property attribute reading is shared, not the Model. The result is a
// small value-equatable Info (named XxxInfo, not XxxModel).
internal static class ColumnAttributeHelper
{
    private const string NameAttributeName = "Smart.Data.Accessor.Attributes.NameAttribute";
    private const string IgnoreAttributeName = "Smart.Data.Accessor.Attributes.IgnoreAttribute";
    private const string KeyAttributeName = "Smart.Data.Accessor.Attributes.KeyAttribute";
    private const string DatabaseManagedAttributeName = "Smart.Data.Accessor.Attributes.DatabaseManagedAttribute";

    // Reads the column-mapping attributes of an entity property. ColumnName falls back to the property
    // name when [Name] is absent. (Consumers apply their own member-eligibility filter — accessibility,
    // static, get/set — before/after this.)
    public static ColumnAttributeInfo Read(IPropertySymbol property)
    {
        string? name = null;
        var isKey = false;
        var isDatabaseManaged = false;
        var isIgnored = false;
        foreach (var attribute in property.GetAttributes())
        {
            switch (attribute.AttributeClass?.ToDisplayString())
            {
                case NameAttributeName when (attribute.ConstructorArguments.Length > 0) && (attribute.ConstructorArguments[0].Value is string nameValue):
                    name = nameValue;
                    break;
                case KeyAttributeName:
                    isKey = true;
                    break;
                case DatabaseManagedAttributeName:
                    isDatabaseManaged = true;
                    break;
                case IgnoreAttributeName:
                    isIgnored = true;
                    break;
            }
        }
        return new ColumnAttributeInfo(name ?? property.Name, isKey, isDatabaseManaged, isIgnored);
    }
}

// Equatable result of column-mapping attribute reading (an Info shared as a Model member, not a
// generator Mapping Model).
internal readonly record struct ColumnAttributeInfo(string ColumnName, bool IsKey, bool IsDatabaseManaged, bool IsIgnored);
