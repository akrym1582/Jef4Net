using System.Text;
using Jef4Net.Unisys;

namespace Jef4Net.Tests;

public sealed class JbisEncodingTests
{
    static JbisEncodingTests() => Encoding.RegisterProvider(JbisEncodingProvider.Instance);

    [Theory]
    [InlineData("jbis7", "2121212221252126A24FA250", "　、．・˘ˇ")]
    [InlineData("jbis8", "A1A1A1A2A1A5A1A6A24FA250", "　、．・˘ˇ")]
    public void PureKnownVectorsRoundTrip(string name, string hex, string text)
    {
        Encoding encoding = Strict(name); byte[] bytes = Convert.FromHexString(hex);
        Assert.Equal(text, encoding.GetString(bytes)); Assert.Equal(bytes, encoding.GetBytes(text));
    }

    [Theory]
    [InlineData("jis-ascii-jbis7", "4142439E467C4B5C9F444546", "ABC日本DEF")]
    [InlineData("japan-ebcdic-jbis8", "C1C2C32BC6FCCBDC2CC4C5C6", "ABC日本DEF")]
    [InlineData("japan-v24-jbis8", "C1C2C32BC6FCCBDC2CC4C5C6", "ABC日本DEF")]
    public void MixedProfilesShiftOnceAndCloseAtFlush(string name, string hex, string text)
    {
        Encoding encoding = Strict(name); byte[] expected = Convert.FromHexString(hex);
        Assert.Equal(expected, encoding.GetBytes(text)); Assert.Equal(text, encoding.GetString(expected));
    }

    [Fact]
    public void ProviderAndConcreteTypesExposeAllProfiles()
    {
        Assert.IsType<Jbis7Encoding>(Encoding.GetEncoding("JBIS7"));
        Assert.IsType<Jbis8Encoding>(Encoding.GetEncoding("jbis8"));
        Assert.IsType<JisAsciiJbis7Encoding>(Encoding.GetEncoding("jis-ascii-jbis7"));
        Assert.IsType<JapanEbcdicJbis8Encoding>(Encoding.GetEncoding("japan-ebcdic-jbis8"));
        Assert.IsType<JapanV24Jbis8Encoding>(Encoding.GetEncoding("japan-v24-jbis8"));
        Assert.Null(JbisEncodingProvider.Instance.GetEncoding(12345));
    }

    [Fact]
    public void StreamingDecoderBuffersPairAndRequiresEdo()
    {
        Decoder decoder = Strict("jis-ascii-jbis7").GetDecoder(); char[] output = new char[1];
        Assert.Equal(0, decoder.GetChars(new byte[] { 0x9E, 0x24 }, 0, 2, output, 0, false));
        Assert.Equal(1, decoder.GetChars(new byte[] { 0x22, 0x9F }, 0, 2, output, 0, true)); Assert.Equal('あ', output[0]);
        Assert.Throws<DecoderFallbackException>(() => Strict("jis-ascii-jbis7").GetString(new byte[] { 0x9E, 0x24, 0x22 }));
    }

    [Fact]
    public void ShiftByteCanBeAValidDbcsTrail()
    {
        Encoding encoding = Strict("jis-ascii-jbis7"); byte[] bytes = { 0x9E, 0xA7, 0x9E, 0x9F };
        Assert.Equal("\u045F", encoding.GetString(bytes)); Assert.Equal(bytes, encoding.GetBytes("\u045F"));

        for (int split = 0; split <= bytes.Length; split++)
        {
            Decoder decoder = encoding.GetDecoder(); char[] output = new char[1];
            int count = decoder.GetChars(bytes, 0, split, output, 0, false);
            count += decoder.GetChars(bytes, split, bytes.Length - split, output, count, true);
            Assert.Equal(1, count); Assert.Equal('\u045F', output[0]);
        }
    }

    [Theory]
    [InlineData("jbis7", "21")]
    [InlineData("jbis8", "A1")]
    [InlineData("jbis7", "41A1")]
    [InlineData("jis-ascii-jbis7", "9F")]
    [InlineData("jis-ascii-jbis7", "9E9E")]
    public void InvalidAndCustomSequencesUseFallback(string name, string hex)
        => Assert.Throws<DecoderFallbackException>(() => Strict(name).GetString(Convert.FromHexString(hex)));

    [Fact]
    public void EveryMappedPureCodeRoundTrips()
    {
        foreach ((string name, int leadStart, int trailStart) in new[] { ("jbis7", 0x21, 0x21), ("jbis8", 0xA1, 0xA1) })
        {
            Encoding encoding = Strict(name);
            for (int lead = leadStart; lead < leadStart + 94; lead++) for (int trail = trailStart; trail < trailStart + 94; trail++)
            {
                byte[] bytes = { (byte)lead, (byte)trail };
                try { string text = encoding.GetString(bytes); Assert.Equal(bytes, encoding.GetBytes(text)); } catch (DecoderFallbackException) { }
            }
        }
    }

    private static Encoding Strict(string name) => Encoding.GetEncoding(name, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
}
