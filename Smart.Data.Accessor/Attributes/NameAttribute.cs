namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// 列名(プロパティ / パラメータ)の上書き。エンティティクラスに付けた場合は Builder のテーブル名になる(Table= 未指定時)。
// Overrides the column name (property / parameter). On an entity class it supplies the builder table name (when Table= is absent).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class NameAttribute : Attribute
{
    public string Name { get; }

    public NameAttribute(string name)
    {
        Name = name;
    }
}
