namespace Smart.Data.Accessor.AotTests;

using Microsoft.Extensions.DependencyInjection;

using Smart.Data.Accessor.Attributes;

// [DataAccessorRegistration] placeholder: the generator fills the implementation with the singleton
// registration of AotAccessor (static new + GetRequiredService, no reflection).
internal static partial class AotRegistration
{
    [DataAccessorRegistration]
    public static partial IServiceCollection AddAotDataAccessors(this IServiceCollection services);
}
