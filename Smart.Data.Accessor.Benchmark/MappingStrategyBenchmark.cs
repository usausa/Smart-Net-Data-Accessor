namespace Smart.Data.Accessor.Benchmark;

using System.Data;
using System.Data.Common;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

using Dapper;

using Smart.Mock.Data;

// PoC: 行マッピング戦略の比較。F18 採用判断に使った履歴的ベンチで、各手書きメソッドが戦略を再現する。
//
//  * Manual straight-line / Current-style：旧方式(GetOrdinal 直線展開)の再現。全列そろう場合のみ動作。
//  * ReaderDriven：reader の列を走査し「列→プロパティ」plan を stackalloc に構築、行ループは plan の
//          switch dispatch(不採用案。per-row の間接ジャンプで全一致 2.3 倍劣化)。
//  * PropertyGuard：直線展開＋存在ガード(採用案 F18 の原型)。
//  * Current (generated)：**生成コードの現状**。F18 実装後は PropertyGuard 系(列名照合・欠落 -1・存在ガード)
//          であり旧 GetOrdinal 方式ではない。部分列(Subset)でも throw せず動作する。
//
// シナリオ：全一致(10プロパティ/10列) と 部分列(10プロパティ/2列)。class(settable) と record(ctor)。
// 旧方式ベースラインの実測値は __docs/benchmark-results.md(2026-07 マッピング戦略 PoC)の記録を参照。
//
// Run: dotnet run -c Release --project Smart.Data.Accessor.Benchmark -- --filter *MappingStrategyBenchmark*
public class MappingConfig : ManualConfig
{
    public MappingConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddColumn(StatisticColumn.Mean, StatisticColumn.Error, StatisticColumn.StdDev, StatisticColumn.P90);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddJob(Job.MediumRun);
    }
}

#pragma warning disable CA1001
[Config(typeof(MappingConfig))]
public class MappingStrategyBenchmark
{
    private const int RowCount = 100;
    private const string WideSql = "SELECT Id, Name, Age, Score, Active, Status, Description, Category, Tag, Weight FROM BenchData";
    private const string SubsetSql = "SELECT Id, Name FROM BenchData";

    // Mock は CommandText を無視し、列はモックの列定義で決まる(テキストは cosmetic)。CommandText には
    // この定数を直接代入して CA2100(非定数 SQL)を避ける。Dapper 呼び出しには上記の記述用 SQL を渡す。
    private const string CommandTextConst = "SELECT * FROM BenchData";

    private MockRepeatDbConnection mockWide = default!;
    private MockRepeatDbConnection mockSubset = default!;
    private BenchmarkAccessor accessor = default!;

    [GlobalSetup]
    public void Setup()
    {
#pragma warning disable CA2000
        mockWide = new MockRepeatDbConnection(new MockDataReader(
            [
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Name"),
                new MockColumn(typeof(int), "Age"),
                new MockColumn(typeof(double), "Score"),
                new MockColumn(typeof(bool), "Active"),
                new MockColumn(typeof(int), "Status"),
                new MockColumn(typeof(string), "Description"),
                new MockColumn(typeof(int), "Category"),
                new MockColumn(typeof(string), "Tag"),
                new MockColumn(typeof(double), "Weight")
            ],
            Enumerable.Range(1, RowCount).Select(static x => new object[]
            {
                (long)x,
                $"Name-{x}",
                x % 80,
                x * 1.5,
                (x % 2) == 0,
                x % 4,
                $"Description-{x}",
                x % 8,
                $"Tag-{x}",
                x * 0.25
            })));

        mockSubset = new MockRepeatDbConnection(new MockDataReader(
            [
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Name")
            ],
            Enumerable.Range(1, RowCount).Select(static x => new object[]
            {
                (long)x,
                $"Name-{x}"
            })));
#pragma warning restore CA2000

        accessor = new BenchmarkAccessor();

        // 正当性の簡易確認(誤マッピングを timing 前に検出する)。
        VerifyOrThrow();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        mockWide.Dispose();
        mockSubset.Dispose();
    }

    // -----------------------------------------------------------------
    // Full match: class, 10 properties / 10 columns
    // -----------------------------------------------------------------

    [Benchmark(Description = "Full/class: Current (generated)")]
    public IReadOnlyList<BenchWideRow> FullClassCurrent() => accessor.QueryWide(mockWide);

    [Benchmark(Description = "Full/class: Manual straight-line")]
    public List<BenchWideRow> FullClassManual() => ManualWide(mockWide);

    [Benchmark(Description = "Full/class: Dapper")]
    public List<BenchWideRow> FullClassDapper() => mockWide.Query<BenchWideRow>(WideSql).AsList();

    [Benchmark(Description = "Full/class: ReaderDriven (SingleResult)")]
    public List<BenchWideRow> FullClassReaderDriven() => ReaderDrivenWide(mockWide, CommandBehavior.SingleResult);

    [Benchmark(Description = "Full/class: ReaderDriven (SequentialAccess)")]
    public List<BenchWideRow> FullClassReaderDrivenSeq() => ReaderDrivenWide(mockWide, CommandBehavior.SequentialAccess);

    [Benchmark(Description = "Full/class: PropertyGuard (straight-line + guard)")]
    public List<BenchWideRow> FullClassPropertyGuard() => PropertyGuardWide(mockWide);

    // -----------------------------------------------------------------
    // Full match: record, 10 params / 10 columns
    // -----------------------------------------------------------------

    [Benchmark(Description = "Full/record: Current-style straight-line")]
    public List<BenchWideRecord> FullRecordCurrent() => CurrentWideRecord(mockWide);

    [Benchmark(Description = "Full/record: Dapper")]
    public List<BenchWideRecord> FullRecordDapper() => mockWide.Query<BenchWideRecord>(WideSql).AsList();

    [Benchmark(Description = "Full/record: ReaderDriven (SequentialAccess)")]
    public List<BenchWideRecord> FullRecordReaderDriven() => ReaderDrivenWideRecord(mockWide, CommandBehavior.SequentialAccess);

    [Benchmark(Description = "Full/record: PropertyGuard (straight-line + guard)")]
    public List<BenchWideRecord> FullRecordPropertyGuard() => PropertyGuardWideRecord(mockWide);

    // -----------------------------------------------------------------
    // Subset: class, 10 properties / 2 columns returned (Current throws -> excluded)
    // -----------------------------------------------------------------

    [Benchmark(Description = "Subset/class: Current (generated)")]
    public IReadOnlyList<BenchWideRow> SubsetClassGenerated() => accessor.QueryWide(mockSubset);

    [Benchmark(Description = "Subset/class: Manual minimal")]
    public List<BenchWideRow> SubsetClassManual() => ManualSubset(mockSubset);

    [Benchmark(Description = "Subset/class: Dapper")]
    public List<BenchWideRow> SubsetClassDapper() => mockSubset.Query<BenchWideRow>(SubsetSql).AsList();

    [Benchmark(Description = "Subset/class: ReaderDriven (SequentialAccess)")]
    public List<BenchWideRow> SubsetClassReaderDriven() => ReaderDrivenWide(mockSubset, CommandBehavior.SequentialAccess);

    [Benchmark(Description = "Subset/class: PropertyGuard (straight-line + guard)")]
    public List<BenchWideRow> SubsetClassPropertyGuard() => PropertyGuardWide(mockSubset);

    // -----------------------------------------------------------------
    // Mapping implementations (hand-written to mirror what the generator would emit)
    // -----------------------------------------------------------------

    // "列名 -> プロパティ番号" 対応(無関係列は -1)。プロパティ名はコンパイル時定数なので generator が switch を emit できる。
    private static int MapWide(string name) => name switch
    {
        "Id" => 0,
        "Name" => 1,
        "Age" => 2,
        "Score" => 3,
        "Active" => 4,
        "Status" => 5,
        "Description" => 6,
        "Category" => 7,
        "Tag" => 8,
        "Weight" => 9,
        _ => -1
    };

    // プロパティ主導＋存在ガード(class)：序数はローカル(欠落は -1)。行ループは現行同様の straight-line で、
    // 各プロパティに `ord < 0 ? default : read` のガードを1個足すだけ(switch/plan 配列なし)。プロパティ宣言順読み。
    private static List<BenchWideRow> PropertyGuardWide(DbConnection con)
    {
        var list = new List<BenchWideRow>();
        using var cmd = con.CreateCommand();
        cmd.CommandText = CommandTextConst;
        using var reader = cmd.ExecuteReader(CommandBehavior.SingleResult);
        if (reader.Read())
        {
            int oId = -1, oName = -1, oAge = -1, oScore = -1, oActive = -1,
                oStatus = -1, oDescription = -1, oCategory = -1, oTag = -1, oWeight = -1;
            var fieldCount = reader.FieldCount;
            for (var i = 0; i < fieldCount; i++)
            {
                switch (reader.GetName(i))
                {
                    case "Id": oId = i; break;
                    case "Name": oName = i; break;
                    case "Age": oAge = i; break;
                    case "Score": oScore = i; break;
                    case "Active": oActive = i; break;
                    case "Status": oStatus = i; break;
                    case "Description": oDescription = i; break;
                    case "Category": oCategory = i; break;
                    case "Tag": oTag = i; break;
                    case "Weight": oWeight = i; break;
                }
            }
            do
            {
                // 無い列は「default を設定」ではなく「設定しない」——プロパティ初期化子/既定を上書きしない。
                var obj = new BenchWideRow();
#pragma warning disable SA1503 // Braces not omitted — compact guarded assignment for readability
                if (oId >= 0) obj.Id = reader.GetInt64(oId);
                if (oName >= 0) obj.Name = reader.GetString(oName);
                if (oAge >= 0) obj.Age = reader.GetInt32(oAge);
                if (oScore >= 0) obj.Score = reader.GetDouble(oScore);
                if (oActive >= 0) obj.Active = reader.GetBoolean(oActive);
                if (oStatus >= 0) obj.Status = reader.GetInt32(oStatus);
                if (oDescription >= 0) obj.Description = reader.GetString(oDescription);
                if (oCategory >= 0) obj.Category = reader.GetInt32(oCategory);
                if (oTag >= 0) obj.Tag = reader.GetString(oTag);
                if (oWeight >= 0) obj.Weight = reader.GetDouble(oWeight);
#pragma warning restore SA1503
                list.Add(obj);
            }
            while (reader.Read());
        }
        return list;
    }

    // プロパティ主導＋存在ガード(record)：ガード式をそのまま ctor 引数に。ローカル不要。
    private static List<BenchWideRecord> PropertyGuardWideRecord(DbConnection con)
    {
        var list = new List<BenchWideRecord>();
        using var cmd = con.CreateCommand();
        cmd.CommandText = CommandTextConst;
        using var reader = cmd.ExecuteReader(CommandBehavior.SingleResult);
        if (reader.Read())
        {
            int oId = -1, oName = -1, oAge = -1, oScore = -1, oActive = -1,
                oStatus = -1, oDescription = -1, oCategory = -1, oTag = -1, oWeight = -1;
            var fieldCount = reader.FieldCount;
            for (var i = 0; i < fieldCount; i++)
            {
                switch (reader.GetName(i))
                {
                    case "Id": oId = i; break;
                    case "Name": oName = i; break;
                    case "Age": oAge = i; break;
                    case "Score": oScore = i; break;
                    case "Active": oActive = i; break;
                    case "Status": oStatus = i; break;
                    case "Description": oDescription = i; break;
                    case "Category": oCategory = i; break;
                    case "Tag": oTag = i; break;
                    case "Weight": oWeight = i; break;
                }
            }
            do
            {
                list.Add(new BenchWideRecord(
                    oId < 0 ? default : reader.GetInt64(oId),
                    oName < 0 ? string.Empty : reader.GetString(oName),
                    oAge < 0 ? default : reader.GetInt32(oAge),
                    oScore < 0 ? default : reader.GetDouble(oScore),
                    oActive >= 0 && reader.GetBoolean(oActive),
                    oStatus < 0 ? default : reader.GetInt32(oStatus),
                    oDescription < 0 ? string.Empty : reader.GetString(oDescription),
                    oCategory < 0 ? default : reader.GetInt32(oCategory),
                    oTag < 0 ? string.Empty : reader.GetString(oTag),
                    oWeight < 0 ? default : reader.GetDouble(oWeight)));
            }
            while (reader.Read());
        }
        return list;
    }

    // 新方式(class)：plan を stackalloc し、行ループは plan の switch dispatch。reader の列数分だけ処理。
    private static List<BenchWideRow> ReaderDrivenWide(DbConnection con, CommandBehavior behavior)
    {
        var list = new List<BenchWideRow>();
        using var cmd = con.CreateCommand();
        cmd.CommandText = CommandTextConst;
        using var reader = cmd.ExecuteReader(behavior);
        if (reader.Read())
        {
            var fieldCount = reader.FieldCount;
            // PoC は FieldCount が小さい(2/10)ため stackalloc 固定。本番は FieldCount 大で ArrayPool へ分岐する。
            Span<int> plan = stackalloc int[fieldCount];
            for (var i = 0; i < fieldCount; i++)
            {
                plan[i] = MapWide(reader.GetName(i));
            }
            do
            {
                var obj = new BenchWideRow();
                for (var i = 0; i < fieldCount; i++)
                {
                    switch (plan[i])
                    {
                        case 0: obj.Id = reader.GetInt64(i); break;
                        case 1: obj.Name = reader.GetString(i); break;
                        case 2: obj.Age = reader.GetInt32(i); break;
                        case 3: obj.Score = reader.GetDouble(i); break;
                        case 4: obj.Active = reader.GetBoolean(i); break;
                        case 5: obj.Status = reader.GetInt32(i); break;
                        case 6: obj.Description = reader.GetString(i); break;
                        case 7: obj.Category = reader.GetInt32(i); break;
                        case 8: obj.Tag = reader.GetString(i); break;
                        case 9: obj.Weight = reader.GetDouble(i); break;
                    }
                }
                list.Add(obj);
            }
            while (reader.Read());
        }
        return list;
    }

    // 新方式(record)：欠落引数は default のまま、存在列だけローカルへ読み、最後に construct。読み取りは reader の列数分。
    private static List<BenchWideRecord> ReaderDrivenWideRecord(DbConnection con, CommandBehavior behavior)
    {
        var list = new List<BenchWideRecord>();
        using var cmd = con.CreateCommand();
        cmd.CommandText = CommandTextConst;
        using var reader = cmd.ExecuteReader(behavior);
        if (reader.Read())
        {
            var fieldCount = reader.FieldCount;
            // PoC は FieldCount が小さい(2/10)ため stackalloc 固定。本番は FieldCount 大で ArrayPool へ分岐する。
            Span<int> plan = stackalloc int[fieldCount];
            for (var i = 0; i < fieldCount; i++)
            {
                plan[i] = MapWide(reader.GetName(i));
            }
            do
            {
                long id = default;
                var name = string.Empty;
                int age = default;
                double score = default;
                var active = false;
                int status = default;
                var description = string.Empty;
                int category = default;
                var tag = string.Empty;
                double weight = default;
                for (var i = 0; i < fieldCount; i++)
                {
                    switch (plan[i])
                    {
                        case 0: id = reader.GetInt64(i); break;
                        case 1: name = reader.GetString(i); break;
                        case 2: age = reader.GetInt32(i); break;
                        case 3: score = reader.GetDouble(i); break;
                        case 4: active = reader.GetBoolean(i); break;
                        case 5: status = reader.GetInt32(i); break;
                        case 6: description = reader.GetString(i); break;
                        case 7: category = reader.GetInt32(i); break;
                        case 8: tag = reader.GetString(i); break;
                        case 9: weight = reader.GetDouble(i); break;
                    }
                }
                list.Add(new BenchWideRecord(id, name, age, score, active, status, description, category, tag, weight));
            }
            while (reader.Read());
        }
        return list;
    }

    // 現行相当(record)：GetOrdinal を 1 回、直線で全 ctor 引数を読む。
    private static List<BenchWideRecord> CurrentWideRecord(DbConnection con)
    {
        var list = new List<BenchWideRecord>();
        using var cmd = con.CreateCommand();
        cmd.CommandText = CommandTextConst;
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

    // 現行相当(class・全一致の手書きベースライン)。
    private static List<BenchWideRow> ManualWide(DbConnection con)
    {
        var list = new List<BenchWideRow>();
        using var cmd = con.CreateCommand();
        cmd.CommandText = CommandTextConst;
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

    // 部分列の手書き最小(返る 2 列だけを直接読む理論下限)。
    private static List<BenchWideRow> ManualSubset(DbConnection con)
    {
        var list = new List<BenchWideRow>();
        using var cmd = con.CreateCommand();
        cmd.CommandText = CommandTextConst;
        using var reader = cmd.ExecuteReader(CommandBehavior.SingleResult);
        if (reader.Read())
        {
            var oId = reader.GetOrdinal("Id");
            var oName = reader.GetOrdinal("Name");
            do
            {
                list.Add(new BenchWideRow
                {
                    Id = reader.GetInt64(oId),
                    Name = reader.GetString(oName)
                });
            }
            while (reader.Read());
        }
        return list;
    }

    private void VerifyOrThrow()
    {
        var full = ReaderDrivenWide(mockWide, CommandBehavior.SequentialAccess);
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if ((full.Count != RowCount) || (full[0].Id != 1L) || (full[0].Name != "Name-1") || (full[^1].Weight != RowCount * 0.25))
        {
            throw new InvalidOperationException("ReaderDrivenWide produced incorrect results.");
        }

        var rec = ReaderDrivenWideRecord(mockWide, CommandBehavior.SequentialAccess);
        if ((rec.Count != RowCount) || (rec[0].Id != 1L) || (rec[0].Tag != "Tag-1"))
        {
            throw new InvalidOperationException("ReaderDrivenWideRecord produced incorrect results.");
        }

        var subset = ReaderDrivenWide(mockSubset, CommandBehavior.SequentialAccess);
        // 部分列：Id/Name は設定され、返らない列(例：Age)は既定値のまま。
        if ((subset.Count != RowCount) || (subset[0].Id != 1L) || (subset[0].Name != "Name-1") || (subset[0].Age != 0))
        {
            throw new InvalidOperationException("ReaderDrivenWide (subset) produced incorrect results.");
        }
    }
}
#pragma warning restore CA1001
