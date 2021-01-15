namespace Smart.Data.Accessor.Attributes
{
    public sealed class QueryAttribute : LoaderMethodAttribute
    {
        public QueryAttribute()
            : base(MethodType.Query)
        {
        }
    }
}
