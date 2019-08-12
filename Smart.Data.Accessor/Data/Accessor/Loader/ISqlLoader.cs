namespace Smart.Data.Accessor.Loader
{
    using System.Reflection;

    public interface ISqlLoader
    {
        string Load(MethodInfo mi);
    }
}
