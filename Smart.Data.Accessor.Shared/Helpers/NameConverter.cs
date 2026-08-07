namespace Smart.Data.Accessor.Shared.Helpers;

using System.Text;

// [Naming] 規約による既定名変換の純関数(コンパイル時にのみ実行。決定的・カルチャ非依存)。snake_case の
// 語分割は System.Text.Json の JsonNamingPolicy と同じ規則：小文字/数字→大文字の境界で分割、大文字連続の
// 後に小文字が続く場合は最後の大文字の前で分割、既存アンダースコアは区切りとして維持(変換は冪等)。
// Pure default-name conversion for the [Naming] convention (runs at compile time only; deterministic and
// culture-invariant). The snake_case word splitting follows the same rules as System.Text.Json's
// JsonNamingPolicy: split at a lower/digit -> upper boundary, an uppercase run followed by lowercase splits
// before its last upper, and existing underscores are kept as separators (the conversion is idempotent).
internal static class NameConverter
{
    private enum CharCategory
    {
        Boundary,
        Upper,
        LowerOrOther
    }

    public static string Convert(string name, NamingConvention convention) => convention switch
    {
        NamingConvention.SnakeCaseLower => ConvertSnakeCase(name, toUpper: false),
        NamingConvention.SnakeCaseUpper => ConvertSnakeCase(name, toUpper: true),
        NamingConvention.LowerCase => ConvertCase(name, toUpper: false),
        NamingConvention.UpperCase => ConvertCase(name, toUpper: true),
        _ => name
    };

    // 識別子は 1 文字 1 文字の Invariant 変換で十分(1 対多のカルチャ特殊対応は不要)。CA1308 も回避できる。
    // Per-char invariant conversion is sufficient for identifiers (no one-to-many culture special cases needed);
    // it also avoids CA1308.
    private static string ConvertCase(string name, bool toUpper)
    {
        if (String.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            builder.Append(toUpper ? Char.ToUpperInvariant(c) : Char.ToLowerInvariant(c));
        }
        return builder.ToString();
    }

    private static string ConvertSnakeCase(string name, bool toUpper)
    {
        if (String.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 4);
        var previous = CharCategory.Boundary;
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c == '_')
            {
                builder.Append('_');
                previous = CharCategory.Boundary;
                continue;
            }

            if (Char.IsUpper(c))
            {
                if ((previous == CharCategory.LowerOrOther) ||
                    ((previous == CharCategory.Upper) && (i + 1 < name.Length) && Char.IsLower(name[i + 1])))
                {
                    builder.Append('_');
                }
                builder.Append(toUpper ? c : Char.ToLowerInvariant(c));
                previous = CharCategory.Upper;
            }
            else
            {
                builder.Append(toUpper ? Char.ToUpperInvariant(c) : c);
                previous = CharCategory.LowerOrOther;
            }
        }

        return builder.ToString();
    }
}
