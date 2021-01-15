namespace Smart.Data.Accessor.Attributes
{
    public sealed class ExecuteScalarAttribute : LoaderMethodAttribute
    {
        public ExecuteScalarAttribute()
            : base(MethodType.ExecuteScalar)
        {
        }
    }
}
