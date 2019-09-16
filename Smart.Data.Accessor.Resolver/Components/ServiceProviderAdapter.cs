namespace Smart.Data.Accessor.Resolver.Components
{
    using System;

    using Smart.Resolver;

    public sealed class ServiceProviderAdapter : IServiceProvider
    {
        private readonly IResolver resolver;

        public ServiceProviderAdapter(IResolver resolver)
        {
            this.resolver = resolver;
        }

        public object GetService(Type serviceType) => resolver.Get(serviceType);
    }
}
