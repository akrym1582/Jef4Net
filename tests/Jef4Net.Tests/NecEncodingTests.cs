using System.Text;
using Jef4Net.Nec;

namespace Jef4Net.Tests;

public sealed class NecEncodingTests
{
    static NecEncodingTests() => Encoding.RegisterProvider(NecEncodingProvider.Instance);

    [Fact]
    public void ProviderResolvesOnlyDocumentedNames()
    {
        string[] names =
        {
            "JIS8", "EBCDIK", "JIPSJ", "JIPSE", "JIPSJ-HanyoDenshi", "JIPSE-HanyoDenshi",
            "JIS8+JIPSJ", "JIS8+JIPSJ-HanyoDenshi", "JIPSJ+JIS8", "JIPSJ-HanyoDenshi+JIS8",
            "EBCDIK+JIPSE", "EBCDIK+JIPSE-HanyoDenshi", "JIPSE+EBCDIK", "JIPSE-HanyoDenshi+EBCDIK",
        };
        foreach (string name in names) Assert.NotNull(Encoding.GetEncoding("x-NEC-" + name.ToLowerInvariant()));
        Assert.Null(NecEncodingProvider.Instance.GetEncoding("JIPSJ"));
        Assert.Null(NecEncodingProvider.Instance.GetEncoding("x-NEC-EBCDIK+JIPSJ"));
        Assert.Null(NecEncodingProvider.Instance.GetEncoding(12345));
    }

    [Theory]
    [InlineData("x-NEC-JIS8", "A", "41")]
    [InlineData("x-NEC-EBCDIK", "A", "C1")]
    [InlineData("x-NEC-JIPSJ", "あ海", "24223324")]
    [InlineData("x-NEC-JIPSE", "あ海", "E07FF3E0")]
    [InlineData("x-NEC-JIS8+JIPSJ", "AあB", "411A7024221A7142")]
    [InlineData("x-NEC-EBCDIK+JIPSE", "AあB", "C13F75E07F3F76C2")]
    [InlineData("x-NEC-JIPSJ+JIS8", "Aあ", "1A71411A702422")]
    public void KnownVectorsRoundTrip(string name, string text, string hex)
    {
        Encoding encoding = Encoding.GetEncoding(name, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        byte[] expected = Convert.FromHexString(hex);
        Assert.Equal(expected, encoding.GetBytes(text));
        Assert.Equal(text, encoding.GetString(expected));
    }

    [Fact]
    public void PrivateUseRangesRoundTrip()
    {
        Encoding encoding = Encoding.GetEncoding("x-NEC-JIPSJ", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        Assert.Equal(new byte[] { 0x74, 0x21, 0x7E, 0x7E, 0xE0, 0xA1, 0xFE, 0xFE }, encoding.GetBytes("\uE000\uE409\uE40A\uEF6B"));
        Assert.Equal("\uE000\uE409\uE40A\uEF6B", encoding.GetString(new byte[] { 0x74, 0x21, 0x7E, 0x7E, 0xE0, 0xA1, 0xFE, 0xFE }));
    }

    [Fact]
    public void HanyoDenshiEmitsIvsWhileNormalSuppressesIt()
    {
        Encoding normal = Encoding.GetEncoding("x-NEC-JIPSJ", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        Encoding hd = Encoding.GetEncoding("x-NEC-JIPSJ-HanyoDenshi", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        byte[] bytes = { 0x30, 0x3B };
        Assert.Equal("\u98F4", normal.GetString(bytes));
        Assert.Equal("\u98F4\U000E0103", hd.GetString(bytes));
        Assert.Equal(bytes, hd.GetBytes("\u98F4\U000E0103"));
    }

    [Fact]
    public void SplitShiftAndLeadAreBuffered()
    {
        Encoding encoding = Encoding.GetEncoding("x-NEC-JIS8+JIPSJ", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        Decoder decoder = encoding.GetDecoder(); char[] chars = new char[1];
        Assert.Equal(0, decoder.GetChars(new byte[] { 0x1A }, 0, 1, chars, 0, false));
        Assert.Equal(0, decoder.GetChars(new byte[] { 0x70, 0x24 }, 0, 2, chars, 0, false));
        Assert.Equal(1, decoder.GetChars(new byte[] { 0x22 }, 0, 1, chars, 0, true));
        Assert.Equal('あ', chars[0]);
    }
}
