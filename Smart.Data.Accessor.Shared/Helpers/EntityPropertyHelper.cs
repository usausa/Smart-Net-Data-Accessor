namespace Smart.Data.Accessor.Shared.Helpers;

using Microsoft.CodeAnalysis;

internal static class EntityPropertyHelper
{
    public static List<IPropertySymbol> GetInstanceProperties(INamedTypeSymbol type)
    {
        var types = new List<INamedTypeSymbol>();
        for (var current = type; (current is not null) && (current.SpecialType != SpecialType.System_Object); current = current.BaseType)
        {
            types.Add(current);
        }

        var hidden = new HashSet<string>(StringComparer.Ordinal);
        var properties = new List<IPropertySymbol>[types.Count];
        for (var i = 0; i < types.Count; i++)
        {
            properties[i] = [];
            var declared = new List<string>();
            foreach (var member in types[i].GetMembers())
            {
                if (hidden.Contains(member.Name))
                {
                    continue;
                }
                if (member is IPropertySymbol { IsStatic: false, IsIndexer: false } property)
                {
                    properties[i].Add(property);
                }
                if (member.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal)
                {
                    declared.Add(member.Name);
                }
            }
            hidden.UnionWith(declared);
        }

        var result = new List<IPropertySymbol>();
        for (var i = types.Count - 1; i >= 0; i--)
        {
            result.AddRange(properties[i]);
        }
        return result;
    }
}
