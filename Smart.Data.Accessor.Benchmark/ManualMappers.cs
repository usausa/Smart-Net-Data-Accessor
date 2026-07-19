namespace Smart.Data.Accessor.Benchmark;

using System.Data;
using System.Data.Common;

// 手書き(直書き)ベースライン実装(DapperComparisonBenchmark の baseline)。
// Mock 接続は CommandText を無視し、列はモックの列定義で決まる(テキストは cosmetic)。
internal static class ManualMappers
{
    public const string CommandText = "SELECT * FROM BenchData";

    public static List<BenchIntRow> QueryInt(DbConnection con)
    {
        var list = new List<BenchIntRow>();
        using var cmd = con.CreateCommand();
        cmd.CommandText = CommandText;
        using var reader = cmd.ExecuteReader(CommandBehavior.SingleResult);
        if (reader.Read())
        {
            var oId = reader.GetOrdinal("Id");
            do
            {
                list.Add(new BenchIntRow { Id = reader.GetInt64(oId) });
            }
            while (reader.Read());
        }
        return list;
    }

    public static List<BenchWideRow> QueryWide(DbConnection con)
    {
        var list = new List<BenchWideRow>();
        using var cmd = con.CreateCommand();
        cmd.CommandText = CommandText;
        using var reader = cmd.ExecuteReader(CommandBehavior.SingleResult);
        if (reader.Read())
        {
            var oId = reader.GetOrdinal("Id");
            var oName = reader.GetOrdinal("Name");
            var oAge = reader.GetOrdinal("Age");
            var oScore = reader.GetOrdinal("Score");
            var oActive = reader.GetOrdinal("Active");
            var oStatus = reader.GetOrdinal("Status");
            var oDescription = reader.GetOrdinal("Description");
            var oCategory = reader.GetOrdinal("Category");
            var oTag = reader.GetOrdinal("Tag");
            var oWeight = reader.GetOrdinal("Weight");
            do
            {
                list.Add(new BenchWideRow
                {
                    Id = reader.GetInt64(oId),
                    Name = reader.GetString(oName),
                    Age = reader.GetInt32(oAge),
                    Score = reader.GetDouble(oScore),
                    Active = reader.GetBoolean(oActive),
                    Status = reader.GetInt32(oStatus),
                    Description = reader.GetString(oDescription),
                    Category = reader.GetInt32(oCategory),
                    Tag = reader.GetString(oTag),
                    Weight = reader.GetDouble(oWeight)
                });
            }
            while (reader.Read());
        }
        return list;
    }

    public static List<BenchExtraWideRow> QueryExtraWide(DbConnection con)
    {
        var list = new List<BenchExtraWideRow>();
        using var cmd = con.CreateCommand();
        cmd.CommandText = CommandText;
        using var reader = cmd.ExecuteReader(CommandBehavior.SingleResult);
        if (reader.Read())
        {
            var oId = reader.GetOrdinal("Id");
            var oName = reader.GetOrdinal("Name");
            var oAge = reader.GetOrdinal("Age");
            var oScore = reader.GetOrdinal("Score");
            var oActive = reader.GetOrdinal("Active");
            var oStatus = reader.GetOrdinal("Status");
            var oDescription = reader.GetOrdinal("Description");
            var oCategory = reader.GetOrdinal("Category");
            var oTag = reader.GetOrdinal("Tag");
            var oWeight = reader.GetOrdinal("Weight");
            var oOwner = reader.GetOrdinal("Owner");
            var oTeam = reader.GetOrdinal("Team");
            var oLevel = reader.GetOrdinal("Level");
            var oPosition = reader.GetOrdinal("Position");
            var oVersion = reader.GetOrdinal("Version");
            var oCity = reader.GetOrdinal("City");
            var oState = reader.GetOrdinal("State");
            var oCountry = reader.GetOrdinal("Country");
            var oNote = reader.GetOrdinal("Note");
            var oMemo = reader.GetOrdinal("Memo");
            do
            {
                list.Add(new BenchExtraWideRow
                {
                    Id = reader.GetInt64(oId),
                    Name = reader.GetString(oName),
                    Age = reader.GetInt32(oAge),
                    Score = reader.GetDouble(oScore),
                    Active = reader.GetBoolean(oActive),
                    Status = reader.GetInt32(oStatus),
                    Description = reader.GetString(oDescription),
                    Category = reader.GetInt32(oCategory),
                    Tag = reader.GetString(oTag),
                    Weight = reader.GetDouble(oWeight),
                    Owner = reader.GetInt32(oOwner),
                    Team = reader.GetInt32(oTeam),
                    Level = reader.GetInt32(oLevel),
                    Position = reader.GetInt32(oPosition),
                    Version = reader.GetInt32(oVersion),
                    City = reader.GetString(oCity),
                    State = reader.GetString(oState),
                    Country = reader.GetString(oCountry),
                    Note = reader.GetString(oNote),
                    Memo = reader.GetString(oMemo)
                });
            }
            while (reader.Read());
        }
        return list;
    }

    public static List<BenchWideRecord> QueryWideRecord(DbConnection con)
    {
        var list = new List<BenchWideRecord>();
        using var cmd = con.CreateCommand();
        cmd.CommandText = CommandText;
        using var reader = cmd.ExecuteReader(CommandBehavior.SingleResult);
        if (reader.Read())
        {
            var oId = reader.GetOrdinal("Id");
            var oName = reader.GetOrdinal("Name");
            var oAge = reader.GetOrdinal("Age");
            var oScore = reader.GetOrdinal("Score");
            var oActive = reader.GetOrdinal("Active");
            var oStatus = reader.GetOrdinal("Status");
            var oDescription = reader.GetOrdinal("Description");
            var oCategory = reader.GetOrdinal("Category");
            var oTag = reader.GetOrdinal("Tag");
            var oWeight = reader.GetOrdinal("Weight");
            do
            {
                list.Add(new BenchWideRecord(
                    reader.GetInt64(oId),
                    reader.GetString(oName),
                    reader.GetInt32(oAge),
                    reader.GetDouble(oScore),
                    reader.GetBoolean(oActive),
                    reader.GetInt32(oStatus),
                    reader.GetString(oDescription),
                    reader.GetInt32(oCategory),
                    reader.GetString(oTag),
                    reader.GetDouble(oWeight)));
            }
            while (reader.Read());
        }
        return list;
    }

    public static List<BenchEnumRow> QueryEnum(DbConnection con)
    {
        var list = new List<BenchEnumRow>();
        using var cmd = con.CreateCommand();
        cmd.CommandText = CommandText;
        using var reader = cmd.ExecuteReader(CommandBehavior.SingleResult);
        if (reader.Read())
        {
            var oId = reader.GetOrdinal("Id");
            var oName = reader.GetOrdinal("Name");
            var oStatus = reader.GetOrdinal("Status");
            do
            {
                list.Add(new BenchEnumRow
                {
                    Id = reader.GetInt64(oId),
                    Name = reader.GetString(oName),
                    Status = (BenchStatus)reader.GetInt32(oStatus)
                });
            }
            while (reader.Read());
        }
        return list;
    }

    // 部分列の手書き最小(返る 2 列だけを直接読む理論下限)。
    public static List<BenchWideRow> QuerySubset(DbConnection con)
    {
        var list = new List<BenchWideRow>();
        using var cmd = con.CreateCommand();
        cmd.CommandText = CommandText;
        using var reader = cmd.ExecuteReader(CommandBehavior.SingleResult);
        if (reader.Read())
        {
            var oId = reader.GetOrdinal("Id");
            var oName = reader.GetOrdinal("Name");
            do
            {
                list.Add(new BenchWideRow { Id = reader.GetInt64(oId), Name = reader.GetString(oName) });
            }
            while (reader.Read());
        }
        return list;
    }
}
