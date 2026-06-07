namespace Smart.Data.Accessor.Generator.Helpers;

using System;

// Generator-internal, domain-agnostic string utilities.
internal static class StringHelper
{
    // The text before the first '.' (e.g. "arg.Prop" -> "arg"); the whole string when there is no dot.
    public static string ExtractRoot(string name)
    {
        var dot = name.IndexOf('.');
        return dot < 0 ? name : name[..dot];
    }

    // Whole-word occurrence of identifier in text, using identifier-char boundaries so e.g. "log" does not match "dialog".
    public static bool ContainsWholeWordIdentifier(string text, string identifier)
    {
        if (String.IsNullOrEmpty(identifier))
        {
            return false;
        }
        var index = text.IndexOf(identifier, StringComparison.Ordinal);
        while (index >= 0)
        {
            var beforeOk = (index == 0) || !IsIdentifierChar(text[index - 1]);
            var afterPos = index + identifier.Length;
            var afterOk = (afterPos >= text.Length) || !IsIdentifierChar(text[afterPos]);
            if (beforeOk && afterOk)
            {
                return true;
            }
            index = text.IndexOf(identifier, index + 1, StringComparison.Ordinal);
        }
        return false;
    }

    private static bool IsIdentifierChar(char character) => Char.IsLetterOrDigit(character) || (character == '_');
}
