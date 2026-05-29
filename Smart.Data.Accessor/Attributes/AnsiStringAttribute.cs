namespace Smart.Data.Accessor.Attributes;

using System;

// Marks a string parameter/property to be sent as ANSI (DbType.AnsiString) instead of unicode.
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class AnsiStringAttribute : Attribute
{
}
