namespace Smart.Data.Accessor.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public sealed class ProviderAttribute : Attribute
    {
        public object Parameter { get; }

        public ProviderAttribute(object parameter)
        {
            Parameter = parameter;
        }
    }
}
