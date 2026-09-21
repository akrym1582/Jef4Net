using System.Text;
using Jef4Net.Hitachi;

namespace Jef4Net.Tests;

public sealed class HitachiEncodingTests
{
    static HitachiEncodingTests() => Encoding.RegisterProvider(HitachiEncodingProvider.Instance);

    [Fact]
    public void ProviderResolvesCanonicalNamesAndNoAliases()
    {
        string[] sbcs = { "EBCDIC", "EBCDIK" };
        string[] keis = { "KEIS78", "KEIS83", "KEIS78-ShiftSpaceSingle", "KEIS83-ShiftSpaceSingle" };
        foreach (string s in sbcs) Assert.NotNull(Encoding.GetEncoding("x-Hitachi-" + s));
        foreach (string k in keis)
        {
            Assert.NotNull(Encoding.GetEncoding("x-Hitachi-" + k.ToLowerInvariant()));
            foreach (string s in sbcs)
            {
                Assert.NotNull(Encoding.GetEncoding("x-Hitachi-" + s + "+" + k));
                Assert.NotNull(Encoding.GetEncoding("x-Hitachi-" + k + "+" + s));
            }
        }
        Assert.Null(HitachiEncodingProvider.Instance.GetEncoding("KEIS83"));
        Assert.Null(HitachiEncodingProvider.Instance.GetEncoding("x-Hitachi-KEIS78-HanyoDenshi"));
        Assert.Null(HitachiEncodingProvider.Instance.GetEncoding("x-Hitachi-EBCDIC+KEIS83-HanyoDenshi"));
        Assert.Null(HitachiEncodingProvider.Instance.GetEncoding(12345));
    }

    [Fact]
    public void ConflictingEbcdicAliasIsNotSilentlyEncoded()
    {
        Encoding encoding = Encoding.GetEncoding("x-Hitachi-EBCDIC", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        Assert.Equal("\uFF89", encoding.GetString(new byte[] { 0x8F }));
        Assert.Equal(new byte[] { 0x8F }, encoding.GetBytes("\uFF89"));
        Assert.Throws<EncoderFallbackException>(() => encoding.GetBytes("\uFF88"));
    }

    [Theory]
    [InlineData("x-Hitachi-EBCDIC", "a", "81")]
    [InlineData("x-Hitachi-EBCDIK", "a", "59")]
    [InlineData("x-Hitachi-KEIS78", "あ海", "A4A2B3A4")]
    [InlineData("x-Hitachi-KEIS83", "あ海", "A4A2B3A4")]
    [InlineData("x-Hitachi-EBCDIC+KEIS83", "aあb", "810A42A4A20A4182")]
    [InlineData("x-Hitachi-KEIS83+EBCDIC", "aあ", "0A41810A42A4A2")]
    public void KnownVectorsRoundTrip(string name, string text, string hex)
    {
        Encoding encoding = Encoding.GetEncoding(name, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        byte[] expected = Convert.FromHexString(hex);
        Assert.Equal(expected, encoding.GetBytes(text));
        Assert.Equal(text, encoding.GetString(expected));
    }

    [Fact]
    public void ShiftSpaceSingleAndPrivateUseArea()
    {
        Encoding normal = Encoding.GetEncoding("x-Hitachi-KEIS83");
        Encoding shifted = Encoding.GetEncoding("x-Hitachi-KEIS83-ShiftSpaceSingle");
        Assert.Equal("\u3000", normal.GetString(new byte[] { 0x40, 0x40 }));
        Assert.Equal("  ", shifted.GetString(new byte[] { 0x40, 0x40 }));
        Assert.Equal(new byte[] { 0x81, 0xA1, 0xA0, 0xFE }, normal.GetBytes("\uE000\uEBBF"));
        Assert.Equal("\uE000\uEBBF", normal.GetString(new byte[] { 0x81, 0xA1, 0xA0, 0xFE }));
    }

    [Fact]
    public void SplitShiftAndLeadAreBufferedAndResetWorks()
    {
        Encoding encoding = Encoding.GetEncoding("x-Hitachi-EBCDIC+KEIS83", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        Decoder decoder = encoding.GetDecoder();
        char[] chars = new char[2];
        Assert.Equal(0, decoder.GetChars(new byte[] { 0x0A }, 0, 1, chars, 0, false));
        Assert.Equal(0, decoder.GetChars(new byte[] { 0x42, 0xA4 }, 0, 2, chars, 0, false));
        Assert.Equal(1, decoder.GetChars(new byte[] { 0xA2 }, 0, 1, chars, 0, true));
        Assert.Equal('あ', chars[0]);
        decoder.Reset();
        Assert.Equal(1, decoder.GetChars(new byte[] { 0x81 }, 0, 1, chars, 0, true));
        Assert.Equal('a', chars[0]);
    }
}
