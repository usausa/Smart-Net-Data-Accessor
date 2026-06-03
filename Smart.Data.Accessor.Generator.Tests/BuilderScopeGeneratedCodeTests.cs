namespace Smart.Data.Accessor.Generator.Tests;

using System;

using Xunit;

// Phase R3: verifies the Builder generator honours the spec §7.7 [TypeHandler<>] scope chain
// (member > method > class > profile) on the write side (ToDb) and the property-scope [DbType] (F3).
public sealed class BuilderScopeGeneratedCodeTests
{
    private const string Preamble = """
        using System;
        using Smart.Data.Accessor.Attributes;
        using Smart.Data.Accessor.Builders;
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

        internal sealed class Entity
        {
            public long Id { get; set; }
            public DateTime CreatedAt { get; set; }
        }
        """;

    [Fact]
    public void MethodScopeConverterEmitsToDb()
    {
        var source = Preamble + """

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Insert(typeof(Entity), Table = "T")]
                [Execute]
                [TypeHandler(typeof(ClassConv))]
                public partial int Insert(Entity entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("AddInParameter<global::ClassConv, ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileScopeConverterEmitsToDb()
    {
        var source = Preamble + """

            [AccessorProfile]
            [TypeHandler(typeof(ClassConv))]
            internal static class Profile
            {
            }

            [DataAccessor]
            [ExecuteConfig(typeof(Profile))]
            internal sealed partial class Accessor
            {
                [Insert(typeof(Entity), Table = "T")]
                [Execute]
                public partial int Insert(Entity entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("AddInParameter<global::ClassConv, ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PropertyScopeOverridesClassScope()
    {
        var source = Preamble + """

            internal sealed class Entity2
            {
                public long Id { get; set; }
                [TypeHandler(typeof(PropConv))]
                public DateTime CreatedAt { get; set; }
            }

            [DataAccessor]
            [TypeHandler(typeof(ClassConv))]
            internal sealed partial class Accessor
            {
                [Insert(typeof(Entity2), Table = "T")]
                [Execute]
                public partial int Insert(Entity2 entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("AddInParameter<global::PropConv, ", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ClassConv", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PropertyDbTypeEmitsParameterDbType()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;
            using Smart.Data.Accessor.Builders;

            internal sealed class Entity
            {
                public long Id { get; set; }
                [DbType(System.Data.DbType.AnsiString)]
                public string Name { get; set; } = string.Empty;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Insert(typeof(Entity), Table = "T")]
                [Execute]
                public partial int Insert(Entity entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        // The Name column carries an explicit DbType (F3), passed as the AddInParameter DbType argument.
        Assert.Contains("AddInParameter(cmd, \"@Name\", entity.Name, (global::System.Data.DbType)", text, StringComparison.Ordinal);
    }
}
