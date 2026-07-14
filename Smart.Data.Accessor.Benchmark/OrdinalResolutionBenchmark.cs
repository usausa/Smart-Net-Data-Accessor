namespace Smart.Data.Accessor.Benchmark;

using System.Collections.Frozen;
using System.Data.Common;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;

using Smart.Mock.Data;

// PoC: __From の序数解決(列名→プロパティ序数の照合)だけを切り出した戦略比較。
//
//  * UpperSwitch     : 現行 emit。switch (reader.GetName(i).ToUpperInvariant()) — コンパイラ生成のハッシュ switch。
//                      Pascal 列名では ToUpperInvariant が列毎に割当(既に大文字なら同一インスタンスが返り割当なし)。
//  * Dictionary      : 事前構築の Dictionary<string,int>(OrdinalIgnoreCase) + stackalloc 序数表。ToUpper 不要＝割当ゼロ。
//  * FrozenDictionary: 同上の FrozenDictionary 版(読み取り最適化)。
//  * NormalizedHash  : Dapper.AOT 方式。小文字正規化しながら char 単位で FNV-1a ハッシュを計算(割当ゼロ)し、
//                      コンパイル時に事前計算した uint 定数の switch + when 句の NormalizedEquals で検証。
//
// 全戦略とも意味論は同一(大小無視・先勝ち・欠落 -1・全解決で早期 break)。列数 10/30/60、列名の
// ケーシング(Pascal/UPPER)を変えて計測する。Miss 系(別クラス)は不一致 60 列の SELECT * 相当。
//
// Run: dotnet run -c Release --project Smart.Data.Accessor.Benchmark -- --filter *OrdinalResolution*
#pragma warning disable CA1001, SA1107, SA1312, SA1501, SA1503 // mirrors generated-emit style verbatim
[Config(typeof(MappingConfig))]
public class OrdinalResolutionBenchmark
{
    [Params(10, 30, 60)]
    public int Columns { get; set; }

    [Params(false, true)]
    public bool UppercaseColumns { get; set; }

    private MockDataReader reader = default!;
    private Dictionary<string, int> dictionary = default!;
    private FrozenDictionary<string, int> frozen = default!;

    [GlobalSetup]
    public void Setup()
    {
        var columns = Enumerable.Range(0, Columns)
            .Select(x => new MockColumn(typeof(long), UppercaseColumns ? $"COLUMN{x}" : $"Column{x}"))
            .ToArray();
        reader = new MockDataReader(columns, new List<object[]>());

        dictionary = new Dictionary<string, int>(Columns, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Columns; i++)
        {
            dictionary[$"Column{i}"] = i;
        }
        frozen = dictionary.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    [GlobalCleanup]
    public void Cleanup() => reader.Dispose();

    [Benchmark(Description = "UpperSwitch (current emit)")]
    public int UpperSwitch() => Columns switch
    {
        10 => UpperSwitch10(reader),
        30 => UpperSwitch30(reader),
        _ => UpperSwitch60(reader),
    };

    [Benchmark(Description = "Dictionary(OrdinalIgnoreCase)")]
    public int Dictionary() => DictionaryResolve(reader, dictionary, Columns);

    [Benchmark(Description = "FrozenDictionary(OrdinalIgnoreCase)")]
    public int Frozen() => FrozenResolve(reader, frozen, Columns);

    [Benchmark(Description = "NormalizedHash switch (Dapper.AOT-style)")]
    public int NormalizedHashSwitch() => Columns switch
    {
        10 => HashSwitch10(reader),
        30 => HashSwitch30(reader),
        _ => HashSwitch60(reader),
    };

    // Dapper.AOT 方式のランタイムヘルパー相当(採用時は Smart.Data.Accessor.Helpers へ置く想定)。
    // 生成側はコンパイル時に同一アルゴリズムでハッシュ定数と小文字リテラルを事前計算する。
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint NormalizedHash(string name)
    {
        var hash = 2166136261u;
        foreach (var ch in name)
        {
            hash = (hash ^ char.ToLowerInvariant(ch)) * 16777619;
        }
        return hash;
    }

    internal static bool NormalizedEquals(string name, string normalized)
    {
        if (name.Length != normalized.Length)
        {
            return false;
        }
        for (var i = 0; i < name.Length; i++)
        {
            if (char.ToLowerInvariant(name[i]) != normalized[i])
            {
                return false;
            }
        }
        return true;
    }

    // Dictionary 戦略：emit 想定は「stackalloc 序数表を引いて struct を構築」。ここでは序数表の総和を返す
    // (struct 構築コストは全戦略共通のため省略)。
    internal static int DictionaryResolve(DbDataReader reader, Dictionary<string, int> map, int count)
    {
        Span<int> ordinals = stackalloc int[64];
        ordinals = ordinals[..count];
        ordinals.Fill(-1);
        var resolved = 0;
        var fieldCount = reader.FieldCount;
        for (var i = 0; i < fieldCount; i++)
        {
            if (map.TryGetValue(reader.GetName(i), out var index) && (ordinals[index] < 0))
            {
                ordinals[index] = i;
                resolved++;
                if (resolved == count)
                {
                    break;
                }
            }
        }
        var sum = 0;
        foreach (var ordinal in ordinals)
        {
            sum += ordinal;
        }
        return sum;
    }

    internal static int FrozenResolve(DbDataReader reader, FrozenDictionary<string, int> map, int count)
    {
        Span<int> ordinals = stackalloc int[64];
        ordinals = ordinals[..count];
        ordinals.Fill(-1);
        var resolved = 0;
        var fieldCount = reader.FieldCount;
        for (var i = 0; i < fieldCount; i++)
        {
            if (map.TryGetValue(reader.GetName(i), out var index) && (ordinals[index] < 0))
            {
                ordinals[index] = i;
                resolved++;
                if (resolved == count)
                {
                    break;
                }
            }
        }
        var sum = 0;
        foreach (var ordinal in ordinals)
        {
            sum += ordinal;
        }
        return sum;
    }

    // 現行 emit の忠実な再現(N=10)。
    internal static int UpperSwitch10(DbDataReader reader)
    {
        var __ord0 = -1;
        var __ord1 = -1;
        var __ord2 = -1;
        var __ord3 = -1;
        var __ord4 = -1;
        var __ord5 = -1;
        var __ord6 = -1;
        var __ord7 = -1;
        var __ord8 = -1;
        var __ord9 = -1;
        var __resolved = 0;
        var __fieldCount = reader.FieldCount;
        for (var __i = 0; __i < __fieldCount; __i++)
        {
            switch (reader.GetName(__i).ToUpperInvariant())
            {
                case "COLUMN0":
                    if (__ord0 < 0) { __ord0 = __i; __resolved++; }
                    break;
                case "COLUMN1":
                    if (__ord1 < 0) { __ord1 = __i; __resolved++; }
                    break;
                case "COLUMN2":
                    if (__ord2 < 0) { __ord2 = __i; __resolved++; }
                    break;
                case "COLUMN3":
                    if (__ord3 < 0) { __ord3 = __i; __resolved++; }
                    break;
                case "COLUMN4":
                    if (__ord4 < 0) { __ord4 = __i; __resolved++; }
                    break;
                case "COLUMN5":
                    if (__ord5 < 0) { __ord5 = __i; __resolved++; }
                    break;
                case "COLUMN6":
                    if (__ord6 < 0) { __ord6 = __i; __resolved++; }
                    break;
                case "COLUMN7":
                    if (__ord7 < 0) { __ord7 = __i; __resolved++; }
                    break;
                case "COLUMN8":
                    if (__ord8 < 0) { __ord8 = __i; __resolved++; }
                    break;
                case "COLUMN9":
                    if (__ord9 < 0) { __ord9 = __i; __resolved++; }
                    break;
            }
            if (__resolved == 10) break;
        }
        return __ord0 + __ord1 + __ord2 + __ord3 + __ord4 + __ord5 + __ord6 + __ord7 + __ord8 + __ord9;
    }

    // 現行 emit の忠実な再現(N=30)。
    internal static int UpperSwitch30(DbDataReader reader)
    {
        var __ord0 = -1;
        var __ord1 = -1;
        var __ord2 = -1;
        var __ord3 = -1;
        var __ord4 = -1;
        var __ord5 = -1;
        var __ord6 = -1;
        var __ord7 = -1;
        var __ord8 = -1;
        var __ord9 = -1;
        var __ord10 = -1;
        var __ord11 = -1;
        var __ord12 = -1;
        var __ord13 = -1;
        var __ord14 = -1;
        var __ord15 = -1;
        var __ord16 = -1;
        var __ord17 = -1;
        var __ord18 = -1;
        var __ord19 = -1;
        var __ord20 = -1;
        var __ord21 = -1;
        var __ord22 = -1;
        var __ord23 = -1;
        var __ord24 = -1;
        var __ord25 = -1;
        var __ord26 = -1;
        var __ord27 = -1;
        var __ord28 = -1;
        var __ord29 = -1;
        var __resolved = 0;
        var __fieldCount = reader.FieldCount;
        for (var __i = 0; __i < __fieldCount; __i++)
        {
            switch (reader.GetName(__i).ToUpperInvariant())
            {
                case "COLUMN0":
                    if (__ord0 < 0) { __ord0 = __i; __resolved++; }
                    break;
                case "COLUMN1":
                    if (__ord1 < 0) { __ord1 = __i; __resolved++; }
                    break;
                case "COLUMN2":
                    if (__ord2 < 0) { __ord2 = __i; __resolved++; }
                    break;
                case "COLUMN3":
                    if (__ord3 < 0) { __ord3 = __i; __resolved++; }
                    break;
                case "COLUMN4":
                    if (__ord4 < 0) { __ord4 = __i; __resolved++; }
                    break;
                case "COLUMN5":
                    if (__ord5 < 0) { __ord5 = __i; __resolved++; }
                    break;
                case "COLUMN6":
                    if (__ord6 < 0) { __ord6 = __i; __resolved++; }
                    break;
                case "COLUMN7":
                    if (__ord7 < 0) { __ord7 = __i; __resolved++; }
                    break;
                case "COLUMN8":
                    if (__ord8 < 0) { __ord8 = __i; __resolved++; }
                    break;
                case "COLUMN9":
                    if (__ord9 < 0) { __ord9 = __i; __resolved++; }
                    break;
                case "COLUMN10":
                    if (__ord10 < 0) { __ord10 = __i; __resolved++; }
                    break;
                case "COLUMN11":
                    if (__ord11 < 0) { __ord11 = __i; __resolved++; }
                    break;
                case "COLUMN12":
                    if (__ord12 < 0) { __ord12 = __i; __resolved++; }
                    break;
                case "COLUMN13":
                    if (__ord13 < 0) { __ord13 = __i; __resolved++; }
                    break;
                case "COLUMN14":
                    if (__ord14 < 0) { __ord14 = __i; __resolved++; }
                    break;
                case "COLUMN15":
                    if (__ord15 < 0) { __ord15 = __i; __resolved++; }
                    break;
                case "COLUMN16":
                    if (__ord16 < 0) { __ord16 = __i; __resolved++; }
                    break;
                case "COLUMN17":
                    if (__ord17 < 0) { __ord17 = __i; __resolved++; }
                    break;
                case "COLUMN18":
                    if (__ord18 < 0) { __ord18 = __i; __resolved++; }
                    break;
                case "COLUMN19":
                    if (__ord19 < 0) { __ord19 = __i; __resolved++; }
                    break;
                case "COLUMN20":
                    if (__ord20 < 0) { __ord20 = __i; __resolved++; }
                    break;
                case "COLUMN21":
                    if (__ord21 < 0) { __ord21 = __i; __resolved++; }
                    break;
                case "COLUMN22":
                    if (__ord22 < 0) { __ord22 = __i; __resolved++; }
                    break;
                case "COLUMN23":
                    if (__ord23 < 0) { __ord23 = __i; __resolved++; }
                    break;
                case "COLUMN24":
                    if (__ord24 < 0) { __ord24 = __i; __resolved++; }
                    break;
                case "COLUMN25":
                    if (__ord25 < 0) { __ord25 = __i; __resolved++; }
                    break;
                case "COLUMN26":
                    if (__ord26 < 0) { __ord26 = __i; __resolved++; }
                    break;
                case "COLUMN27":
                    if (__ord27 < 0) { __ord27 = __i; __resolved++; }
                    break;
                case "COLUMN28":
                    if (__ord28 < 0) { __ord28 = __i; __resolved++; }
                    break;
                case "COLUMN29":
                    if (__ord29 < 0) { __ord29 = __i; __resolved++; }
                    break;
            }
            if (__resolved == 30) break;
        }
        return __ord0 + __ord1 + __ord2 + __ord3 + __ord4 + __ord5 + __ord6 + __ord7 + __ord8 + __ord9 + __ord10 + __ord11 + __ord12 + __ord13 + __ord14 + __ord15 + __ord16 + __ord17 + __ord18 + __ord19 + __ord20 + __ord21 + __ord22 + __ord23 + __ord24 + __ord25 + __ord26 + __ord27 + __ord28 + __ord29;
    }

    // 現行 emit の忠実な再現(N=60)。
    internal static int UpperSwitch60(DbDataReader reader)
    {
        var __ord0 = -1;
        var __ord1 = -1;
        var __ord2 = -1;
        var __ord3 = -1;
        var __ord4 = -1;
        var __ord5 = -1;
        var __ord6 = -1;
        var __ord7 = -1;
        var __ord8 = -1;
        var __ord9 = -1;
        var __ord10 = -1;
        var __ord11 = -1;
        var __ord12 = -1;
        var __ord13 = -1;
        var __ord14 = -1;
        var __ord15 = -1;
        var __ord16 = -1;
        var __ord17 = -1;
        var __ord18 = -1;
        var __ord19 = -1;
        var __ord20 = -1;
        var __ord21 = -1;
        var __ord22 = -1;
        var __ord23 = -1;
        var __ord24 = -1;
        var __ord25 = -1;
        var __ord26 = -1;
        var __ord27 = -1;
        var __ord28 = -1;
        var __ord29 = -1;
        var __ord30 = -1;
        var __ord31 = -1;
        var __ord32 = -1;
        var __ord33 = -1;
        var __ord34 = -1;
        var __ord35 = -1;
        var __ord36 = -1;
        var __ord37 = -1;
        var __ord38 = -1;
        var __ord39 = -1;
        var __ord40 = -1;
        var __ord41 = -1;
        var __ord42 = -1;
        var __ord43 = -1;
        var __ord44 = -1;
        var __ord45 = -1;
        var __ord46 = -1;
        var __ord47 = -1;
        var __ord48 = -1;
        var __ord49 = -1;
        var __ord50 = -1;
        var __ord51 = -1;
        var __ord52 = -1;
        var __ord53 = -1;
        var __ord54 = -1;
        var __ord55 = -1;
        var __ord56 = -1;
        var __ord57 = -1;
        var __ord58 = -1;
        var __ord59 = -1;
        var __resolved = 0;
        var __fieldCount = reader.FieldCount;
        for (var __i = 0; __i < __fieldCount; __i++)
        {
            switch (reader.GetName(__i).ToUpperInvariant())
            {
                case "COLUMN0":
                    if (__ord0 < 0) { __ord0 = __i; __resolved++; }
                    break;
                case "COLUMN1":
                    if (__ord1 < 0) { __ord1 = __i; __resolved++; }
                    break;
                case "COLUMN2":
                    if (__ord2 < 0) { __ord2 = __i; __resolved++; }
                    break;
                case "COLUMN3":
                    if (__ord3 < 0) { __ord3 = __i; __resolved++; }
                    break;
                case "COLUMN4":
                    if (__ord4 < 0) { __ord4 = __i; __resolved++; }
                    break;
                case "COLUMN5":
                    if (__ord5 < 0) { __ord5 = __i; __resolved++; }
                    break;
                case "COLUMN6":
                    if (__ord6 < 0) { __ord6 = __i; __resolved++; }
                    break;
                case "COLUMN7":
                    if (__ord7 < 0) { __ord7 = __i; __resolved++; }
                    break;
                case "COLUMN8":
                    if (__ord8 < 0) { __ord8 = __i; __resolved++; }
                    break;
                case "COLUMN9":
                    if (__ord9 < 0) { __ord9 = __i; __resolved++; }
                    break;
                case "COLUMN10":
                    if (__ord10 < 0) { __ord10 = __i; __resolved++; }
                    break;
                case "COLUMN11":
                    if (__ord11 < 0) { __ord11 = __i; __resolved++; }
                    break;
                case "COLUMN12":
                    if (__ord12 < 0) { __ord12 = __i; __resolved++; }
                    break;
                case "COLUMN13":
                    if (__ord13 < 0) { __ord13 = __i; __resolved++; }
                    break;
                case "COLUMN14":
                    if (__ord14 < 0) { __ord14 = __i; __resolved++; }
                    break;
                case "COLUMN15":
                    if (__ord15 < 0) { __ord15 = __i; __resolved++; }
                    break;
                case "COLUMN16":
                    if (__ord16 < 0) { __ord16 = __i; __resolved++; }
                    break;
                case "COLUMN17":
                    if (__ord17 < 0) { __ord17 = __i; __resolved++; }
                    break;
                case "COLUMN18":
                    if (__ord18 < 0) { __ord18 = __i; __resolved++; }
                    break;
                case "COLUMN19":
                    if (__ord19 < 0) { __ord19 = __i; __resolved++; }
                    break;
                case "COLUMN20":
                    if (__ord20 < 0) { __ord20 = __i; __resolved++; }
                    break;
                case "COLUMN21":
                    if (__ord21 < 0) { __ord21 = __i; __resolved++; }
                    break;
                case "COLUMN22":
                    if (__ord22 < 0) { __ord22 = __i; __resolved++; }
                    break;
                case "COLUMN23":
                    if (__ord23 < 0) { __ord23 = __i; __resolved++; }
                    break;
                case "COLUMN24":
                    if (__ord24 < 0) { __ord24 = __i; __resolved++; }
                    break;
                case "COLUMN25":
                    if (__ord25 < 0) { __ord25 = __i; __resolved++; }
                    break;
                case "COLUMN26":
                    if (__ord26 < 0) { __ord26 = __i; __resolved++; }
                    break;
                case "COLUMN27":
                    if (__ord27 < 0) { __ord27 = __i; __resolved++; }
                    break;
                case "COLUMN28":
                    if (__ord28 < 0) { __ord28 = __i; __resolved++; }
                    break;
                case "COLUMN29":
                    if (__ord29 < 0) { __ord29 = __i; __resolved++; }
                    break;
                case "COLUMN30":
                    if (__ord30 < 0) { __ord30 = __i; __resolved++; }
                    break;
                case "COLUMN31":
                    if (__ord31 < 0) { __ord31 = __i; __resolved++; }
                    break;
                case "COLUMN32":
                    if (__ord32 < 0) { __ord32 = __i; __resolved++; }
                    break;
                case "COLUMN33":
                    if (__ord33 < 0) { __ord33 = __i; __resolved++; }
                    break;
                case "COLUMN34":
                    if (__ord34 < 0) { __ord34 = __i; __resolved++; }
                    break;
                case "COLUMN35":
                    if (__ord35 < 0) { __ord35 = __i; __resolved++; }
                    break;
                case "COLUMN36":
                    if (__ord36 < 0) { __ord36 = __i; __resolved++; }
                    break;
                case "COLUMN37":
                    if (__ord37 < 0) { __ord37 = __i; __resolved++; }
                    break;
                case "COLUMN38":
                    if (__ord38 < 0) { __ord38 = __i; __resolved++; }
                    break;
                case "COLUMN39":
                    if (__ord39 < 0) { __ord39 = __i; __resolved++; }
                    break;
                case "COLUMN40":
                    if (__ord40 < 0) { __ord40 = __i; __resolved++; }
                    break;
                case "COLUMN41":
                    if (__ord41 < 0) { __ord41 = __i; __resolved++; }
                    break;
                case "COLUMN42":
                    if (__ord42 < 0) { __ord42 = __i; __resolved++; }
                    break;
                case "COLUMN43":
                    if (__ord43 < 0) { __ord43 = __i; __resolved++; }
                    break;
                case "COLUMN44":
                    if (__ord44 < 0) { __ord44 = __i; __resolved++; }
                    break;
                case "COLUMN45":
                    if (__ord45 < 0) { __ord45 = __i; __resolved++; }
                    break;
                case "COLUMN46":
                    if (__ord46 < 0) { __ord46 = __i; __resolved++; }
                    break;
                case "COLUMN47":
                    if (__ord47 < 0) { __ord47 = __i; __resolved++; }
                    break;
                case "COLUMN48":
                    if (__ord48 < 0) { __ord48 = __i; __resolved++; }
                    break;
                case "COLUMN49":
                    if (__ord49 < 0) { __ord49 = __i; __resolved++; }
                    break;
                case "COLUMN50":
                    if (__ord50 < 0) { __ord50 = __i; __resolved++; }
                    break;
                case "COLUMN51":
                    if (__ord51 < 0) { __ord51 = __i; __resolved++; }
                    break;
                case "COLUMN52":
                    if (__ord52 < 0) { __ord52 = __i; __resolved++; }
                    break;
                case "COLUMN53":
                    if (__ord53 < 0) { __ord53 = __i; __resolved++; }
                    break;
                case "COLUMN54":
                    if (__ord54 < 0) { __ord54 = __i; __resolved++; }
                    break;
                case "COLUMN55":
                    if (__ord55 < 0) { __ord55 = __i; __resolved++; }
                    break;
                case "COLUMN56":
                    if (__ord56 < 0) { __ord56 = __i; __resolved++; }
                    break;
                case "COLUMN57":
                    if (__ord57 < 0) { __ord57 = __i; __resolved++; }
                    break;
                case "COLUMN58":
                    if (__ord58 < 0) { __ord58 = __i; __resolved++; }
                    break;
                case "COLUMN59":
                    if (__ord59 < 0) { __ord59 = __i; __resolved++; }
                    break;
            }
            if (__resolved == 60) break;
        }
        return __ord0 + __ord1 + __ord2 + __ord3 + __ord4 + __ord5 + __ord6 + __ord7 + __ord8 + __ord9 + __ord10 + __ord11 + __ord12 + __ord13 + __ord14 + __ord15 + __ord16 + __ord17 + __ord18 + __ord19 + __ord20 + __ord21 + __ord22 + __ord23 + __ord24 + __ord25 + __ord26 + __ord27 + __ord28 + __ord29 + __ord30 + __ord31 + __ord32 + __ord33 + __ord34 + __ord35 + __ord36 + __ord37 + __ord38 + __ord39 + __ord40 + __ord41 + __ord42 + __ord43 + __ord44 + __ord45 + __ord46 + __ord47 + __ord48 + __ord49 + __ord50 + __ord51 + __ord52 + __ord53 + __ord54 + __ord55 + __ord56 + __ord57 + __ord58 + __ord59;
    }

    // Dapper.AOT 方式(N=10)：uint 定数 case + when 検証。ハッシュ定数は生成時に事前計算済み。
    internal static int HashSwitch10(DbDataReader reader)
    {
        var __ord0 = -1;
        var __ord1 = -1;
        var __ord2 = -1;
        var __ord3 = -1;
        var __ord4 = -1;
        var __ord5 = -1;
        var __ord6 = -1;
        var __ord7 = -1;
        var __ord8 = -1;
        var __ord9 = -1;
        var __resolved = 0;
        var __fieldCount = reader.FieldCount;
        for (var __i = 0; __i < __fieldCount; __i++)
        {
            var __name = reader.GetName(__i);
            switch (NormalizedHash(__name))
            {
                case 1303558709U when NormalizedEquals(__name, "column0"):
                    if (__ord0 < 0) { __ord0 = __i; __resolved++; }
                    break;
                case 1286781090U when NormalizedEquals(__name, "column1"):
                    if (__ord1 < 0) { __ord1 = __i; __resolved++; }
                    break;
                case 1270003471U when NormalizedEquals(__name, "column2"):
                    if (__ord2 < 0) { __ord2 = __i; __resolved++; }
                    break;
                case 1253225852U when NormalizedEquals(__name, "column3"):
                    if (__ord3 < 0) { __ord3 = __i; __resolved++; }
                    break;
                case 1236448233U when NormalizedEquals(__name, "column4"):
                    if (__ord4 < 0) { __ord4 = __i; __resolved++; }
                    break;
                case 1219670614U when NormalizedEquals(__name, "column5"):
                    if (__ord5 < 0) { __ord5 = __i; __resolved++; }
                    break;
                case 1202892995U when NormalizedEquals(__name, "column6"):
                    if (__ord6 < 0) { __ord6 = __i; __resolved++; }
                    break;
                case 1186115376U when NormalizedEquals(__name, "column7"):
                    if (__ord7 < 0) { __ord7 = __i; __resolved++; }
                    break;
                case 1437779661U when NormalizedEquals(__name, "column8"):
                    if (__ord8 < 0) { __ord8 = __i; __resolved++; }
                    break;
                case 1421002042U when NormalizedEquals(__name, "column9"):
                    if (__ord9 < 0) { __ord9 = __i; __resolved++; }
                    break;
            }
            if (__resolved == 10) break;
        }
        return __ord0 + __ord1 + __ord2 + __ord3 + __ord4 + __ord5 + __ord6 + __ord7 + __ord8 + __ord9;
    }

    // Dapper.AOT 方式(N=30)：uint 定数 case + when 検証。ハッシュ定数は生成時に事前計算済み。
    internal static int HashSwitch30(DbDataReader reader)
    {
        var __ord0 = -1;
        var __ord1 = -1;
        var __ord2 = -1;
        var __ord3 = -1;
        var __ord4 = -1;
        var __ord5 = -1;
        var __ord6 = -1;
        var __ord7 = -1;
        var __ord8 = -1;
        var __ord9 = -1;
        var __ord10 = -1;
        var __ord11 = -1;
        var __ord12 = -1;
        var __ord13 = -1;
        var __ord14 = -1;
        var __ord15 = -1;
        var __ord16 = -1;
        var __ord17 = -1;
        var __ord18 = -1;
        var __ord19 = -1;
        var __ord20 = -1;
        var __ord21 = -1;
        var __ord22 = -1;
        var __ord23 = -1;
        var __ord24 = -1;
        var __ord25 = -1;
        var __ord26 = -1;
        var __ord27 = -1;
        var __ord28 = -1;
        var __ord29 = -1;
        var __resolved = 0;
        var __fieldCount = reader.FieldCount;
        for (var __i = 0; __i < __fieldCount; __i++)
        {
            var __name = reader.GetName(__i);
            switch (NormalizedHash(__name))
            {
                case 1303558709U when NormalizedEquals(__name, "column0"):
                    if (__ord0 < 0) { __ord0 = __i; __resolved++; }
                    break;
                case 1286781090U when NormalizedEquals(__name, "column1"):
                    if (__ord1 < 0) { __ord1 = __i; __resolved++; }
                    break;
                case 1270003471U when NormalizedEquals(__name, "column2"):
                    if (__ord2 < 0) { __ord2 = __i; __resolved++; }
                    break;
                case 1253225852U when NormalizedEquals(__name, "column3"):
                    if (__ord3 < 0) { __ord3 = __i; __resolved++; }
                    break;
                case 1236448233U when NormalizedEquals(__name, "column4"):
                    if (__ord4 < 0) { __ord4 = __i; __resolved++; }
                    break;
                case 1219670614U when NormalizedEquals(__name, "column5"):
                    if (__ord5 < 0) { __ord5 = __i; __resolved++; }
                    break;
                case 1202892995U when NormalizedEquals(__name, "column6"):
                    if (__ord6 < 0) { __ord6 = __i; __resolved++; }
                    break;
                case 1186115376U when NormalizedEquals(__name, "column7"):
                    if (__ord7 < 0) { __ord7 = __i; __resolved++; }
                    break;
                case 1437779661U when NormalizedEquals(__name, "column8"):
                    if (__ord8 < 0) { __ord8 = __i; __resolved++; }
                    break;
                case 1421002042U when NormalizedEquals(__name, "column9"):
                    if (__ord9 < 0) { __ord9 = __i; __resolved++; }
                    break;
                case 1331203542U when NormalizedEquals(__name, "column10"):
                    if (__ord10 < 0) { __ord10 = __i; __resolved++; }
                    break;
                case 1347981161U when NormalizedEquals(__name, "column11"):
                    if (__ord11 < 0) { __ord11 = __i; __resolved++; }
                    break;
                case 1297648304U when NormalizedEquals(__name, "column12"):
                    if (__ord12 < 0) { __ord12 = __i; __resolved++; }
                    break;
                case 1314425923U when NormalizedEquals(__name, "column13"):
                    if (__ord13 < 0) { __ord13 = __i; __resolved++; }
                    break;
                case 1398314018U when NormalizedEquals(__name, "column14"):
                    if (__ord14 < 0) { __ord14 = __i; __resolved++; }
                    break;
                case 1415091637U when NormalizedEquals(__name, "column15"):
                    if (__ord15 < 0) { __ord15 = __i; __resolved++; }
                    break;
                case 1364758780U when NormalizedEquals(__name, "column16"):
                    if (__ord16 < 0) { __ord16 = __i; __resolved++; }
                    break;
                case 1381536399U when NormalizedEquals(__name, "column17"):
                    if (__ord17 < 0) { __ord17 = __i; __resolved++; }
                    break;
                case 1465424494U when NormalizedEquals(__name, "column18"):
                    if (__ord18 < 0) { __ord18 = __i; __resolved++; }
                    break;
                case 1482202113U when NormalizedEquals(__name, "column19"):
                    if (__ord19 < 0) { __ord19 = __i; __resolved++; }
                    break;
                case 1767274541U when NormalizedEquals(__name, "column20"):
                    if (__ord20 < 0) { __ord20 = __i; __resolved++; }
                    break;
                case 1750496922U when NormalizedEquals(__name, "column21"):
                    if (__ord21 < 0) { __ord21 = __i; __resolved++; }
                    break;
                case 1733719303U when NormalizedEquals(__name, "column22"):
                    if (__ord22 < 0) { __ord22 = __i; __resolved++; }
                    break;
                case 1716941684U when NormalizedEquals(__name, "column23"):
                    if (__ord23 < 0) { __ord23 = __i; __resolved++; }
                    break;
                case 1700164065U when NormalizedEquals(__name, "column24"):
                    if (__ord24 < 0) { __ord24 = __i; __resolved++; }
                    break;
                case 1683386446U when NormalizedEquals(__name, "column25"):
                    if (__ord25 < 0) { __ord25 = __i; __resolved++; }
                    break;
                case 1666608827U when NormalizedEquals(__name, "column26"):
                    if (__ord26 < 0) { __ord26 = __i; __resolved++; }
                    break;
                case 1649831208U when NormalizedEquals(__name, "column27"):
                    if (__ord27 < 0) { __ord27 = __i; __resolved++; }
                    break;
                case 1633053589U when NormalizedEquals(__name, "column28"):
                    if (__ord28 < 0) { __ord28 = __i; __resolved++; }
                    break;
                case 1616275970U when NormalizedEquals(__name, "column29"):
                    if (__ord29 < 0) { __ord29 = __i; __resolved++; }
                    break;
            }
            if (__resolved == 30) break;
        }
        return __ord0 + __ord1 + __ord2 + __ord3 + __ord4 + __ord5 + __ord6 + __ord7 + __ord8 + __ord9 + __ord10 + __ord11 + __ord12 + __ord13 + __ord14 + __ord15 + __ord16 + __ord17 + __ord18 + __ord19 + __ord20 + __ord21 + __ord22 + __ord23 + __ord24 + __ord25 + __ord26 + __ord27 + __ord28 + __ord29;
    }

    // Dapper.AOT 方式(N=60)：uint 定数 case + when 検証。ハッシュ定数は生成時に事前計算済み。
    internal static int HashSwitch60(DbDataReader reader)
    {
        var __ord0 = -1;
        var __ord1 = -1;
        var __ord2 = -1;
        var __ord3 = -1;
        var __ord4 = -1;
        var __ord5 = -1;
        var __ord6 = -1;
        var __ord7 = -1;
        var __ord8 = -1;
        var __ord9 = -1;
        var __ord10 = -1;
        var __ord11 = -1;
        var __ord12 = -1;
        var __ord13 = -1;
        var __ord14 = -1;
        var __ord15 = -1;
        var __ord16 = -1;
        var __ord17 = -1;
        var __ord18 = -1;
        var __ord19 = -1;
        var __ord20 = -1;
        var __ord21 = -1;
        var __ord22 = -1;
        var __ord23 = -1;
        var __ord24 = -1;
        var __ord25 = -1;
        var __ord26 = -1;
        var __ord27 = -1;
        var __ord28 = -1;
        var __ord29 = -1;
        var __ord30 = -1;
        var __ord31 = -1;
        var __ord32 = -1;
        var __ord33 = -1;
        var __ord34 = -1;
        var __ord35 = -1;
        var __ord36 = -1;
        var __ord37 = -1;
        var __ord38 = -1;
        var __ord39 = -1;
        var __ord40 = -1;
        var __ord41 = -1;
        var __ord42 = -1;
        var __ord43 = -1;
        var __ord44 = -1;
        var __ord45 = -1;
        var __ord46 = -1;
        var __ord47 = -1;
        var __ord48 = -1;
        var __ord49 = -1;
        var __ord50 = -1;
        var __ord51 = -1;
        var __ord52 = -1;
        var __ord53 = -1;
        var __ord54 = -1;
        var __ord55 = -1;
        var __ord56 = -1;
        var __ord57 = -1;
        var __ord58 = -1;
        var __ord59 = -1;
        var __resolved = 0;
        var __fieldCount = reader.FieldCount;
        for (var __i = 0; __i < __fieldCount; __i++)
        {
            var __name = reader.GetName(__i);
            switch (NormalizedHash(__name))
            {
                case 1303558709U when NormalizedEquals(__name, "column0"):
                    if (__ord0 < 0) { __ord0 = __i; __resolved++; }
                    break;
                case 1286781090U when NormalizedEquals(__name, "column1"):
                    if (__ord1 < 0) { __ord1 = __i; __resolved++; }
                    break;
                case 1270003471U when NormalizedEquals(__name, "column2"):
                    if (__ord2 < 0) { __ord2 = __i; __resolved++; }
                    break;
                case 1253225852U when NormalizedEquals(__name, "column3"):
                    if (__ord3 < 0) { __ord3 = __i; __resolved++; }
                    break;
                case 1236448233U when NormalizedEquals(__name, "column4"):
                    if (__ord4 < 0) { __ord4 = __i; __resolved++; }
                    break;
                case 1219670614U when NormalizedEquals(__name, "column5"):
                    if (__ord5 < 0) { __ord5 = __i; __resolved++; }
                    break;
                case 1202892995U when NormalizedEquals(__name, "column6"):
                    if (__ord6 < 0) { __ord6 = __i; __resolved++; }
                    break;
                case 1186115376U when NormalizedEquals(__name, "column7"):
                    if (__ord7 < 0) { __ord7 = __i; __resolved++; }
                    break;
                case 1437779661U when NormalizedEquals(__name, "column8"):
                    if (__ord8 < 0) { __ord8 = __i; __resolved++; }
                    break;
                case 1421002042U when NormalizedEquals(__name, "column9"):
                    if (__ord9 < 0) { __ord9 = __i; __resolved++; }
                    break;
                case 1331203542U when NormalizedEquals(__name, "column10"):
                    if (__ord10 < 0) { __ord10 = __i; __resolved++; }
                    break;
                case 1347981161U when NormalizedEquals(__name, "column11"):
                    if (__ord11 < 0) { __ord11 = __i; __resolved++; }
                    break;
                case 1297648304U when NormalizedEquals(__name, "column12"):
                    if (__ord12 < 0) { __ord12 = __i; __resolved++; }
                    break;
                case 1314425923U when NormalizedEquals(__name, "column13"):
                    if (__ord13 < 0) { __ord13 = __i; __resolved++; }
                    break;
                case 1398314018U when NormalizedEquals(__name, "column14"):
                    if (__ord14 < 0) { __ord14 = __i; __resolved++; }
                    break;
                case 1415091637U when NormalizedEquals(__name, "column15"):
                    if (__ord15 < 0) { __ord15 = __i; __resolved++; }
                    break;
                case 1364758780U when NormalizedEquals(__name, "column16"):
                    if (__ord16 < 0) { __ord16 = __i; __resolved++; }
                    break;
                case 1381536399U when NormalizedEquals(__name, "column17"):
                    if (__ord17 < 0) { __ord17 = __i; __resolved++; }
                    break;
                case 1465424494U when NormalizedEquals(__name, "column18"):
                    if (__ord18 < 0) { __ord18 = __i; __resolved++; }
                    break;
                case 1482202113U when NormalizedEquals(__name, "column19"):
                    if (__ord19 < 0) { __ord19 = __i; __resolved++; }
                    break;
                case 1767274541U when NormalizedEquals(__name, "column20"):
                    if (__ord20 < 0) { __ord20 = __i; __resolved++; }
                    break;
                case 1750496922U when NormalizedEquals(__name, "column21"):
                    if (__ord21 < 0) { __ord21 = __i; __resolved++; }
                    break;
                case 1733719303U when NormalizedEquals(__name, "column22"):
                    if (__ord22 < 0) { __ord22 = __i; __resolved++; }
                    break;
                case 1716941684U when NormalizedEquals(__name, "column23"):
                    if (__ord23 < 0) { __ord23 = __i; __resolved++; }
                    break;
                case 1700164065U when NormalizedEquals(__name, "column24"):
                    if (__ord24 < 0) { __ord24 = __i; __resolved++; }
                    break;
                case 1683386446U when NormalizedEquals(__name, "column25"):
                    if (__ord25 < 0) { __ord25 = __i; __resolved++; }
                    break;
                case 1666608827U when NormalizedEquals(__name, "column26"):
                    if (__ord26 < 0) { __ord26 = __i; __resolved++; }
                    break;
                case 1649831208U when NormalizedEquals(__name, "column27"):
                    if (__ord27 < 0) { __ord27 = __i; __resolved++; }
                    break;
                case 1633053589U when NormalizedEquals(__name, "column28"):
                    if (__ord28 < 0) { __ord28 = __i; __resolved++; }
                    break;
                case 1616275970U when NormalizedEquals(__name, "column29"):
                    if (__ord29 < 0) { __ord29 = __i; __resolved++; }
                    break;
                case 3813893796U when NormalizedEquals(__name, "column30"):
                    if (__ord30 < 0) { __ord30 = __i; __resolved++; }
                    break;
                case 3830671415U when NormalizedEquals(__name, "column31"):
                    if (__ord31 < 0) { __ord31 = __i; __resolved++; }
                    break;
                case 3847449034U when NormalizedEquals(__name, "column32"):
                    if (__ord32 < 0) { __ord32 = __i; __resolved++; }
                    break;
                case 3864226653U when NormalizedEquals(__name, "column33"):
                    if (__ord33 < 0) { __ord33 = __i; __resolved++; }
                    break;
                case 3746783320U when NormalizedEquals(__name, "column34"):
                    if (__ord34 < 0) { __ord34 = __i; __resolved++; }
                    break;
                case 3763560939U when NormalizedEquals(__name, "column35"):
                    if (__ord35 < 0) { __ord35 = __i; __resolved++; }
                    break;
                case 3780338558U when NormalizedEquals(__name, "column36"):
                    if (__ord36 < 0) { __ord36 = __i; __resolved++; }
                    break;
                case 3797116177U when NormalizedEquals(__name, "column37"):
                    if (__ord37 < 0) { __ord37 = __i; __resolved++; }
                    break;
                case 3679672844U when NormalizedEquals(__name, "column38"):
                    if (__ord38 < 0) { __ord38 = __i; __resolved++; }
                    break;
                case 3696450463U when NormalizedEquals(__name, "column39"):
                    if (__ord39 < 0) { __ord39 = __i; __resolved++; }
                    break;
                case 3713080987U when NormalizedEquals(__name, "column40"):
                    if (__ord40 < 0) { __ord40 = __i; __resolved++; }
                    break;
                case 3696303368U when NormalizedEquals(__name, "column41"):
                    if (__ord41 < 0) { __ord41 = __i; __resolved++; }
                    break;
                case 3746636225U when NormalizedEquals(__name, "column42"):
                    if (__ord42 < 0) { __ord42 = __i; __resolved++; }
                    break;
                case 3729858606U when NormalizedEquals(__name, "column43"):
                    if (__ord43 < 0) { __ord43 = __i; __resolved++; }
                    break;
                case 3780191463U when NormalizedEquals(__name, "column44"):
                    if (__ord44 < 0) { __ord44 = __i; __resolved++; }
                    break;
                case 3763413844U when NormalizedEquals(__name, "column45"):
                    if (__ord45 < 0) { __ord45 = __i; __resolved++; }
                    break;
                case 3813746701U when NormalizedEquals(__name, "column46"):
                    if (__ord46 < 0) { __ord46 = __i; __resolved++; }
                    break;
                case 3796969082U when NormalizedEquals(__name, "column47"):
                    if (__ord47 < 0) { __ord47 = __i; __resolved++; }
                    break;
                case 3578860035U when NormalizedEquals(__name, "column48"):
                    if (__ord48 < 0) { __ord48 = __i; __resolved++; }
                    break;
                case 3562082416U when NormalizedEquals(__name, "column49"):
                    if (__ord49 < 0) { __ord49 = __i; __resolved++; }
                    break;
                case 3612268178U when NormalizedEquals(__name, "column50"):
                    if (__ord50 < 0) { __ord50 = __i; __resolved++; }
                    break;
                case 3629045797U when NormalizedEquals(__name, "column51"):
                    if (__ord51 < 0) { __ord51 = __i; __resolved++; }
                    break;
                case 3578712940U when NormalizedEquals(__name, "column52"):
                    if (__ord52 < 0) { __ord52 = __i; __resolved++; }
                    break;
                case 3595490559U when NormalizedEquals(__name, "column53"):
                    if (__ord53 < 0) { __ord53 = __i; __resolved++; }
                    break;
                case 3545157702U when NormalizedEquals(__name, "column54"):
                    if (__ord54 < 0) { __ord54 = __i; __resolved++; }
                    break;
                case 3561935321U when NormalizedEquals(__name, "column55"):
                    if (__ord55 < 0) { __ord55 = __i; __resolved++; }
                    break;
                case 3511602464U when NormalizedEquals(__name, "column56"):
                    if (__ord56 < 0) { __ord56 = __i; __resolved++; }
                    break;
                case 3528380083U when NormalizedEquals(__name, "column57"):
                    if (__ord57 < 0) { __ord57 = __i; __resolved++; }
                    break;
                case 3746489130U when NormalizedEquals(__name, "column58"):
                    if (__ord58 < 0) { __ord58 = __i; __resolved++; }
                    break;
                case 3763266749U when NormalizedEquals(__name, "column59"):
                    if (__ord59 < 0) { __ord59 = __i; __resolved++; }
                    break;
            }
            if (__resolved == 60) break;
        }
        return __ord0 + __ord1 + __ord2 + __ord3 + __ord4 + __ord5 + __ord6 + __ord7 + __ord8 + __ord9 + __ord10 + __ord11 + __ord12 + __ord13 + __ord14 + __ord15 + __ord16 + __ord17 + __ord18 + __ord19 + __ord20 + __ord21 + __ord22 + __ord23 + __ord24 + __ord25 + __ord26 + __ord27 + __ord28 + __ord29 + __ord30 + __ord31 + __ord32 + __ord33 + __ord34 + __ord35 + __ord36 + __ord37 + __ord38 + __ord39 + __ord40 + __ord41 + __ord42 + __ord43 + __ord44 + __ord45 + __ord46 + __ord47 + __ord48 + __ord49 + __ord50 + __ord51 + __ord52 + __ord53 + __ord54 + __ord55 + __ord56 + __ord57 + __ord58 + __ord59;
    }
}
#pragma warning restore CA1001, SA1107, SA1312, SA1501, SA1503

// SELECT * 相当のミス走査：不一致 60 列に対する各戦略のコスト(早期 break は一度も発火しない)。
#pragma warning disable CA1001, SA1107, SA1312, SA1501, SA1503 // mirrors generated-emit style verbatim
[Config(typeof(MappingConfig))]
public class OrdinalResolutionMissBenchmark
{
    private MockDataReader reader = default!;
    private Dictionary<string, int> dictionary = default!;
    private FrozenDictionary<string, int> frozen = default!;

    [GlobalSetup]
    public void Setup()
    {
        var columns = Enumerable.Range(0, 60)
            .Select(x => new MockColumn(typeof(long), $"Extra{x}"))
            .ToArray();
        reader = new MockDataReader(columns, new List<object[]>());

        dictionary = new Dictionary<string, int>(10, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 10; i++)
        {
            dictionary[$"Column{i}"] = i;
        }
        frozen = dictionary.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    [GlobalCleanup]
    public void Cleanup() => reader.Dispose();

    [Benchmark(Description = "Miss60: UpperSwitch (N=10)")]
    public int UpperSwitch() => OrdinalResolutionBenchmark.UpperSwitch10(reader);

    [Benchmark(Description = "Miss60: Dictionary (N=10)")]
    public int Dictionary() => OrdinalResolutionBenchmark.DictionaryResolve(reader, dictionary, 10);

    [Benchmark(Description = "Miss60: FrozenDictionary (N=10)")]
    public int Frozen() => OrdinalResolutionBenchmark.FrozenResolve(reader, frozen, 10);

    [Benchmark(Description = "Miss60: NormalizedHash (N=10)")]
    public int NormalizedHashSwitch() => OrdinalResolutionBenchmark.HashSwitch10(reader);
}
#pragma warning restore CA1001, SA1107, SA1312, SA1501, SA1503
