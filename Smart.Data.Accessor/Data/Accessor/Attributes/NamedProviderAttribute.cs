namespace Smart.Data.Accessor.Attributes
{
    public sealed class NamedProviderAttribute : ProviderAttribute
    {
        public NamedProviderAttribute(string name)
            : base(typeof(NamedDbProviderSelector), name)
        {
        }
    }
}
