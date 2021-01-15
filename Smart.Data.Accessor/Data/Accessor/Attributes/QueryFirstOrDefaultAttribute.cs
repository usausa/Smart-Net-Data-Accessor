namespace Smart.Data.Accessor.Attributes
{
    public sealed class QueryFirstOrDefaultAttribute : LoaderMethodAttribute
    {
        public QueryFirstOrDefaultAttribute()
            : base(MethodType.QueryFirstOrDefault)
        {
        }
    }
}
