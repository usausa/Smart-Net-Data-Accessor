namespace Smart.Data.Accessor.Resolver.Handlers;

using System.Reflection;

using Smart.ComponentModel;
using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Resolver.Providers;
using Smart.Resolver.Bindings;
using Smart.Resolver.Handlers;
using Smart.Resolver.Scopes;

public sealed class AccessorMissingHandler : IMissingHandler
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Factory")]
    public IEnumerable<Binding> Handle(ComponentContainer components, BindingTable table, Type type)
    {
        if (!type.IsInterface || (type.GetCustomAttribute<DataAccessorAttribute>() is null))
        {
            return Enumerable.Empty<Binding>();
        }

        return new[]
        {
            new Binding(type, new DataAccessorProvider(type), new SingletonScope(components), null, null, null)
        };
    }
}
