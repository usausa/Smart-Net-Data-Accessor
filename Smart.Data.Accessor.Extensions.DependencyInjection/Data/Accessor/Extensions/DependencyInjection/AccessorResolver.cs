namespace Smart.Data.Accessor.Extensions.DependencyInjection
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Ignore")]
    internal sealed class AccessorResolver<T> : IAccessorResolver<T>
    {
        public T Accessor { get; }

        public AccessorResolver(DataAccessorFactory dataAccessorFactory)
        {
            Accessor = dataAccessorFactory.Create<T>();
        }
    }
}
