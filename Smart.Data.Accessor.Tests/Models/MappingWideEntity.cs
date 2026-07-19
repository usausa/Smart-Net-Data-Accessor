namespace Smart.Data.Accessor.Tests.Models;

using Smart.Data.Accessor.Attributes;

// 閾値(NarrowOrdinalGroupThreshold)超えのエンティティ。序数解決がハッシュ switch 形で emit される経路を
// 実際に「実行して」検証するために置いている。閾値以下の直比較形しか実行されないと、switch 形の退行は
// 生成テキストの assert でしか捕まらない。
// An entity above NarrowOrdinalGroupThreshold, so its ordinal resolution is emitted as the hash-switch form. It
// exists so that path is actually executed: without it a switch-form regression could only be caught by asserting
// on generated text.
internal sealed class MappingWideEntity
{
    public long Id { get; set; }

    public string Name { get; set; } = "unset";

    public int Age { get; set; } = -1;

    public string Email { get; set; } = "unset";

    public string Phone { get; set; } = "unset";

    public string City { get; set; } = "unset";

    public string State { get; set; } = "unset";

    public string Country { get; set; } = "unset";

    public string Note { get; set; } = "unset";

    public int Status { get; set; } = -1;

    public int Rank { get; set; } = -1;

    public int Level { get; set; } = -1;

    public int OwnerId { get; set; } = -1;

    public int GroupId { get; set; } = -1;

    public int Version { get; set; } = -1;

    public string CreatedBy { get; set; } = "unset";

    public string UpdatedBy { get; set; } = "unset";
}

// 既定のサンプリング位置(先頭/中央/末尾)で衝突するキー集合。同一長＋共通接頭辞＋共通末尾は SQL でありふれた
// 命名で、`item_code / item_name / item_note / item_type / item_size` は 5 個が同じハッシュに落ちる。
// ジェネレータは衝突しないサンプリング位置を探し、見つからなければ 1 つの case にまとめて Equals 連鎖で解く。
// どちらの経路を通っても照合結果が変わらないことを実行して確認する。
// A key set that collides under the default sampling positions. Equal length + shared prefix + shared final character
// is idiomatic SQL naming, and `item_code / item_name / item_note / item_type / item_size` all hash alike. The
// generator searches for non-colliding sampling positions and buckets into a single case with an Equals chain if it
// finds none; this verifies by execution that either path resolves identically.
internal sealed class MappingCollideHashEntity
{
    [Name("item_code")]
    public string ItemCode { get; set; } = "unset";

    [Name("item_name")]
    public string ItemName { get; set; } = "unset";

    [Name("item_note")]
    public string ItemNote { get; set; } = "unset";

    [Name("item_type")]
    public string ItemType { get; set; } = "unset";

    [Name("item_size")]
    public string ItemSize { get; set; } = "unset";

    [Name("item_unit")]
    public string ItemUnit { get; set; } = "unset";

    [Name("item_rank")]
    public string ItemRank { get; set; } = "unset";

    [Name("item_desc")]
    public string ItemDesc { get; set; } = "unset";

    [Name("item_memo")]
    public string ItemMemo { get; set; } = "unset";

    [Name("flag_01")]
    public int Flag01 { get; set; } = -1;

    [Name("flag_02")]
    public int Flag02 { get; set; } = -1;

    [Name("flag_11")]
    public int Flag11 { get; set; } = -1;

    [Name("flag_12")]
    public int Flag12 { get; set; } = -1;

    [Name("value_01")]
    public int Value01 { get; set; } = -1;

    [Name("value_02")]
    public int Value02 { get; set; } = -1;

    [Name("value_03")]
    public int Value03 { get; set; } = -1;

    [Name("value_04")]
    public int Value04 { get; set; } = -1;

    // 長さ 8 では index 1 がどのサンプリング位置候補からも参照されないため、この 2 つはどの三つ組を選んでも
    // 必ず同じハッシュに落ちる。位置探索では回避できず、1 つの case にまとめた Equals 連鎖でしか解けない
    // ＝バケット化経路を実行時に通すためのペア。
    // At length 8 no candidate sampling position reaches index 1, so these two collide under every possible triple.
    // The search cannot avoid it and only the bucketed Equals chain can separate them - this pair is what drives the
    // bucketing path at run time.
    [Name("t1_value")]
    public int T1Value { get; set; } = -1;

    [Name("t2_value")]
    public int T2Value { get; set; } = -1;
}

// ASCII と非 ASCII が混在する列名の集合。switch 形の健全性の最も微妙な前提を実行時に固定するために置いている：
// ジェネレータは「サンプリング 3 位置が全キーで ASCII になる三つ組」しか採用せず、その保証は
//   (1) ASCII の ToUpperInvariant はジェネレータ側・アプリ側のランタイムで一致する
//   (2) OrdinalIgnoreCase で ASCII 文字と等しくなる非 ASCII 文字は存在しない
// の 2 性質による。よって「非サンプリング位置に非 ASCII を含むキー」(user_名前 / col_дата)でも switch 形が
// 選ばれ、大小違いの実行時文字列（COL_ДАТА 等）はハッシュ一致＋case 内 Equals で正しく解決される。
// 全キーの長さを互いに異ならせている（長さ項が上位 16 bit を占めるため、どの三つ組でも衝突ゼロが保証され、
// テストの関心を混在キーの照合だけに絞れる）。
// A key set mixing ASCII and non-ASCII names. It pins, at run time, the subtlest soundness premise of the switch
// form: the generator only adopts triples whose three sampled positions are ASCII across every key, guaranteed by
//   (1) ASCII ToUpperInvariant agrees between the generator's runtime and the app's runtime, and
//   (2) no non-ASCII character is OrdinalIgnoreCase-equal to an ASCII one.
// Keys with non-ASCII only at NON-sampled positions (user_名前 / col_дата) therefore still take the switch form, and
// case-variant runtime strings (e.g. COL_ДАТА) resolve via hash match + in-case Equals. Every key has a distinct
// length (the length term occupies the upper 16 bits, so zero collisions under any triple - keeping the test focused
// on mixed-key matching alone).
internal sealed class MappingMixedNameEntity
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
}

// サンプリング位置を ASCII に取れない列名。閾値を超えていてもハッシュ switch は使えず直比較へ落ちる
// (ハッシュ定数はジェネレータのランタイムで、照合は対象アプリのランタイムで計算されるため、ASCII 以外では
// 両者の一致を保証できない)。閾値超えの直比較経路も実行されていなかったので、ここで併せて確認する。
// Column names with no ASCII-samplable positions. Even above the threshold the hash switch cannot be used and the
// direct chain is emitted (the hash constant is computed by the generator's runtime and the match by the target
// app's, so agreement is only guaranteed for ASCII). The above-threshold direct path was also unexercised, so this
// covers it too.
internal sealed class MappingNonAsciiEntity
{
    [Name("社員番号")]
    public int EmployeeNo { get; set; } = -1;

    [Name("氏名")]
    public string FullName { get; set; } = "unset";

    [Name("氏名カナ")]
    public string FullNameKana { get; set; } = "unset";

    [Name("所属コード")]
    public string DepartmentCode { get; set; } = "unset";

    [Name("所属名称")]
    public string DepartmentName { get; set; } = "unset";

    [Name("役職コード")]
    public string PositionCode { get; set; } = "unset";

    [Name("入社年月日")]
    public string HiredOn { get; set; } = "unset";

    [Name("退社年月日")]
    public string RetiredOn { get; set; } = "unset";

    [Name("生年月日")]
    public string BornOn { get; set; } = "unset";

    [Name("性別区分")]
    public int GenderCode { get; set; } = -1;

    [Name("郵便番号")]
    public string PostalCode { get; set; } = "unset";

    [Name("住所1")]
    public string Address1 { get; set; } = "unset";

    [Name("住所2")]
    public string Address2 { get; set; } = "unset";

    [Name("電話番号")]
    public string Phone { get; set; } = "unset";

    [Name("更新日時")]
    public string UpdatedAt { get; set; } = "unset";

    [Name("更新者")]
    public string UpdatedBy { get; set; } = "unset";

    [Name("作成日時")]
    public string CreatedAt { get; set; } = "unset";
}
