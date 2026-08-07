namespace Smart.Data.Accessor.Tests.Models;

using Smart.Data.Accessor.Attributes;

// [Naming] 規約検証用。[Name] を持たないプロパティは規約変換された列名(user_id 等)で照合される。
// For [Naming] convention coverage: properties without [Name] match through convention-converted
// column names (user_id, ...).
internal sealed class NamingEntity
{
    [Key]
    public long UserId { get; set; }

    public string FirstName { get; set; } = "unset";
}
