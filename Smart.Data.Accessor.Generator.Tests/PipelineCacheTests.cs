namespace Smart.Data.Accessor.Generator.Tests;

using SourceGenerateHelper.Testing;

public sealed class PipelineCacheTests
{
    private const string AccessorSource =
        """
        using System.Collections.Generic;
        using Smart.Data.Accessor.Attributes;

        internal sealed class Entity
        {
            public long Id { get; set; }

            public string Name { get; set; } = "";
        }

        [DataAccessor]
        internal sealed partial class Accessor
        {
            [Query]
            public partial IReadOnlyList<Entity> All();
        }
        """;

    private const string AccessorAddedSource =
        """
        using System.Collections.Generic;
        using Smart.Data.Accessor.Attributes;

        [DataAccessor]
        internal sealed partial class AddedAccessor
        {
            [Query]
            public partial IReadOnlyList<Entity> All();
        }
        """;

    private const string BuilderSource =
        """
        using Smart.Data.Accessor.Attributes;

        internal sealed class Entity
        {
            [Key]
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;
        }

        [DataAccessor]
        internal sealed partial class Accessor
        {
            [Insert(typeof(Entity))]
            [Execute]
            public partial int Insert(Entity entity);
        }
        """;

    private const string BuilderAddedSource =
        """
        using Smart.Data.Accessor.Attributes;

        [DataAccessor]
        internal sealed partial class AddedAccessor
        {
            [Insert(typeof(Entity))]
            [Execute]
            public partial int Insert(Entity entity);
        }
        """;

    private const string UnrelatedSource =
        """
        namespace Other;

        internal sealed class Unrelated;
        """;

    private const string AccessorWithRegistrationSource =
        """
        using System.Collections.Generic;
        using Microsoft.Extensions.DependencyInjection;
        using Smart.Data.Accessor.Attributes;

        internal sealed class Entity
        {
            public long Id { get; set; }

            public string Name { get; set; } = "";
        }

        [DataAccessor]
        internal sealed partial class Accessor
        {
            [Query]
            public partial IReadOnlyList<Entity> All();
        }

        internal static partial class Registration
        {
            [DataAccessorRegistration]
            public static partial IServiceCollection AddDataAccessors(this IServiceCollection services);
        }
        """;

    // ------------------------------------------------------------
    // Accessor
    // ------------------------------------------------------------

    [Fact]
    public void UnrelatedEditKeepsAccessorModelCached()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncremental(AccessorSource, UnrelatedSource, ("Accessor.All", "select Id, Name from T"), ("AddedAccessor.All", "select Id, Name from T"));

        // Assert
        Assert.Equal(result.FirstGeneratedText, result.SecondGeneratedText);
        Assert.NotEmpty(result.OutputReasons);
        Assert.DoesNotContain(result.OutputReasons, static x => x.IsChanged());
    }

    [Fact]
    public void AccessorEditRebuildsModel()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncremental(AccessorSource, AccessorAddedSource, ("Accessor.All", "select Id, Name from T"), ("AddedAccessor.All", "select Id, Name from T"));

        // Assert
        Assert.Contains(result.OutputReasons, static x => x.IsChanged());
    }

    // ------------------------------------------------------------
    // Registration
    // ------------------------------------------------------------

    [Fact]
    public void UnrelatedEditKeepsRegistrationCached()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncrementalWithServiceCollection(AccessorWithRegistrationSource, UnrelatedSource, ("Accessor.All", "select Id, Name from T"), ("AddedAccessor.All", "select Id, Name from T"));

        // Assert
        Assert.Equal(result.FirstGeneratedText, result.SecondGeneratedText);
        Assert.NotEmpty(result.OutputReasons);
        Assert.DoesNotContain(result.OutputReasons, static x => x.IsChanged());
    }

    [Fact]
    public void AccessorAddedRebuildsRegistration()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncrementalWithServiceCollection(AccessorWithRegistrationSource, AccessorAddedSource, ("Accessor.All", "select Id, Name from T"), ("AddedAccessor.All", "select Id, Name from T"));

        // Assert
        Assert.Contains(result.OutputReasons, static x => x.IsChanged());
        Assert.DoesNotContain("AddSingleton<global::AddedAccessor>", result.FirstGeneratedText, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<global::AddedAccessor>", result.SecondGeneratedText, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------
    // Builder
    // ------------------------------------------------------------

    [Fact]
    public void UnrelatedEditKeepsBuilderModelCached()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncrementalBuilder(BuilderSource, UnrelatedSource);

        // Assert
        Assert.Equal(result.FirstGeneratedText, result.SecondGeneratedText);
        Assert.NotEmpty(result.OutputReasons);
        Assert.DoesNotContain(result.OutputReasons, static x => x.IsChanged());
    }

    [Fact]
    public void BuilderEditRebuildsModel()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncrementalBuilder(BuilderSource, BuilderAddedSource);

        // Assert
        Assert.Contains(result.OutputReasons, static x => x.IsChanged());
    }
}
