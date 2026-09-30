namespace Smart.Data.Accessor.Shared.Helpers;

using System.Globalization;

using Microsoft.CodeAnalysis;

internal static class EnumValueHelper
{
    public static bool IsDefined(TypedConstant constant)
    {
        if ((constant.Type is not INamedTypeSymbol { TypeKind: TypeKind.Enum } type) || (constant.Value is null))
        {
            return true;
        }

        var value = ToInt64(constant);
        var flags = false;
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == "System.FlagsAttribute")
            {
                flags = true;
            }
        }

        var all = 0L;
        foreach (var member in type.GetMembers())
        {
            if (member is IFieldSymbol { HasConstantValue: true, ConstantValue: not null } field)
            {
                var bits = Convert.ToInt64(field.ConstantValue, CultureInfo.InvariantCulture);
                if (bits == value)
                {
                    return true;
                }

                all |= bits;
            }
        }

        return flags && ((value & ~all) == 0);
    }

    public static string ToText(TypedConstant constant) =>
        ToInt64(constant).ToString(CultureInfo.InvariantCulture);

    private static long ToInt64(TypedConstant constant) =>
        Convert.ToInt64(constant.Value, CultureInfo.InvariantCulture);
}
