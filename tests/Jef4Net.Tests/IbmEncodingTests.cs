using System.Text;
using Jef4Net.Ibm;

namespace Jef4Net.Tests;

public sealed class IbmEncodingTests
{
    static IbmEncodingTests() => Encoding.RegisterProvider(IbmEncodingProvider.Instance);

    [Fact]
    public void ProviderResolvesComponentsMixedAliasesAndCodePages()
    {
        foreach (string name in new[] { "x-IBM-8482", "x-IBM-5123", "x-IBM-16684", "x-IBM-8482+16684", "x-IBM-16684+8482", "x-IBM-5123+16684", "x-IBM-16684+5123", "x-IBM-1390", "x-IBM-1399", "x-IBM-11684", "x-IBM-8482+11684" })
            Assert.NotNull(Encoding.GetEncoding(name));
        foreach (int codepage in new[] { 1390, 1399, 8482, 5123, 16684 }) Assert.NotNull(IbmEncodingProvider.Instance.GetEncoding(codepage));
        Assert.Equal("x-IBM-8482+16684", Encoding.GetEncoding("x-IBM-1390").WebName);
        Assert.Equal("x-IBM-5123+16684", Encoding.GetEncoding("x-IBM-1399").WebName);
    }

    [Theory]
    [InlineData("x-IBM-1390", "A　B", "C10E40400FC2")]
    [InlineData("x-IBM-1390", "AあB", "C10E44810FC2")]
    [InlineData("x-IBM-8482", "Aｱ€", "C181E1")]
    [InlineData("x-IBM-16684", "　あ\uE000", "404044816941")]
    [InlineData("x-IBM-16684", "\U0002000Bæ̀", "B342ECC3")]
    public void KnownVectorsRoundTrip(string name, string text, string hex)
    {
        Encoding encoding = Encoding.GetEncoding(name, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        byte[] expected = Convert.FromHexString(hex);
        Assert.Equal(expected, encoding.GetBytes(text));
        Assert.Equal(text, encoding.GetString(expected));
    }

    [Fact]
    public void MixedDecoderAndEncoderWorkAcrossEveryInputSplit()
    {
        Encoding encoding = Encoding.GetEncoding("x-IBM-1390", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        byte[] bytes = Convert.FromHexString("C10E40400FC2"); string text = "A　B";
        for (int split = 0; split <= bytes.Length; split++)
        {
            Decoder decoder = encoding.GetDecoder(); var chars = new char[8];
            int count = decoder.GetChars(bytes, 0, split, chars, 0, false);
            count += decoder.GetChars(bytes, split, bytes.Length - split, chars, count, true);
            Assert.Equal(text, new string(chars, 0, count));
        }
        for (int split = 0; split <= text.Length; split++)
        {
            Encoder encoder = encoding.GetEncoder(); var output = new byte[16];
            int count = encoder.GetBytes(text.ToCharArray(), 0, split, output, 0, false);
            count += encoder.GetBytes(text.ToCharArray(), split, text.Length - split, output, count, true);
            Assert.Equal(bytes, output[..count]);
        }
    }

    [Fact]
    public void SequenceAndSurrogateAreBufferedAndFlushReturnsToInitialState()
    {
        Encoding encoding = Encoding.GetEncoding("x-IBM-1390", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        Encoder encoder = encoding.GetEncoder(); var bytes = new byte[16];
        Assert.Equal(0, encoder.GetBytes(new[] { 'æ' }, 0, 1, bytes, 0, false));
        int count = encoder.GetBytes(new[] { '\u0300' }, 0, 1, bytes, 0, true);
        Assert.Equal(Convert.FromHexString("0EECC30F"), bytes[..count]);
        encoder.Reset();
        Assert.Equal(0, encoder.GetBytes(new[] { '\uD840' }, 0, 1, bytes, 0, false));
        count = encoder.GetBytes(new[] { '\uDC0B' }, 0, 1, bytes, 0, true);
        Assert.Equal(Convert.FromHexString("0EB3420F"), bytes[..count]);
    }

    [Fact]
    public void ShiftRulesAndMalformedDbcsUseConfiguredFallback()
    {
        Encoding mixed = Encoding.GetEncoding("x-IBM-1390", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        Assert.Equal("A", mixed.GetString(Convert.FromHexString("0E0E0F0FC1")));
        Assert.Throws<DecoderFallbackException>(() => mixed.GetString(Convert.FromHexString("0E44")));
        Encoding dbcs = Encoding.GetEncoding("x-IBM-16684", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        Assert.Throws<DecoderFallbackException>(() => dbcs.GetString(new byte[] { 0x0E }));
    }

    [Theory]
    [InlineData("ibm-1390_P110-2003.ucm", "x-IBM-8482")]
    [InlineData("ibm-1399_P110-2003.ucm", "x-IBM-5123")]
    public void EveryCanonicalAndDecodeOnlyUcmMappingIsAvailable(string file, string sbcsName)
    {
        Encoding sbcs = Encoding.GetEncoding(sbcsName, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        Encoding dbcs = Encoding.GetEncoding("x-IBM-16684", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        foreach (string line in File.ReadLines(Path.Combine(AppContext.BaseDirectory, "data", "icu", "ibm", file)))
        {
            if (!line.StartsWith("<U", StringComparison.Ordinal) || !(line.EndsWith("|0", StringComparison.Ordinal) || line.EndsWith("|3", StringComparison.Ordinal))) continue;
            int separator = line.IndexOf(" \\x", StringComparison.Ordinal); if (separator < 0) continue;
            string unicodePart = line[..separator]; string bytePart = line[(separator + 1)..line.LastIndexOf(' ')];
            var scalars = new List<int>();
            foreach (string token in unicodePart.Split(new[] { "<U", ">" }, StringSplitOptions.RemoveEmptyEntries)) scalars.Add(Convert.ToInt32(token, 16));
            string text = string.Concat(scalars.Select(char.ConvertFromUtf32));
            byte[] expected = bytePart.Split(new[] { "\\x" }, StringSplitOptions.RemoveEmptyEntries).Select(x => Convert.ToByte(x, 16)).ToArray();
            Encoding encoding = expected.Length == 1 ? sbcs : dbcs;
            Assert.Equal(text, encoding.GetString(expected));
            if (line.EndsWith("|0", StringComparison.Ordinal)) Assert.Equal(expected, encoding.GetBytes(text));
        }
    }
}
