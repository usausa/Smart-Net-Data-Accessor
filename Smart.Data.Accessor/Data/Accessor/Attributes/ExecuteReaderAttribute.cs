namespace Smart.Data.Accessor.Attributes
{
    public sealed class ExecuteReaderAttribute : LoaderMethodAttribute
    {
        public ExecuteReaderAttribute()
            : base(MethodType.ExecuteReader)
        {
        }
    }
}
