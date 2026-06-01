namespace Smart.Data.Accessor.Generator.Tests;

using Xunit;

// Verifies the reader-side [TypeHandler<>] converter validation (SDA0140–SDA0145) wired through
// ConverterResolver. Each accessor is a [Query] (backed by a SQL file) so column mapping — and
// thus converter resolution — runs.
public sealed class ConverterDiagnosticTests
{
    [Fact]
    public void ConverterNotIValueConverter()
    {
        // SDA0143: the referenced converter type does not implement IValueConverter<,>.
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            internal sealed class NotAConverter
            {
            }

            internal sealed class Entity
            {
                [TypeHandler(typeof(NotAConverter))]
                public int Value { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Entity> Query();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Query", "select Value from T"));

        Assert.Contains(diagnostics, d => d.Id == "SDA0143");
    }

    [Fact]
    public void ConverterTClrMismatch()
    {
        // SDA0142: converter TClr (int) does not match the property type (string).
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;
            using Smart.Data.Accessor.Converters;

            internal sealed class LongToIntConverter : IValueConverter<long, int>
            {
                public static int FromDb(long v) => (int)v;
                public static long ToDb(int v) => v;
            }

            internal sealed class Entity
            {
                [TypeHandler(typeof(LongToIntConverter))]
                public string Value { get; set; } = string.Empty;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Entity> Query();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Query", "select Value from T"));

        Assert.Contains(diagnostics, d => d.Id == "SDA0142");
    }

    [Fact]
    public void ConverterStaticAbstractMissing()
    {
        // SDA0144: converter implements the interface only via explicit static members, so the
        // generated `TConverter.FromDb(...)` call cannot bind.
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;
            using Smart.Data.Accessor.Converters;

            internal sealed class ExplicitConverter : IValueConverter<long, int>
            {
                static int IValueConverter<long, int>.FromDb(long v) => (int)v;
                static long IValueConverter<long, int>.ToDb(int v) => v;
            }

            internal sealed class Entity
            {
                [TypeHandler(typeof(ExplicitConverter))]
                public int Value { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Entity> Query();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Query", "select Value from T"));

        Assert.Contains(diagnostics, d => d.Id == "SDA0144");
    }

    [Fact]
    public void ConverterTypeNotSupported()
    {
        // SDA0141: the property type (int) is not in the converter's [ConverterSupportedTypes] whitelist.
        const string source = """
            using System;
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;
            using Smart.Data.Accessor.Converters;

            [ConverterSupportedTypes(typeof(DateTime))]
            internal sealed class WhitelistConverter : IValueConverter<long, int>
            {
                public static int FromDb(long v) => (int)v;
                public static long ToDb(int v) => v;
            }

            internal sealed class Entity
            {
                [TypeHandler(typeof(WhitelistConverter))]
                public int Value { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Entity> Query();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Query", "select Value from T"));

        Assert.Contains(diagnostics, d => d.Id == "SDA0141");
    }

    [Fact]
    public void TypeHandlerDuplicated()
    {
        // SDA0145: more than one [TypeHandler] on the same property (first wins).
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;
            using Smart.Data.Accessor.Converters;

            internal sealed class ConverterA : IValueConverter<long, int>
            {
                public static int FromDb(long v) => (int)v;
                public static long ToDb(int v) => v;
            }

            internal sealed class ConverterB : IValueConverter<long, int>
            {
                public static int FromDb(long v) => (int)v;
                public static long ToDb(int v) => v;
            }

            internal sealed class Entity
            {
                [TypeHandler(typeof(ConverterA))]
                [TypeHandler(typeof(ConverterB))]
                public int Value { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Entity> Query();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Query", "select Value from T"));

        Assert.Contains(diagnostics, d => d.Id == "SDA0145");
    }

    [Fact]
    public void NonNullableReferenceColumnReportsInfo()
    {
        // SDA0140 (Info): a non-nullable reference-type column may receive DB NULL.
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public string Name { get; set; } = string.Empty;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Entity> Query();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Query", "select Name from T"));

        Assert.Contains(diagnostics, d => d.Id == "SDA0140");
    }

    [Fact]
    public void ValidConverterReportsNoDiagnostic()
    {
        // A well-formed converter on a value-type property produces no SDA diagnostic.
        const string source = """
            using System;
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;
            using Smart.Data.Accessor.Converters;

            internal sealed class TicksConverter : IValueConverter<long, DateTime>
            {
                public static DateTime FromDb(long v) => new(v, DateTimeKind.Utc);
                public static long ToDb(DateTime v) => v.Ticks;
            }

            internal sealed class Entity
            {
                public long Id { get; set; }

                [TypeHandler(typeof(TicksConverter))]
                public DateTime Created { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Entity> Query();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Query", "select Id, Created from T"));

        Assert.Empty(diagnostics);
    }
}
