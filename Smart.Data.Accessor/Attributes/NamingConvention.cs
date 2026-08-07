namespace Smart.Data.Accessor.Attributes;

// [Naming] で指定する、[Name] 明示が無い場合の既定名(プロパティ名/エンティティ型名)の変換規約。
// snake_case の語分割規則は System.Text.Json の JsonNamingPolicy と同一。
// The default-name (property / entity type name) conversion convention specified by [Naming], applied
// only when no explicit [Name] is present. The snake_case word-splitting rules match System.Text.Json's
// JsonNamingPolicy.
public enum NamingConvention
{
    // UserId -> UserId (現行動作 / current behavior)
    None = 0,

    // UserId -> user_id
    SnakeCaseLower,

    // UserId -> USER_ID
    SnakeCaseUpper,

    // UserId -> userid
    LowerCase,

    // UserId -> USERID
    UpperCase
}
