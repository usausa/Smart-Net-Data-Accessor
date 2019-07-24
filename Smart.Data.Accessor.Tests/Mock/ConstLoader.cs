namespace Smart.Mock
{
    using System.Reflection;

    using Smart.Data.Accessor.Loaders;

    public sealed class ConstLoader : ISqlLoader
    {
        private readonly string sql;

        public ConstLoader(string sql)
        {
            this.sql = sql;
        }

        public string Load(MethodInfo mi) => sql;
    }
}
