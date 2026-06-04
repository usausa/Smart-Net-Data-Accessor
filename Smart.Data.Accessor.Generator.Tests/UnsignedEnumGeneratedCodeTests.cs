namespace Smart.Data.Accessor.Generator.Tests;

using Xunit;

// Enums with unsigned / sbyte underlying types read the same-size signed reader method
// and apply an intermediate bit-preserving cast (no boxing GetValue<T> fallback).
public sealed class UnsignedEnumGeneratedCodeTests
{
    [Fact]
    public void UnsignedAndSByteEnumColumnsReadViaSignedCast()
    {
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            internal enum E8 : byte { A }
            internal enum ES8 : sbyte { A }
            internal enum E16 : ushort { A }
            internal enum E32 : uint { A }
            internal enum E64 : ulong { A }

            internal sealed class Entity
            {
                public E8 P8 { get; set; }
                public ES8 Ps8 { get; set; }
                public E16 P16 { get; set; }
                public E32 P32 { get; set; }
                public E64 P64 { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Entity> All();
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.All", "select P8, Ps8, P16, P32, P64 from T")).AllGeneratedText;

        // byte → GetByte, no intermediate cast (byte is already unsigned).
        Assert.Contains("(global::E8)__reader.GetByte(", text, StringComparison.Ordinal);
        // sbyte / ushort / uint / ulong → signed reader + intermediate bit-preserving cast.
        Assert.Contains("(global::ES8)(sbyte)__reader.GetByte(", text, StringComparison.Ordinal);
        Assert.Contains("(global::E16)(ushort)__reader.GetInt16(", text, StringComparison.Ordinal);
        Assert.Contains("(global::E32)(uint)__reader.GetInt32(", text, StringComparison.Ordinal);
        Assert.Contains("(global::E64)(ulong)__reader.GetInt64(", text, StringComparison.Ordinal);
        // No boxing GetValue<T> fallback for these enums.
        Assert.DoesNotContain("GetValue<global::E32>", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetValue<global::E64>", text, StringComparison.Ordinal);
    }
}
