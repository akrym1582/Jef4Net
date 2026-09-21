using System.Text;
using Jef4Net.Melcom;

namespace Jef4Net.Tests;

public sealed class MelcomEncodingTests
{
    [Theory]
    [InlineData("　", 0xA1, 0xA1)]
    [InlineData("あ", 0xA4, 0xA2)]
    [InlineData("漢", 0xB4, 0xC1)]
    [InlineData("Ａ", 0xA3, 0xC1)]
    public void StandardJis83RoundTrips(string text, byte first, byte second)
    {
        var encoding = new MelcomEncoding();
        Assert.Equal(new[] { first, second }, encoding.GetBytes(text));
        Assert.Equal(text, encoding.GetString(new[] { first, second }));
    }

    [Fact]
    public void JisGenerationSelectsPinnedMappingDifference()
    {
        byte[] code = { 0xA2, 0xBE };
        Assert.Equal("⊃", new MelcomEncoding(new() { JisVersion = MelcomJisVersion.Jis78 }).GetString(code));
        Assert.Equal("⊂", new MelcomEncoding(new() { JisVersion = MelcomJisVersion.Jis83 }).GetString(code));
    }

    [Fact]
    public void CustomExtensionMappingDoesNotImplyOfficialAssignment()
    {
        var map = new MelcomExtensionMapping(new[] { new KeyValuePair<ushort, int>(0x8090, 0x9AD9) });
        var encoding = new MelcomEncoding(new() { ExtensionMapping = map });
        Assert.Equal("髙", encoding.GetString(new byte[] { 0x80, 0x90 }));
        Assert.Equal(new byte[] { 0x80, 0x90 }, encoding.GetBytes("髙"));
    }

    [Fact]
    public void UnknownCodeUsesFallback()
    {
        var encoding = new MelcomEncoding(new() { DecoderFallback = DecoderFallback.ExceptionFallback });
        Assert.Throws<DecoderFallbackException>(() => encoding.GetString(new byte[] { 0x80, 0xA1 }));
    }

    [Fact]
    public void DecoderPreservesDbcsByteAcrossChunks()
    {
        Decoder decoder = new MelcomEncoding().GetDecoder();
        char[] chars = new char[2];
        Assert.Equal(0, decoder.GetChars(new byte[] { 0xB4 }, 0, 1, chars, 0, false));
        Assert.Equal(1, decoder.GetChars(new byte[] { 0xC1 }, 0, 1, chars, 0, true));
        Assert.Equal('漢', chars[0]);
    }

    [Fact]
    public void MixedProfileUsesOnlyCallerSuppliedShifts()
    {
        var encoding = new MelcomMixedEncoding(new()
        {
            SbcsEncoding = Encoding.ASCII,
            DbcsEncoding = new MelcomEncoding(),
            Shift = new() { KanjiIn = new byte[] { 0x01, 0x02 }, KanjiOut = new byte[] { 0x03 } },
        });
        byte[] expected = { 0x41, 0x01, 0x02, 0xB4, 0xC1, 0x03, 0x42 };
        Assert.Equal(expected, encoding.GetBytes("A漢B"));
        Assert.Equal("A漢B", encoding.GetString(expected));
    }
}
