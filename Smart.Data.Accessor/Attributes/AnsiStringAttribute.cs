namespace Smart.Data.Accessor.Attributes;

using System;

// Marks a string parameter/property to be sent as ANSI (DbType.AnsiString) instead of unicode.
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class AnsiStringAttribute : Attribute
{
}
