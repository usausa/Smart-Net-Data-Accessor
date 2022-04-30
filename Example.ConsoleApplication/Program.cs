namespace Example.ConsoleApplication;

using Example.ConsoleApplication.Accessor;
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

        var accessor = factory.Create<IExampleAccessor>();

        accessor.Create();

        accessor.Insert(new DataEntity { Id = 1L, Name = "Data-1", Type = "A" });
        accessor.Insert(new DataEntity { Id = 2L, Name = "Data-2", Type = "B" });
        accessor.Insert(new DataEntity { Id = 3L, Name = "Data-3", Type = "A" });

        var typeA = accessor.QueryDataList("A");
        Console.WriteLine(typeA.Count);

        var all = accessor.QueryDataList();
        Console.WriteLine(all.Count);

        var ordered = accessor.QueryDataList(order: "Name DESC");
        Console.WriteLine(ordered[0].Name);
    }
}
