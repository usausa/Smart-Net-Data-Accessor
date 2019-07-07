namespace Smart.Data.Accessor.Loaders
{
    using System.Reflection;

    public interface ISqlLoader
    {
        string Load(MethodInfo mi);
    }
}
