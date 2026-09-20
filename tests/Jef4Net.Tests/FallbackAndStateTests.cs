using System.Text;
using Jef4Net.Fujitsu;

namespace Jef4Net.Tests;

public class FallbackAndStateTests
{
    [Theory]
    [InlineData("JEF")]
    [InlineData("EBCDIC-Lower+JEF")]
    [InlineData("JEF+EBCDIC-Lower")]
    public void DefaultReplacementAndStrict(string name)
    {
        var e = EncodingTests.Get(name, false);
        Assert.Equal("\u3000\u3000", e.GetString(e.GetBytes("😀")));
        Assert.Equal("\u3000", e.GetString(e.GetBytes("\uD800")));
        var strict = EncodingTests.Get(name);
        var ex = Assert.Throws<EncoderFallbackException>(() => strict.GetBytes("😀"));
        Assert.True(ex.IsUnknownSurrogate()); Assert.Equal(0, ex.Index);
        var dex = Assert.Throws<DecoderFallbackException>(() => strict.GetString(new byte[] { 0x41 }));
        Assert.Equal(new byte[] { 0x41 }, dex.BytesUnknown);
        Assert.Equal("\uFFFD", e.GetString(new byte[] { 0x41 }));
    }
    [Fact]
    public void ReplacementContracts()
    {
        var e = FujitsuEncodingProvider.Instance.GetEncoding("EBCDIC-Lower+JEF", new EncoderReplacementFallback("aあ"), new DecoderReplacementFallback("[bad]"))!;
        Assert.Equal("海aあaあ海", e.GetString(e.GetBytes("海😀海")));
        Assert.Equal("[bad]", e.GetString(new byte[] { 0x41 }));
        var bytes = new List<byte>();
        EncodingTests.Encode(e.GetEncoder(), "海😀海", true, 1, bytes);
        Assert.Equal(e.GetBytes("海😀海"), bytes);
        var chars = new StringBuilder();
        EncodingTests.Decode(e.GetDecoder(), new byte[] { 0x41, 0x81 }, true, 1, chars);
        Assert.Equal("[bad]a", chars.ToString());
        var recursive = FujitsuEncodingProvider.Instance.GetEncoding("JEF", new EncoderReplacementFallback("?"), DecoderFallback.ExceptionFallback)!;
        Assert.Throws<ArgumentException>(() => recursive.GetBytes("😀"));
        var empty = FujitsuEncodingProvider.Instance.GetEncoding("JEF", new EncoderReplacementFallback(""), new DecoderReplacementFallback(""))!;
        Assert.Empty(empty.GetBytes("😀")); Assert.Equal("", empty.GetString(new byte[] { 0x41 }));
    }
    [Fact]
    public void K2AcrossEverySplitAndMalformedK2()
    {
        var e = EncodingTests.Get("EBCDIC-Lower+JEF");
        byte[] vector = Convert.FromHexString("8130E2A4A22982");
        for (int i = 0; i <= vector.Length; i++)
        {
            var decoder = e.GetDecoder(); var result = new StringBuilder();
            EncodingTests.Decode(decoder, vector.AsSpan(0, i), false, 1, result);
            EncodingTests.Decode(decoder, vector.AsSpan(i), true, 1, result);
            Assert.Equal("aあb", result.ToString());
        }
        Assert.Throws<DecoderFallbackException>(() => e.GetString(new byte[] { 0x30 }));
        Assert.Equal("�a", EncodingTests.Get("EBCDIC-Lower+JEF", false).GetString(new byte[] { 0x30, 0x81 }));
    }
    [Fact]
    public void ResetAndIndependentInstances()
    {
        var e = EncodingTests.Get("EBCDIC-Lower+JEF");
        var one = e.GetEncoder(); var two = e.GetEncoder();
        byte[] bytes = new byte[32]; char[] chars = new char[32];
        Assert.Equal(3, one.GetBytes("あ".AsSpan(), bytes, false));
        Assert.Equal(1, two.GetBytes("a".AsSpan(), bytes, true));
        Assert.Equal((byte)0x81, bytes[0]);
        one.Reset(); Assert.Equal(1, one.GetBytes("a".AsSpan(), bytes, true));
        one.Convert("\uD800".AsSpan(), bytes, false, out int used, out int written, out bool completed);
        Assert.Equal(1, used); Assert.Equal(0, written); Assert.True(completed);
        one.Reset(); Assert.Equal(1, one.GetBytes("a".AsSpan(), bytes, true));
        var decoder = e.GetDecoder();
        decoder.Convert(new byte[] { 0x28, 0xA4 }, chars, false, out used, out written, out completed);
        Assert.Equal(2, used); Assert.Equal(0, written); Assert.True(completed);
        Assert.Equal(1, decoder.GetCharCount(new byte[] { 0xA2 }, 0, 1, true));
        Assert.Equal(1, decoder.GetChars(new byte[] { 0xA2 }, 0, 1, chars, 0, true));
        Assert.Equal('あ', chars[0]);
        decoder.Reset(); Assert.Equal(1, decoder.GetChars(new byte[] { 0x81 }, 0, 1, chars, 0, true));
        Assert.Equal('a', chars[0]);
    }
    [Fact]
    public void CountDoesNotMutateAndInsufficientGetDoesNotLoseState()
    {
        var e = EncodingTests.Get("EBCDIC-Lower+JEF"); var encoder = e.GetEncoder();
        Assert.Equal(4, encoder.GetByteCount("あ".AsSpan(), true));
        Assert.Equal(4, encoder.GetByteCount("あ".AsSpan(), true));
        Assert.Throws<ArgumentException>(() => encoder.GetBytes("あ".AsSpan(), new byte[2], true));
        byte[] buffer = new byte[4];
        Assert.Equal(4, encoder.GetBytes("あ".AsSpan(), buffer, true));
        Assert.Equal(Convert.FromHexString("28A4A229"), buffer);
        var decoder = e.GetDecoder();
        decoder.GetChars(new byte[] { 0x28, 0xA4 }, 0, 2, new char[2], 0, false);
        Assert.Throws<ArgumentException>(() => decoder.GetChars(new byte[] { 0xA2 }, 0, 1, Array.Empty<char>(), 0, true));
        char[] chars = new char[1];
        Assert.Equal(1, decoder.GetChars(new byte[] { 0xA2 }, 0, 1, chars, 0, true));
        Assert.Equal('あ', chars[0]);
    }
    [Fact]
    public void ExactlySizedStatefulBuffersProcessTrailingShifts()
    {
        var decoder = EncodingTests.Get("EBCDIC-Lower+JEF").GetDecoder();
        char[] chars = new char[1];
        Assert.Equal(1, decoder.GetChars(Convert.FromHexString("28A4A229"), 0, 4, chars, 0, false));
        Assert.Equal(1, decoder.GetChars(new byte[] { 0x81 }, 0, 1, chars, 0, true));
        Assert.Equal('a', chars[0]);
    }
    [Fact]
    public void EmptyFallbackStillConsumesInGetMethods()
    {
        var e = FujitsuEncodingProvider.Instance.GetEncoding("JEF", new EncoderReplacementFallback(""), new DecoderReplacementFallback(""))!;
        var enc = e.GetEncoder();
        enc.GetBytes("\uD800".AsSpan(), Span<byte>.Empty, false);
        enc.GetBytes("\uDC00".AsSpan(), Span<byte>.Empty, true);
        enc.Fallback = EncoderFallback.ExceptionFallback;
        Assert.Equal(0, enc.GetByteCount(ReadOnlySpan<char>.Empty, true));
        var dec = e.GetDecoder();
        dec.GetChars(new byte[] { 0xA4 }, Span<char>.Empty, false);
        dec.GetChars(ReadOnlySpan<byte>.Empty, Span<char>.Empty, true);
        dec.Fallback = DecoderFallback.ExceptionFallback;
        Assert.Equal(0, dec.GetCharCount(ReadOnlySpan<byte>.Empty, true));
    }
}
