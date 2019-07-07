namespace Smart.Data.Accessor.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public abstract class ProviderAttribute : Attribute
    {
        public Type SelectorType { get; }

        public object Parameter { get; }

        protected ProviderAttribute(Type selectorType, object parameter)
        {
            SelectorType = selectorType;
            Parameter = parameter;
        }
    }
}
