namespace Smart.Data.Accessor.Attributes
{
    public sealed class ExecuteScalarReader : LoaderMethodAttribute
    {
        public ExecuteScalarReader()
            : base(MethodType.ExecuteReader)
        {
        }
    }
}
