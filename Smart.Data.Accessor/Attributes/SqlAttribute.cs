namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Inline 2-way SQL: the constructor argument is processed by the same 2-way SQL pipeline as a
// .sql additional file (bind markers /*@ */, conditional blocks /*% */, /*!using*/ directives).
// The execution-kind attribute ([Execute] / [ExecuteScalar] / [Query] / [QueryFirst] /
// [ExecuteReader]) is still required — command-source attributes never default it.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlAttribute : Attribute
{
    public string Text { get; }

    public SqlAttribute(string text)
    {
        Text = text;
    }
}
