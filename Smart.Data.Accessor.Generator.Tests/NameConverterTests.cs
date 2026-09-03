namespace Smart.Data.Accessor.Generator.Tests;

using Smart.Data.Accessor.Shared.Helpers;

// NameConverter([Naming] 規約の既定名変換)の単体テスト。snake_case の語分割は System.Text.Json の
// JsonNamingPolicy と同じ規則(小文字/数字→大文字の境界、大文字連続+小文字は最後の大文字の前、既存
// アンダースコアは維持)で、変換は冪等でなければならない。
// Unit tests for NameConverter (the [Naming] default-name conversion). The snake_case word splitting follows
// the same rules as System.Text.Json's JsonNamingPolicy (boundary at lower/digit -> upper, an uppercase run
// followed by lowercase splits before its last upper, existing underscores are kept), and the conversion must
// be idempotent.
public sealed class NameConverterTests
{
    [Theory]
    [InlineData("UserId", "user_id")]
    [InlineData("UserID", "user_id")]
    [InlineData("HTMLParser", "html_parser")]
    [InlineData("IOStream", "io_stream")]
    [InlineData("XMLHttpRequest", "xml_http_request")]
    [InlineData("Sha512Hash", "sha512_hash")]
    [InlineData("User_Id", "user_id")]
    [InlineData("user_id", "user_id")]
    [InlineData("ABC", "abc")]
    [InlineData("A", "a")]
    [InlineData("名前", "名前")]
    [InlineData("名前Id", "名前_id")]
    public void ConvertsSnakeCaseLower(string name, string expected)
        => Assert.Equal(expected, NameConverter.Convert(name, NamingConvention.SnakeCaseLower));

    [Theory]
    [InlineData("UserId", "USER_ID")]
    [InlineData("USER_ID", "USER_ID")]
    [InlineData("Sha512Hash", "SHA512_HASH")]
    [InlineData("IOStream", "IO_STREAM")]
    public void ConvertsSnakeCaseUpper(string name, string expected)
        => Assert.Equal(expected, NameConverter.Convert(name, NamingConvention.SnakeCaseUpper));

    [Theory]
    [InlineData("UserId", "userid")]
    [InlineData("USER_ID", "user_id")]
    [InlineData("名前", "名前")]
    public void ConvertsLowerCase(string name, string expected)
        => Assert.Equal(expected, NameConverter.Convert(name, NamingConvention.LowerCase));

    [Theory]
    [InlineData("UserId", "USERID")]
    [InlineData("user_id", "USER_ID")]
    public void ConvertsUpperCase(string name, string expected)
        => Assert.Equal(expected, NameConverter.Convert(name, NamingConvention.UpperCase));

    [Fact]
    public void NoneKeepsName()
        => Assert.Equal("UserId", NameConverter.Convert("UserId", NamingConvention.None));
}
