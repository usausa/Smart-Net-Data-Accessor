namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Marks a string parameter/property to be sent as ANSI (DbType.AnsiString) instead of unicode.
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
public sealed class AnsiStringAttribute : Attribute
{
}
