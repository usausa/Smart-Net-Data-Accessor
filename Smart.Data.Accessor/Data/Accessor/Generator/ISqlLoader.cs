namespace Smart.Data.Accessor.Generator
{
    using System.Reflection;

    public interface ISqlLoader
    {
        string Load(MethodInfo mi);
    }
}
