namespace Smart.Data.Accessor.Shared.Helpers;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

// [Naming(NamingConvention)] の走査(assembly / class / method の各スコープ共通)。スコープの優先順は
// 呼び出し側が method → class → assembly で連結する([BindPrefix] と同じ規則)。未定義の enum 値は None
// (変換なし)として扱い、警告(SDA0012)はコア Generator のみが報告する(Builder Generator との二重報告を避ける)。
// Reads [Naming(NamingConvention)] (shared by every scope: assembly / class / method). Callers chain the
// scopes method → class → assembly (the same rule as [BindPrefix]). An undefined enum value degrades to
// None (no conversion); only the core generator reports the warning (SDA0012) so the builder generators do
// not double-report it.
internal static class NamingAttributeHelper
{
    private const string NamingAttributeName = "Smart.Data.Accessor.Attributes.NamingAttribute";

    // [Naming] の規約値を取り出す(無ければ null。未定義値は None 扱い)。
    // Extract the [Naming] convention (null when absent; an undefined value degrades to None).
    public static NamingConvention? Resolve(ImmutableArray<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if ((attribute.AttributeClass?.ToDisplayString() == NamingAttributeName) &&
                (attribute.ConstructorArguments.Length > 0) &&
                (attribute.ConstructorArguments[0].Value is int value))
            {
                return IsDefined(value) ? (NamingConvention)value : NamingConvention.None;
            }
        }
        return null;
    }

    // 未定義の enum 値を持つ [Naming] を返す(コア Generator の SDA0012 用。無ければ null)。
    // Return the [Naming] carrying an undefined enum value (for the core generator's SDA0012; null when absent).
    public static AttributeData? FindInvalid(ImmutableArray<AttributeData> attributes, out int value)
    {
        foreach (var attribute in attributes)
        {
            if ((attribute.AttributeClass?.ToDisplayString() == NamingAttributeName) &&
                (attribute.ConstructorArguments.Length > 0) &&
                (attribute.ConstructorArguments[0].Value is int raw) &&
                !IsDefined(raw))
            {
                value = raw;
                return attribute;
            }
        }
        value = 0;
        return null;
    }

    private static bool IsDefined(int value) =>
        value is >= (int)NamingConvention.None and <= (int)NamingConvention.UpperCase;
}
