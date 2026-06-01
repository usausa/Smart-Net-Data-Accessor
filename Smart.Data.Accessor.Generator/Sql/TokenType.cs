namespace Smart.Data.Accessor.Generator.Sql;

public enum TokenType
{
    /// <summary>2-way SQL directive (<c>/*!</c>, <c>/*@</c>, <c>/*#</c>, <c>/*%</c>). Consumed by <see cref="Nodes.NodeBuilder"/>.</summary>
    Comment,

    /// <summary>SQL optimiser hint (<c>/*+ ... */</c>). Emitted inline in the output SQL, with no whitespace side-effects.</summary>
    Hint,

    Block,
    Comma,
    Blank,
    OpenParenthesis,
    CloseParenthesis
}
