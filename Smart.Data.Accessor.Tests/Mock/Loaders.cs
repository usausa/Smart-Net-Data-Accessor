namespace Smart.Mock;

using System.Reflection;

using Smart.Data.Accessor.Generator;

public sealed class ConstLoader : ISqlLoader
{
    private readonly string sql;

    public ConstLoader(string sql)
    {
        this.sql = sql;
    }

    public string Load(MethodInfo mi) => sql;
}

public sealed class MapLoader : ISqlLoader
{
    private readonly Dictionary<string, string> map;

    public MapLoader(Dictionary<string, string> map)
    {
        this.map = map;
    }

    public string Load(MethodInfo mi) => map[mi.Name];
}
