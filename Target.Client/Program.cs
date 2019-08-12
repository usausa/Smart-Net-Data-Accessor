namespace Target.Client
{
    using System.IO;

    using Microsoft.Data.Sqlite;

    using Smart.Data;
    using Smart.Data.Accessor;
    using Smart.Data.Accessor.Engine;
    using Smart.Data.Mapper;

    using Target.Dao;

    public static class Program
    {
        private const string FileName = "test.db";
        private const string ConnectionString = "Data Source=" + FileName;

        public static void Main()
        {
            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }

            using (var con = new SqliteConnection(ConnectionString))
            {
                con.Execute("CREATE TABLE IF NOT EXISTS Data (Id int PRIMARY KEY, Name text)");
                con.Execute("INSERT INTO Data (Id, Name) VALUES (1, 'data')");
            }

            var engine = new ExecuteEngineConfig()
                .ConfigureComponents(c =>
                {
                    c.Add<IDbProvider>(new DelegateDbProvider(() => new SqliteConnection(ConnectionString)));
                })
                .ToEngine();
            var factory = new DataAccessorFactory(engine);

            var dao = factory.Create<ISampleDao>();

            var entity1 = dao.QueryData(1);
            var entity2 = dao.QueryData(2);
        }
    }
}
