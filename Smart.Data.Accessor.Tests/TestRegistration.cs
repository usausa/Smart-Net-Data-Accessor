namespace Smart.Data.Accessor.Tests;

using Microsoft.Extensions.DependencyInjection;

using Smart.Data.Accessor.Attributes;

// [DataAccessorRegistration] placeholders whose implementation parts the generator fills: one singleton
// registration per accessor of this assembly (AddTestDataAccessors), or per accessor under the Accessors
// namespace (AddTestAccessorsNamespace; the same set here, exercising the Namespace filter path).
internal static partial class TestRegistration
{
    [DataAccessorRegistration]
    public static partial IServiceCollection AddTestDataAccessors(this IServiceCollection services);

    [DataAccessorRegistration(Namespace = "Smart.Data.Accessor.Tests.Accessors")]
    public static partial IServiceCollection AddTestAccessorsNamespace(this IServiceCollection services);
}
