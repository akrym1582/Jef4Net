using System.Globalization;
using System.Text;
using System.Text.Json;
using Jef4Net.Fujitsu;

namespace Jef4Net.Tests;

public class EncodingTests
{
    internal static Encoding Get(string suffix, bool strict = true) => strict
        ? FujitsuEncodingProvider.Instance.GetEncoding("x-Fujitsu-" + suffix, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback)!
        : FujitsuEncodingProvider.Instance.GetEncoding("x-Fujitsu-" + suffix)!;
    public static IEnumerable<object[]> Names()
    {
        yield return new object[] { "JEF" };
        yield return new object[] { "JEF-Roundtrip" };
        yield return new object[] { "JEF-HanyoDenshi" };
        foreach (string k in new[] { "Lower", "Kana", "Ascii" })
        {
            yield return new object[] { "EBCDIC-" + k };
            yield return new object[] { "EBCDIC-" + k + "+JEF" };
            yield return new object[] { "JEF+EBCDIC-" + k };
            yield return new object[] { "EBCDIC-" + k + "+JEF-HanyoDenshi" };
            yield return new object[] { "JEF-HanyoDenshi+EBCDIC-" + k };
        }
    }
    [Fact]
    public void ProviderAndArguments()
    {
        Encoding.RegisterProvider(FujitsuEncodingProvider.Instance);
        foreach (object[] row in Names())
        {
            string name = "x-Fujitsu-" + row[0];
            Assert.Equal(name, Encoding.GetEncoding(name.ToLowerInvariant()).WebName);
            Assert.Empty(Encoding.GetEncoding(name).GetPreamble());
        }
        Assert.Null(FujitsuEncodingProvider.Instance.GetEncoding(0));
        Assert.Null(FujitsuEncodingProvider.Instance.GetEncoding("JEF-Roundtrip+EBCDIC-Lower"));
        Assert.Null(FujitsuEncodingProvider.Instance.GetEncoding("JEF-Roundtrip-HanyoDenshi"));
        Assert.Throws<ArgumentNullException>(() => FujitsuEncodingProvider.Instance.GetEncoding(null!));
        var e = Get("JEF");
        Assert.Throws<ArgumentNullException>(() => e.GetByteCount((string)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => e.GetByteCount(new char[1], -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => e.GetMaxByteCount(int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => e.GetMaxCharCount(-1));
    }
    [Fact]
    public void KnownVectors()
    {
        var e = Get("EBCDIC-Lower+JEF");
        byte[] expected = Convert.FromHexString("8128A4A2298228B3A42983");
        Assert.Equal(expected, e.GetBytes("aあb海c"));
        Assert.Equal("aあb海c", e.GetString(expected));
        Assert.Equal("あ", e.GetString(Convert.FromHexString("38A4A229")));
        Assert.Equal("あ", e.GetString(Convert.FromHexString("30E2A4A229")));
        Assert.Equal("あ", e.GetString(Convert.FromHexString("28283830E2A4A22929")));
        Assert.Equal(Convert.FromHexString("28A4A229"), e.GetBytes("あ"));
        Assert.Equal(Convert.FromHexString("298128"), Get("JEF+EBCDIC-Lower").GetBytes("a"));
        Assert.Equal("\uFA30", Get("JEF").GetString(Convert.FromHexString("42BB")));
        Assert.Equal(Convert.FromHexString("C9EE"), Get("JEF").GetBytes("侮"));
        Assert.Equal(Convert.FromHexString("B3EC"), Get("JEF").GetBytes("\uFA60"));
        Assert.Equal("\u3000", Get("JEF").GetString(Convert.FromHexString("A1A1")));
    }
    [Theory]
    [InlineData("JEF", "jef", 65536)]
    [InlineData("EBCDIC-Lower", "ebcdic_lower", 256)]
    [InlineData("EBCDIC-Kana", "ebcdic_kana", 256)]
    [InlineData("EBCDIC-Ascii", "ebcdic_ascii", 256)]
    public void AllMappingsAndEveryCode(string name, string file, int size)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "fujitsu_" + file + "_mapping.json")));
        var decode = new Dictionary<int, string>();
        var encode = new Dictionary<string, int>();
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            string[] opts = row.GetProperty("options").EnumerateArray().Select(o => o.GetString()!).ToArray();
            if (row.TryGetProperty("hd", out _) || row.TryGetProperty("aj1", out _) || opts.Contains("unmappable")) continue;
            int code = int.Parse(row.GetProperty("code").GetString()!, NumberStyles.HexNumber);
            string text = char.ConvertFromUtf32(int.Parse(row.GetProperty("unicode").GetString()!, NumberStyles.HexNumber));
            if (row.TryGetProperty("sp", out var sp)) text += char.ConvertFromUtf32(int.Parse(sp.GetString()!, NumberStyles.HexNumber));
            if (!opts.Contains("encode_only")) decode[code] = text;
            if (!opts.Contains("decode_only")) encode[text] = code;
        }
        if (size == 65536)
            for (int i = 0; i <= 0xC1D; i++)
            {
                int code = ((0x80 + i / 94) << 8) | (0xA1 + i % 94);
                decode[code] = char.ConvertFromUtf32(0xE000 + i);
                encode[decode[code]] = code;
            }
        var e = Get(name);
        for (int code = 0; code < size; code++)
        {
            byte[] bytes = size == 256 ? [(byte)code] : [(byte)(code >> 8), (byte)code];
            if (decode.TryGetValue(code, out string? text))
            {
                Assert.Equal(text, e.GetString(bytes));
                Assert.Equal(text.Length, e.GetCharCount(bytes));
            }
            else Assert.Throws<DecoderFallbackException>(() => e.GetString(bytes));
        }
        foreach (var (text, code) in encode)
        {
            byte[] bytes = size == 256 ? [(byte)code] : [(byte)(code >> 8), (byte)code];
            Assert.Equal(bytes, e.GetBytes(text));
            Assert.Equal(bytes.Length, e.GetByteCount(text));
        }
    }
    [Theory]
    [MemberData(nameof(Names))]
    public void StreamsAndAllSplits(string name)
    {
        var e = Get(name);
        string text = name == "JEF" ? "あ海\u3000𫝃𛀙゙\uE000\uEC1D" : name.Contains("HanyoDenshi") ? "あ\u4E08\U000E0103\uE000" : name == "JEF-Roundtrip" ? "あ海\u3000\uE000\uEC1D" : name.Contains('+') ? "AあB海\u3000𫝃𛀙゙\uE000\uEC1D" : "ABC 123\r\n";
        byte[] expected = e.GetBytes(text);
        for (int split = 0; split <= text.Length; split++)
            foreach (int capacity in new[] { 1, 2, 3, 7 })
            {
                var encoder = e.GetEncoder(); var result = new List<byte>();
                Encode(encoder, text.AsSpan(0, split), false, capacity, result);
                Encode(encoder, text.AsSpan(split), true, capacity, result);
                Assert.Equal(expected, result);
            }
        for (int split = 0; split <= expected.Length; split++)
            foreach (int capacity in new[] { 1, 2, 3, 7 })
            {
                var decoder = e.GetDecoder(); var result = new StringBuilder();
                Decode(decoder, expected.AsSpan(0, split), false, capacity, result);
                Decode(decoder, expected.AsSpan(split), true, capacity, result);
                Assert.Equal(text, result.ToString());
            }
        var enc = e.GetEncoder(); var singleBytes = new List<byte>();
        foreach (char ch in text) Encode(enc, new[] { ch }, false, 1, singleBytes);
        Encode(enc, default, true, 1, singleBytes);
        Assert.Equal(expected, singleBytes);
        var dec = e.GetDecoder(); var singleChars = new StringBuilder();
        foreach (byte b in expected) Decode(dec, new[] { b }, false, 1, singleChars);
        Decode(dec, default, true, 1, singleChars);
        Assert.Equal(text, singleChars.ToString());
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, e, 128, true))
            for (int i = 0; i < 50; i++) writer.Write(text);
        stream.Position = 0;
        using var reader = new StreamReader(stream, e, false, 128, true);
        Assert.Equal(string.Concat(Enumerable.Repeat(text, 50)), reader.ReadToEnd());
    }
    internal static void Encode(Encoder encoder, ReadOnlySpan<char> input, bool flush, int capacity, List<byte> result)
    {
        byte[] buffer = new byte[capacity];
        for (int guard = 0; guard < 10000; guard++)
        {
            encoder.Convert(input, buffer, flush, out int used, out int written, out bool completed);
            Assert.InRange(used, 0, input.Length); Assert.InRange(written, 0, buffer.Length);
            result.AddRange(buffer.Take(written)); input = input.Slice(used);
            if (completed) { Assert.True(input.IsEmpty); return; }
            Assert.True(used != 0 || written != 0);
        }
        Assert.Fail("Encoder did not terminate.");
    }
    internal static void Decode(Decoder decoder, ReadOnlySpan<byte> input, bool flush, int capacity, StringBuilder result)
    {
        char[] buffer = new char[capacity];
        for (int guard = 0; guard < 10000; guard++)
        {
            decoder.Convert(input, buffer, flush, out int used, out int written, out bool completed);
            Assert.InRange(used, 0, input.Length); Assert.InRange(written, 0, buffer.Length);
            result.Append(buffer, 0, written); input = input.Slice(used);
            if (completed) { Assert.True(input.IsEmpty); return; }
            Assert.True(used != 0 || written != 0);
        }
        Assert.Fail("Decoder did not terminate.");
    }
}
