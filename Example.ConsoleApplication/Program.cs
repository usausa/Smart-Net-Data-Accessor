namespace Example.ConsoleApplication
{
    using System;
    using System.IO;

    using Example.ConsoleApplication.Dao;
    using Example.ConsoleApplication.Models;

    using Microsoft.Data.Sqlite;

    using Smart.Data;
    using Smart.Data.Accessor;
    using Smart.Data.Accessor.Engine;

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

            var engine = new ExecuteEngineConfig()
                .ConfigureComponents(c =>
                {
                    c.Add<IDbProvider>(new DelegateDbProvider(() => new SqliteConnection(ConnectionString)));
                })
                .ToEngine();
            var factory = new DataAccessorFactory(engine);

            var dao = factory.Create<IExampleDao>();

            dao.Create();

            dao.Insert(new DataEntity { Id = 1L, Name = "Data-1", Type = "A" });
            dao.Insert(new DataEntity { Id = 2L, Name = "Data-2", Type = "B" });
            dao.Insert(new DataEntity { Id = 3L, Name = "Data-3", Type = "A" });

            var typeA = dao.QueryDataList("A");
            Console.WriteLine(typeA.Count);

            var all = dao.QueryDataList();
            Console.WriteLine(all.Count);
        }
    }
}
