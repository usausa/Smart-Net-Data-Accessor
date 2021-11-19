namespace Smart.Data.Accessor.Scripts;

using System;
using System.Collections;
using System.Runtime.CompilerServices;

public static class ScriptHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNull(object? value)
    {
        return value is null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotNull(object? value)
    {
        return value is not null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(string? value)
    {
        return (value is null) || (value.Length == 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotEmpty(string? value)
    {
        return (value is not null) && (value.Length > 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Any(Array? array)
    {
        return array?.Length > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Any(ICollection? ic)
    {
        return ic?.Count > 0;
    }
}
