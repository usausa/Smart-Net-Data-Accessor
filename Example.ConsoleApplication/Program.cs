namespace Example.ConsoleApplication;

using Example.ConsoleApplication.Accessor;
using Example.ConsoleApplication.Models;

using Microsoft.Data.Sqlite;

internal static class Program
{
    public static int Main()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var accessor = new ExampleAccessor(connection);

        accessor.Create();

        accessor.Insert(new DataEntity { Name = "Alice", Type = 1 });
        accessor.Insert(new DataEntity { Name = "Bob", Type = 2 });
        accessor.Insert(new DataEntity { Name = "Carol", Type = 3 });

        var list = accessor.QueryDataList();
        foreach (var item in list)
        {
            System.Console.WriteLine($"Id={item.Id}, Name={item.Name}, Type={item.Type}");
        }

        return list.Count == 3 ? 0 : 1;
    }
}
