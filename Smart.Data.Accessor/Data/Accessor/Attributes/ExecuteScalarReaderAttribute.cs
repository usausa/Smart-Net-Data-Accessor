namespace Smart.Data.Accessor.Attributes
{
    public sealed class ExecuteScalarReaderAttribute : LoaderMethodAttribute
    {
        public ExecuteScalarReaderAttribute()
            : base(MethodType.ExecuteReader)
        {
        }
    }
}
