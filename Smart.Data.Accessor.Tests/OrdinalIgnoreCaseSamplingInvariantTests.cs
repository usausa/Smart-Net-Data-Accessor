namespace Smart.Data.Accessor.Tests;

// 閾値超えの序数解決が emit するハッシュ switch は、次の 2 つが同時に成り立つ限りだけ
// FrozenDictionary 形と等価になる。どちらもランタイム側の性質なので、プラットフォーム更新で崩れ得る。
// 崩れた場合の症状は「列が黙ってマップされない」であり、テストが無ければ気付けない。
//
//   (A) ASCII 内で Char.ToUpperInvariant による畳み込みが OrdinalIgnoreCase の等価性と一致すること
//   (B) OrdinalIgnoreCase で ASCII 文字と等しくなる非 ASCII 文字が存在しないこと
//
// ジェネレータはサンプリング位置が全キーで ASCII になる三つ組しか選ばない。(A) によりハッシュ定数
// (ジェネレータのランタイムで計算)と実行時のハッシュが一致し、(B) により「キー側の該当位置が ASCII なら
// OrdinalIgnoreCase で一致し得る実行時文字列も同じ位置が必ず ASCII」が言える。よってハッシュが食い違うことはない。
//
// The emitted hash switch is equivalent to the FrozenDictionary form it replaced only while both of these hold.
// Both are properties of the runtime, so a platform update could break them, and the symptom would be a column
// silently failing to map:
//   (A) within ASCII, folding by Char.ToUpperInvariant agrees with OrdinalIgnoreCase equality
//   (B) no non-ASCII character is OrdinalIgnoreCase-equal to an ASCII one
// The generator only picks triples whose sampled positions are ASCII across every key. (A) makes the generation-time
// hash constant agree with the runtime hash; (B) means any runtime string that could match an ASCII-sampled key is
// itself ASCII at those positions. Together the two hashes cannot disagree.
public sealed class OrdinalIgnoreCaseSamplingInvariantTests
{
    [Fact]
    public void AsciiFoldingAgreesWithOrdinalIgnoreCase()
    {
        // (A) ASCII 同士では、ToUpperInvariant が一致することと OrdinalIgnoreCase で等しいことが同値。
        for (var a = 0; a < 0x80; a++)
        {
            for (var b = 0; b < 0x80; b++)
            {
                var equalByFolding = Char.ToUpperInvariant((char)a) == Char.ToUpperInvariant((char)b);
                var equalByComparison = String.Equals(
                    ((char)a).ToString(),
                    ((char)b).ToString(),
                    StringComparison.OrdinalIgnoreCase);

                Assert.True(
                    equalByFolding == equalByComparison,
                    $"U+{a:X4} vs U+{b:X4}: folding={equalByFolding} comparison={equalByComparison}");
            }
        }
    }

    [Fact]
    public void NoNonAsciiCharIsOrdinalIgnoreCaseEqualToAscii()
    {
        // (B) BMP 全域を総当たりする。サロゲートも符号単位として含む(単独サロゲートが ASCII と等しくならないこと)。
        // 1 文字文字列は事前に確保しておく(8.4M 回の比較になるため)。
        var ascii = new string[0x80];
        for (var a = 0; a < ascii.Length; a++)
        {
            ascii[a] = ((char)a).ToString();
        }

        for (var c = 0x80; c <= 0xFFFF; c++)
        {
            var candidate = ((char)c).ToString();
            for (var a = 0; a < ascii.Length; a++)
            {
                if (String.Equals(candidate, ascii[a], StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Fail($"U+{c:X4} is OrdinalIgnoreCase-equal to ASCII U+{a:X4}; the hash switch would miss it");
                }
            }
        }
    }

    [Fact]
    public void AstralCharsAreNotOrdinalIgnoreCaseEqualToAscii()
    {
        // サロゲートペアを構成する星幅文字も ASCII と等しくならないこと。ペアは 2 符号単位なので長さが違い、
        // 本来ここで一致し得ないが、長さ項だけに頼らず明示的に固定しておく。
        // Astral characters are two code units, so a length mismatch already rules this out; pinned explicitly rather
        // than relying on the length term alone.
        for (var codePoint = 0x10000; codePoint <= 0x1FFFF; codePoint++)
        {
            var candidate = Char.ConvertFromUtf32(codePoint);
            for (var a = 0; a < 0x80; a++)
            {
                if (String.Equals(candidate, ((char)a).ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Fail($"U+{codePoint:X5} is OrdinalIgnoreCase-equal to ASCII U+{a:X4}");
                }
            }
        }
    }
}
