namespace Smart.Data.Accessor.Builders.Configs
{
    using System;

    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
    public sealed class EntitySuffixAttribute : Attribute
    {
        public string[] Values { get; }

        public EntitySuffixAttribute(params string[] values)
        {
            Values = values;
        }
    }
}
