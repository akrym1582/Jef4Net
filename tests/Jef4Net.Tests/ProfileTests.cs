using System.Globalization;
using System.Text;
using System.Text.Json;
using Jef4Net.Fujitsu;

namespace Jef4Net.Tests;

public class ProfileTests
{
    private static JsonDocument Data() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "data", "fujitsu_jef_mapping.json")));
    private static int Hex(JsonElement value) => int.Parse(value.GetString()!, NumberStyles.HexNumber);
    private static byte[] Code(int value) => [(byte)(value >> 8), (byte)value];
    private static string Text(JsonElement row, bool hanyo)
    {
        string value = char.ConvertFromUtf32(Hex(row.GetProperty("unicode")));
        if (hanyo && row.TryGetProperty("hd", out var hd)) value += char.ConvertFromUtf32(Hex(hd));
        else if (row.TryGetProperty("sp", out var sp)) value += char.ConvertFromUtf32(Hex(sp));
        return value;
    }

    [Fact]
    public void RoundtripIsAnInverseOnEveryCodeAndSelectedUnicodeSequence()
    {
        using var doc = Data();
        var decoded = new Dictionary<int, string>();
        var encoded = new Dictionary<string, int>();
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            var options = row.GetProperty("options").EnumerateArray().Select(x => x.GetString()!).ToHashSet();
            if (row.TryGetProperty("hd", out _) || row.TryGetProperty("aj1", out _) ||
                options.Contains("oneway") || options.Contains("unmappable")) continue;
            int code = Hex(row.GetProperty("code"));
            string value = Text(row, false);
            if (!options.Contains("encode_only")) decoded[code] = value;
            if (!options.Contains("decode_only")) encoded[value] = code;
        }
        var selected = decoded.Where(pair => encoded.TryGetValue(pair.Value, out int code) && code == pair.Key)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var e = EncodingTests.Get("JEF-Roundtrip");
        for (int code = 0; code <= 0xFFFF; code++)
        {
            if (code >> 8 is >= 0x80 and <= 0xA0 && (code & 255) is >= 0xA1 and <= 0xFE)
            {
                string pua = char.ConvertFromUtf32(0xE000 + ((code >> 8) - 0x80) * 94 + (code & 255) - 0xA1);
                Assert.Equal(pua, e.GetString(Code(code)));
                Assert.Equal(Code(code), e.GetBytes(pua));
            }
            else if (selected.TryGetValue(code, out string? value))
            {
                Assert.Equal(value, e.GetString(Code(code)));
                Assert.Equal(Code(code), e.GetBytes(value));
            }
            else Assert.Throws<DecoderFallbackException>(() => e.GetString(Code(code)));
        }
        foreach (var (value, code) in encoded)
            if (selected.TryGetValue(code, out string? decodedValue) && decodedValue == value)
                Assert.Equal(Code(code), e.GetBytes(value));
        Assert.Equal("\u3000", EncodingTests.Get("JEF").GetString(Code(0xA1A1)));
        Assert.Throws<DecoderFallbackException>(() => e.GetString(Code(0xA1A1)));
    }

    [Fact]
    public void HanyoDenshiFollowsDataOrderAndDirectionAttributes()
    {
        using var doc = Data();
        var decoded = new Dictionary<int, string>();
        var encoded = new Dictionary<string, int>();
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            var options = row.GetProperty("options").EnumerateArray().Select(x => x.GetString()!).ToHashSet();
            if (row.TryGetProperty("aj1", out _) || options.Contains("unmappable")) continue;
            int code = Hex(row.GetProperty("code"));
            string bare = Text(row, false);
            string withHd = Text(row, true);
            if (!options.Contains("encode_only")) decoded[code] = withHd;
            if (!options.Contains("decode_only"))
            {
                if (!options.Contains("variant_only")) encoded[bare] = code;
                encoded[withHd] = code;
            }
        }
        var e = EncodingTests.Get("JEF-HanyoDenshi");
        for (int code = 0; code <= 0xFFFF; code++)
        {
            if (code >> 8 is >= 0x80 and <= 0xA0 && (code & 255) is >= 0xA1 and <= 0xFE)
                Assert.Equal(char.ConvertFromUtf32(0xE000 + ((code >> 8) - 0x80) * 94 + (code & 255) - 0xA1), e.GetString(Code(code)));
            else if (decoded.TryGetValue(code, out string? value)) Assert.Equal(value, e.GetString(Code(code)));
            else Assert.Throws<DecoderFallbackException>(() => e.GetString(Code(code)));
        }
        foreach (var (value, code) in encoded) Assert.Equal(Code(code), e.GetBytes(value));
        Assert.Equal("\u4E08\U000E0103", e.GetString(Code(0x41A5)));
        Assert.Equal(Code(0x41A5), e.GetBytes("\u4E08\U000E0103"));
        Assert.Equal(Code(0x47C9), e.GetBytes("\u585A"));
        Assert.Equal("\uFA30", e.GetString(Code(0x42BB)));
    }

    [Theory]
    [InlineData("JEF-HanyoDenshi")]
    [InlineData("EBCDIC-Lower+JEF-HanyoDenshi")]
    [InlineData("JEF-HanyoDenshi+EBCDIC-Lower")]
    public void IvsStreamsAcrossEveryBoundary(string name)
    {
        var e = EncodingTests.Get(name);
        string text = "\u4E08\U000E0103\U0002000B\U000E0101\uE000";
        byte[] expected = e.GetBytes(text);
        Assert.Equal(text, e.GetString(expected));
        for (int split = 0; split <= text.Length; split++)
            foreach (int capacity in new[] { 1, 2, 3, 4 })
            {
                var bytes = new List<byte>(); var encoder = e.GetEncoder();
                EncodingTests.Encode(encoder, text.AsSpan(0, split), false, capacity, bytes);
                EncodingTests.Encode(encoder, text.AsSpan(split), true, capacity, bytes);
                Assert.Equal(expected, bytes);
            }
        for (int split = 0; split <= expected.Length; split++)
            foreach (int capacity in new[] { 1, 2, 3, 4 })
            {
                var chars = new StringBuilder(); var decoder = e.GetDecoder();
                EncodingTests.Decode(decoder, expected.AsSpan(0, split), false, capacity, chars);
                EncodingTests.Decode(decoder, expected.AsSpan(split), true, capacity, chars);
                Assert.Equal(text, chars.ToString());
            }
    }

    [Theory]
    [InlineData("Lower")]
    [InlineData("Kana")]
    [InlineData("Ascii")]
    public void MixedHanyoDenshiKeepsShiftsAroundIvs(string kind)
    {
        foreach (string name in new[] { $"EBCDIC-{kind}+JEF-HanyoDenshi", $"JEF-HanyoDenshi+EBCDIC-{kind}" })
        {
            var e = EncodingTests.Get(name);
            string value = "A\u4E08\U000E0103B";
            byte[] bytes = e.GetBytes(value);
            Assert.Equal(value, e.GetString(bytes));
            Assert.Equal(e.GetByteCount(value), bytes.Length);
            Assert.Equal(value.Length, e.GetCharCount(bytes));
            Assert.Equal(value, ((Encoding)e.Clone()).GetString(bytes));
            var decoder = e.GetDecoder(); var result = new StringBuilder();
            byte[] k2 = [0x30, 0xE2, 0x41, 0xA5, 0x29];
            EncodingTests.Decode(decoder, k2, true, 1, result);
            Assert.Equal("\u4E08\U000E0103", result.ToString());
        }
    }

    [Fact]
    public void UnknownSelectorsUseFallbackAndNamesRemainDistinct()
    {
        var strict = EncodingTests.Get("JEF-HanyoDenshi");
        Assert.Throws<EncoderFallbackException>(() => strict.GetBytes("\u4E08\U000E0104"));
        Assert.Throws<EncoderFallbackException>(() => strict.GetBytes("\U000E0103"));
        var replacement = EncodingTests.Get("JEF-HanyoDenshi", false);
        Assert.Equal(replacement.GetBytes("\u4E08").Concat(Code(0x4040)).Concat(Code(0x4040)),
            replacement.GetBytes("\u4E08\U000E0104"));
        Assert.Null(FujitsuEncodingProvider.Instance.GetEncoding("EBCDIC-Lower+JEF-Roundtrip"));
        Assert.Equal("x-Fujitsu-JEF-HanyoDenshi", FujitsuEncodingProvider.Instance.GetEncoding("jef-hanyodenshi")!.WebName);
    }

    [Fact]
    public void IvsFallbackCountResetAndTinyBuffers()
    {
        string ivs = "\u4E08\U000E0103";
        var e = FujitsuEncodingProvider.Instance.GetEncoding("JEF-HanyoDenshi",
            new EncoderReplacementFallback(ivs), new DecoderReplacementFallback("[bad]"))!;
        byte[] expected = Code(0x41A5);
        Assert.Equal(expected.Concat(expected), e.GetBytes("😀"));
        Assert.Equal(expected.Length * 2, e.GetByteCount("😀"));
        var encoder = e.GetEncoder();
        Assert.Equal(2, encoder.GetByteCount(ivs.AsSpan(), true));
        Assert.Equal(2, encoder.GetByteCount(ivs.AsSpan(), true));
        Assert.Throws<ArgumentException>(() => encoder.GetBytes(ivs.AsSpan(), new byte[1], true));
        byte[] buffer = new byte[2];
        Assert.Equal(2, encoder.GetBytes(ivs.AsSpan(), buffer, true));
        Assert.Equal(expected, buffer);
        encoder.Convert("\u4E08".AsSpan(), Span<byte>.Empty, false, out int used, out int written, out bool completed);
        Assert.Equal(1, used); Assert.Equal(0, written); Assert.True(completed);
        encoder.Reset();
        Assert.Equal(expected, e.GetBytes(ivs));
        var decoder = e.GetDecoder();
        Assert.Equal(3, decoder.GetCharCount(expected, 0, expected.Length, true));
        Assert.Throws<ArgumentException>(() => decoder.GetChars(expected, 0, expected.Length, new char[2], 0, true));
        var chars = new char[3];
        Assert.Equal(3, decoder.GetChars(expected, 0, expected.Length, chars, 0, true));
        Assert.Equal(ivs, new string(chars));
        decoder.Convert(expected, Span<char>.Empty, true, out used, out written, out completed);
        Assert.Equal(2, used); Assert.Equal(0, written); Assert.False(completed);
        var result = new StringBuilder();
        EncodingTests.Decode(decoder, default, true, 1, result);
        Assert.Equal(ivs, result.ToString());
        decoder.Reset();
        Assert.Equal(ivs, e.GetString(expected));
        Assert.True(e.GetMaxByteCount(ivs.Length) >= expected.Length);
        Assert.True(e.GetMaxCharCount(expected.Length) >= ivs.Length);
    }
}
