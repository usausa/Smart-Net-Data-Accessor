namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Marks a `static partial` extension method of IServiceCollection whose body the generator fills with
// a singleton registration per [DataAccessor] class of the compilation:
//   services.AddSingleton<TService>(static provider => new TAccessor(provider.GetRequiredService<IDbProvider>(), ...))
// Namespace narrows the accessors to that namespace and its sub-namespaces; several attributes union.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class DataAccessorRegistrationAttribute : Attribute
{
    public string? Namespace { get; set; }
}
