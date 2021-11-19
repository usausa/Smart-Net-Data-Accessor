namespace Smart.Data.Accessor.Attributes;

public sealed class ExecuteAttribute : LoaderMethodAttribute
{
    public ExecuteAttribute()
        : base(MethodType.Execute)
    {
    }
}
