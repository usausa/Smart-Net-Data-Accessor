namespace Smart.Data.Accessor.Generator.Tests;

// Verifies the reader-side [TypeHandler<>] converter validation (SDA0304–SDA0306, SDA0309 and SDA0311) wired through
// ConverterResolver. Each accessor is a [Query] (backed by a SQL file) so column mapping — and
// thus converter resolution — runs.
public partial class DiagnosticTests
{
    [Fact]
    public void Sda0304ConverterNotIValueConverterEmitsDiagnostic()
    {
        // SDA0304: the referenced converter type does not implement IValueConverter<,>.
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

        Assert.Contains(diagnostics, x => x.Id == "SDA0304");
    }

    [Fact]
    public void Sda0305ConverterTClrMismatchEmitsDiagnostic()
    {
        // SDA0305: converter TClr (int) does not match the property type (string).
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;
            using Smart.Data.Accessor.Converters;

            internal sealed class LongToIntConverter : IValueConverter<long, int>
            {
                public static int FromDb(long value) => (int)value;
                public static long ToDb(int value) => value;
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

        Assert.Contains(diagnostics, x => x.Id == "SDA0305");
    }

    [Fact]
    public void Sda0306ConverterStaticAbstractMissingEmitsDiagnostic()
    {
        // SDA0306: converter implements the interface only via explicit static members, so the
        // generated `TConverter.FromDb(...)` call cannot bind.
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;
            using Smart.Data.Accessor.Converters;

            internal sealed class ExplicitConverter : IValueConverter<long, int>
            {
                static int IValueConverter<long, int>.FromDb(long value) => (int)value;
                static long IValueConverter<long, int>.ToDb(int value) => value;
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

        Assert.Contains(diagnostics, x => x.Id == "SDA0306");
    }

    [Fact]
    public void Sda0309TypeHandlerDuplicatedEmitsDiagnostic()
    {
        // SDA0309: more than one [TypeHandler] on the same property (first wins).
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;
            using Smart.Data.Accessor.Converters;

            internal sealed class ConverterA : IValueConverter<long, int>
            {
                public static int FromDb(long value) => (int)value;
                public static long ToDb(int value) => value;
            }

            internal sealed class ConverterB : IValueConverter<long, int>
            {
                public static int FromDb(long value) => (int)value;
                public static long ToDb(int value) => value;
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

        Assert.Contains(diagnostics, x => x.Id == "SDA0309");
    }

    [Fact]
    public void Sda0311NonNullableReferenceColumnReportsInfoEmitsDiagnostic()
    {
        // SDA0311 (Info): a non-nullable reference-type column may receive DB NULL.
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

        Assert.Contains(diagnostics, x => x.Id == "SDA0311");
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
                public static DateTime FromDb(long value) => new(value, DateTimeKind.Utc);
                public static long ToDb(DateTime value) => value.Ticks;
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
