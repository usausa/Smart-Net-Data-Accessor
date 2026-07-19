namespace Smart.Data.Accessor.AotTests;

using Smart.Data.Accessor.Attributes;

internal sealed class AotData
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Type { get; set; }
}

// 閾値(16)超えの 19 グループ＝サンプリングハッシュ switch 形で序数解決されるエンティティ。
// NativeAOT ＋ InvariantGlobalization ＋ 実プロバイダ(SQLite)の組で以下を同時に通す：
//  * 混在キー(user_名前 / col_дата は非サンプリング位置にのみ非 ASCII)でも switch 形が選ばれること
//  * 大小違いの実列名(ID / USER_名前 / LAST_MODIFIED_BY_X)が OrdinalIgnoreCase で解決されること
//  * どのサンプリング三つ組でも衝突する t1_value / t2_value が case 内 Equals 連鎖(バケット化)で解けること
//  * 未マップ列と無別名式列(SELECT の裸の 1)が無害に無視されること
// 全キーの長さは t1_value / t2_value / col_дата(いずれも 8)を除き互いに異なる。
// A 19-group entity (above the threshold of 16), resolved by the sampling-hash switch. Combined with NativeAOT +
// InvariantGlobalization + a real provider (SQLite) this exercises at once: mixed keys (user_名前 / col_дата carry
// non-ASCII only at non-sampled positions) still taking the switch form; case-variant real column names
// (ID / USER_名前 / LAST_MODIFIED_BY_X) resolving under OrdinalIgnoreCase; t1_value / t2_value - colliding under
// every triple - resolving through the bucketed in-case Equals chain; and unmapped plus unaliased expression
// columns (a bare 1 in the SELECT) being ignored harmlessly. All key lengths are distinct except
// t1_value / t2_value / col_дата (each 8).
internal sealed class AotWideData
{
    [Name("id")]
    public long Id { get; set; }

    [Name("age")]
    public int Age { get; set; } = -1;

    [Name("city")]
    public string City { get; set; } = "unset";

    [Name("email")]
    public string Email { get; set; } = "unset";

    [Name("status")]
    public int Status { get; set; } = -1;

    [Name("user_名前")]
    public string UserName { get; set; } = "unset";

    [Name("col_дата")]
    public string ColDate { get; set; } = "unset";

    [Name("item_code")]
    public string ItemCode { get; set; } = "unset";

    [Name("created_at")]
    public string CreatedAt { get; set; } = "unset";

    [Name("status_code")]
    public string StatusCode { get; set; } = "unset";

    [Name("display_name")]
    public string DisplayName { get; set; } = "unset";

    [Name("department_id")]
    public int DepartmentId { get; set; } = -1;

    [Name("address_line_1")]
    public string AddressLine1 { get; set; } = "unset";

    [Name("manager_user_id")]
    public int ManagerUserId { get; set; } = -1;

    [Name("organization_cd1")]
    public string OrganizationCd1 { get; set; } = "unset";

    [Name("registration_date")]
    public string RegistrationDate { get; set; } = "unset";

    [Name("last_modified_by_x")]
    public string LastModifiedByX { get; set; } = "unset";

    [Name("t1_value")]
    public int T1Value { get; set; } = -1;

    [Name("t2_value")]
    public int T2Value { get; set; } = -1;
}
