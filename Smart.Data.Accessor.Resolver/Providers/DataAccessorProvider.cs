namespace Smart.Data.Accessor.Resolver.Providers
{
    using System;

    using Smart.Resolver;
    using Smart.Resolver.Bindings;
    using Smart.Resolver.Providers;

    public sealed class DataAccessorProvider : IProvider
    {
        public Type TargetType { get; }

        public DataAccessorProvider(Type type)
        {
            TargetType = type;
        }

        public Func<IResolver, object> CreateFactory(IKernel kernel, IBinding binding)
        {
            return r =>
            {
                var factory = r.Get<DataAccessorFactory>();
                return factory.Create(TargetType);
            };
        }
    }
}
