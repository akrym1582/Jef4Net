using System.Text;

namespace Jef4Net.Tests;

public class BoundaryTests
{
    [Theory]
    [MemberData(nameof(EncodingTests.Names), MemberType = typeof(EncodingTests))]
    public void RandomMalformedInputIsIndependentOfChunkBoundaries(string name)
    {
        var e = EncodingTests.Get(name, false);
        var random = new Random(51723);
        for (int iteration = 0; iteration < 80; iteration++)
        {
            byte[] input = new byte[random.Next(1, 35)]; random.NextBytes(input);
            string expected = e.GetString(input);
            var decoder = e.GetDecoder(); var result = new StringBuilder();
            foreach (byte b in input) EncodingTests.Decode(decoder, new[] { b }, false, 1, result);
            EncodingTests.Decode(decoder, default, true, 1, result);
            Assert.Equal(expected, result.ToString());
        }
        string text = "\uD800あ\uDC00😀\uD801\uD802𛀙゙A\uD800";
        var encoder = e.GetEncoder(); var bytes = new List<byte>();
        foreach (char ch in text) EncodingTests.Encode(encoder, new[] { ch }, false, 1, bytes);
        EncodingTests.Encode(encoder, default, true, 1, bytes);
        Assert.Equal(e.GetBytes(text), bytes);
    }
    [Fact]
    public void ZeroCapacityDoesNotLoseBufferedInput()
    {
        var e = EncodingTests.Get("EBCDIC-Lower+JEF");
        var encoder = e.GetEncoder();
        encoder.Convert("あ".AsSpan(), Span<byte>.Empty, true, out int used, out int written, out bool completed);
        Assert.InRange(used, 0, 1); Assert.Equal(0, written); Assert.False(completed);
        var bytes = new List<byte>();
        EncodingTests.Encode(encoder, "あ".AsSpan(used), true, 1, bytes);
        Assert.Equal(e.GetBytes("あ"), bytes);
        var decoder = e.GetDecoder(); byte[] input = e.GetBytes("あ");
        decoder.Convert(input, Span<char>.Empty, true, out used, out written, out completed);
        Assert.InRange(used, 0, input.Length); Assert.Equal(0, written); Assert.False(completed);
        var chars = new StringBuilder();
        EncodingTests.Decode(decoder, input.AsSpan(used), true, 1, chars);
        Assert.Equal("あ", chars.ToString());
    }
    [Fact]
    public void NormalSpanConversionDoesNotAllocatePerCharacter()
    {
        var e = EncodingTests.Get("EBCDIC-Lower+JEF");
        string text = string.Concat(Enumerable.Repeat("aあb海𛀙゙", 200));
        byte[] bytes = new byte[e.GetByteCount(text)]; char[] chars = new char[text.Length];
        for (int i = 0; i < 20; i++) { e.GetBytes(text.AsSpan(), bytes); e.GetChars(bytes, chars); }
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10; i++) { e.GetBytes(text.AsSpan(), bytes); e.GetChars(bytes, chars); }
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
