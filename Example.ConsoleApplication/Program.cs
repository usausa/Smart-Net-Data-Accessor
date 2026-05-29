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

        var type2 = accessor.QueryByType(2);
        System.Console.WriteLine($"QueryByType(2) -> {type2.Count} row(s)");
        foreach (var item in type2)
        {
            System.Console.WriteLine($"  Id={item.Id}, Name={item.Name}, Type={item.Type}");
        }

        // SelectBuilder
        var selectAll = accessor.SelectAll();
        System.Console.WriteLine($"SelectAll -> {selectAll.Count} row(s)");

        // UpdateBuilder
        var upd = new DataEntity { Id = selectAll[0].Id, Name = "Alice2", Type = 11 };
        var updated = accessor.UpdateEntity(upd);
        System.Console.WriteLine($"UpdateEntity -> {updated} row(s) updated");

        // DeleteBuilder
        var deleted = accessor.DeleteById(selectAll[2].Id);
        System.Console.WriteLine($"DeleteById -> {deleted} row(s) deleted");

        // CountBuilder + ExecuteScalar
        var count = accessor.CountAll();
        System.Console.WriteLine($"CountAll -> {count}");

        return list.Count == 3 && type2.Count == 1 && updated == 1 && deleted == 1 && count == 2 ? 0 : 1;
    }
}
