namespace Smart.Data.Accessor.Generator.Sql;

public enum TokenType
{
    // 2-way SQL directive (/*!, /*@, /*#, /*%). Consumed by Nodes.NodeBuilder.
    Comment,

    // SQL optimiser hint (/*+ ... */). Emitted inline in the output SQL, with no whitespace side-effects.
    Hint,

    Block,
    Comma,
    Blank,
    OpenParenthesis,
    CloseParenthesis
}
