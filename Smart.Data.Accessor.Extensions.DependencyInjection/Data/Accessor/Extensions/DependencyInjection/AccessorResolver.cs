namespace Smart.Data.Accessor.Extensions.DependencyInjection;

#pragma warning disable CA1812
internal sealed class AccessorResolver<T> : IAccessorResolver<T>
{
    public T Accessor { get; }

    public AccessorResolver(DataAccessorFactory dataAccessorFactory)
    {
        Accessor = dataAccessorFactory.Create<T>();
    }
}
#pragma warning restore CA1812
