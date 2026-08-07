namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// [Name] が無い場合に既定名(プロパティ名/エンティティ型名)へ適用する変換規約を指定する。
// [BindPrefix] と同様に method → class → assembly の順で解決される([Name] 明示は常に優先)。
// Specifies the conversion convention applied to default names (property / entity type names) when no
// [Name] is present. Resolved method → class → assembly like [BindPrefix] (an explicit [Name] always wins).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class NamingAttribute : Attribute
{
    public NamingConvention Convention { get; }

    public NamingAttribute(NamingConvention convention)
    {
        Convention = convention;
    }
}
