namespace Smart.Mock;

using System.Data.Common;

using Smart.Data.Mapper;

public static class DbConnectionExtensions
{
    public static DbConnection SetupDataTable(this DbConnection con)
    {
        con.Execute("CREATE TABLE IF NOT EXISTS Data (Id int PRIMARY KEY, Name text)");
        return con;
    }

    public static DbConnection SetupMultiKeyTable(this DbConnection con)
    {
        con.Execute("CREATE TABLE IF NOT EXISTS MultiKey (Key1 int, Key2 int, Type text, Name text, PRIMARY KEY (Key1, Key2))");
        return con;
    }

    public static DbConnection InsertData(this DbConnection con, DataEntity entity)
    {
        con.Execute("INSERT INTO Data (Id, Name) VALUES (@Id, @Name)", entity);
        return con;
    }

    public static DataEntity? QueryData(this DbConnection con, long id)
    {
        return con.QueryFirstOrDefault<DataEntity>("SELECT * FROM Data WHERE Id = @Id", new { Id = id });
    }

    public static DbConnection InsertMultiKey(this DbConnection con, MultiKeyEntity entity)
    {
        con.Execute("INSERT INTO MultiKey (Key1, Key2, Type, Name) VALUES (@Key1, @Key2, @Type, @Name)", entity);
        return con;
    }

    public static MultiKeyEntity? QueryMultiKey(this DbConnection con, long key1, long key2)
    {
        return con.QueryFirstOrDefault<MultiKeyEntity>("SELECT * FROM MultiKey WHERE Key1 = @Key1 AND Key2 = @Key2", new { Key1 = key1, Key2 = key2 });
    }
}
