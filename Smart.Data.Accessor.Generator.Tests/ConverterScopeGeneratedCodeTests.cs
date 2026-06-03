namespace Smart.Data.Accessor.Generator.Tests;

using System;

using Xunit;

// Phase R2 (spec §7.7): verifies the generated source for the [TypeHandler<>] scope chain —
// member > method > class > profile precedence, writer-side ToDb, and scalar-return FromDb.
public sealed class ConverterScopeGeneratedCodeTests
{
    private const string TwoConverters = """
        using System;
        using System.Collections.Generic;
        using Smart.Data.Accessor.Attributes;
        using Smart.Data.Accessor.Converters;

        internal sealed class ClassConv : IValueConverter<long, DateTime>
        {
            public static DateTime FromDb(long v) => new(v);
            public static long ToDb(DateTime v) => v.Ticks;
        }

        internal sealed class PropConv : IValueConverter<long, DateTime>
        {
            public static DateTime FromDb(long v) => new(v);
            public static long ToDb(DateTime v) => v.Ticks;
        }
        """;

    [Fact]
    public void PropertyScopeOverridesClassScopeOnRead()
    {
        var source = TwoConverters + """

            internal sealed class Entity
            {
                public long Id { get; set; }
                [TypeHandler(typeof(PropConv))]
                public DateTime CreatedAt { get; set; }
            }

            [DataAccessor]
            [TypeHandler(typeof(ClassConv))]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Entity> QueryAll();
            }
            """;

        var result = GeneratorTestHelper.Run(source, ("Accessor.QueryAll", "select Id, CreatedAt from T"));
        var text = result.AllGeneratedText;

        // The property-scope converter wins; the class-scope one is never emitted (spec §7.7).
        Assert.Contains("PropConv.FromDb", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ClassConv", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ClassScopeConverterEmitsToDbOnWrite()
    {
        var source = TwoConverters + """

            [DataAccessor]
            [TypeHandler(typeof(ClassConv))]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Insert(DateTime createdAt);
            }
            """;

        var result = GeneratorTestHelper.Run(source, ("Accessor.Insert", "insert into T (C) values (/*@ createdAt */0)"));
        var text = result.AllGeneratedText;

        // 改善2: the class-scope [TypeHandler] writer binding goes through the converter-sharing overload
        // (the gen-time ClassConv.ToDb(createdAt) value expression disappears).
        Assert.Contains("AddInParameter<global::ClassConv, long, global::System.DateTime>(cmd, \"@p0\", createdAt)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ClassConv.ToDb(createdAt)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ClassScopeConverterWrapsScalarReadWithFromDb()
    {
        var source = TwoConverters + """

            [DataAccessor]
            [TypeHandler(typeof(ClassConv))]
            internal sealed partial class Accessor
            {
                [ExecuteScalar]
                public partial DateTime Max();
            }
            """;

        var result = GeneratorTestHelper.Run(source, ("Accessor.Max", "select max(C) from T"));
        var text = result.AllGeneratedText;

        // Scalar is read as TDb (long) then converted via FromDb (spec §7.4 / §7.7).
        Assert.Contains("ClassConv.FromDb(", text, StringComparison.Ordinal);
        Assert.Contains("ConvertScalar<", text, StringComparison.Ordinal);
    }
}
