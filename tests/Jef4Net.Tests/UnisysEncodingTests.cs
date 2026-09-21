using System.Text;
using Jef4Net.Unisys;

namespace Jef4Net.Tests;

public sealed class UnisysEncodingTests
{
    static UnisysEncodingTests() => Encoding.RegisterProvider(UnisysEncodingProvider.Instance);

    [Fact]
    public void ProviderResolvesOnlyCanonicalNames()
    {
        Assert.NotNull(Encoding.GetEncoding("x-unisys-letsj"));
        Assert.NotNull(Encoding.GetEncoding("X-UNISYS-LETSJ-KANJI"));
        Assert.Null(UnisysEncodingProvider.Instance.GetEncoding("LETSJ"));
        Assert.Null(UnisysEncodingProvider.Instance.GetEncoding(12345));
    }

    [Theory]
    [InlineData("ABCｱ", "414243B1")]
    [InlineData("’‘\\~", "27605C7E")]
    [InlineData("ABCあ,DEF", "4142439370A4A293F12C444546")]
    [InlineData("凜熙", "9370F4A5F4A6")]
    public void MixedKnownVectorsRoundTrip(string text, string hex)
    {
        Encoding encoding = Strict("x-Unisys-LETSJ");
        byte[] bytes = Convert.FromHexString(hex);
        Assert.Equal(bytes, encoding.GetBytes(text));
        Assert.Equal(text, encoding.GetString(bytes));
    }

    [Theory]
    [InlineData("あ", "A4A2")]
    [InlineData("\u3000", "2020")]
    [InlineData("凜熙", "F4A5F4A6")]
    public void KanjiKnownVectorsRoundTrip(string text, string hex)
    {
        Encoding encoding = Strict("x-Unisys-LETSJ-Kanji");
        byte[] bytes = Convert.FromHexString(hex);
        Assert.Equal(bytes, encoding.GetBytes(text));
        Assert.Equal(text, encoding.GetString(bytes));
    }

    [Theory]
    [InlineData(0x70)] [InlineData(0x72)] [InlineData(0xA0)]
    public void EveryRepresentativeEvenShiftEntersDbcs(byte shift)
        => Assert.Equal("あ", Strict("x-Unisys-LETSJ").GetString(new byte[] { 0x93, shift, 0xA4, 0xA2 }));

    [Theory]
    [InlineData(0x71)] [InlineData(0xF1)] [InlineData(0xFF)]
    public void EveryRepresentativeOddShiftEntersSbcs(byte shift)
        => Assert.Equal("A", Strict("x-Unisys-LETSJ").GetString(new byte[] { 0x93, 0x70, 0x93, shift, 0x41 }));

    [Fact]
    public void EncoderDoesNotReturnToSbcsAtEnd()
        => Assert.Equal(Convert.FromHexString("4142439370A4A2"), Strict("x-Unisys-LETSJ").GetBytes("ABCあ"));

    [Fact]
    public void SplitShiftAndDbcsLeadAreBufferedAndResetRestoresSbcs()
    {
        Decoder decoder = Strict("x-Unisys-LETSJ").GetDecoder(); char[] chars = new char[1];
        Assert.Equal(0, decoder.GetChars(new byte[] { 0x93 }, 0, 1, chars, 0, false));
        Assert.Equal(0, decoder.GetChars(new byte[] { 0x70, 0xA4 }, 0, 2, chars, 0, false));
        Assert.Equal(1, decoder.GetChars(new byte[] { 0xA2 }, 0, 1, chars, 0, true));
        Assert.Equal('あ', chars[0]); decoder.Reset();
        Assert.Equal(1, decoder.GetChars(new byte[] { 0x41 }, 0, 1, chars, 0, true));
        Assert.Equal('A', chars[0]);
    }

    [Fact]
    public void CustomerDefinedAndUndefinedSequencesUseFallback()
    {
        Encoding encoding = Strict("x-Unisys-LETSJ-Kanji");
        Assert.Throws<DecoderFallbackException>(() => encoding.GetString(new byte[] { 0x21, 0xA1 }));
        Assert.Throws<DecoderFallbackException>(() => encoding.GetString(new byte[] { 0xA1 }));
        Assert.Throws<EncoderFallbackException>(() => encoding.GetBytes("😀"));
    }

    private static Encoding Strict(string name) => Encoding.GetEncoding(name, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
}
