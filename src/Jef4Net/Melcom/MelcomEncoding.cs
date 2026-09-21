#pragma warning disable
namespace Jef4Net.Melcom;

using System.Globalization;
using System.Text;
using Jef4Net.Hitachi.Internal;

/// <summary>Specifies the JIS generation used by MELCOM/JSII standard characters.</summary>
public enum MelcomJisVersion { Jis78, Jis83 }

/// <summary>Maps documented site-specific or vendor-specific MELCOM codes.</summary>
public interface IMelcomExtensionMapping
{
    /// <summary>Attempts to decode a two-byte MELCOM code.</summary>
    bool TryDecode(ushort melcomCode, out int unicodeScalar);
    /// <summary>Attempts to encode a Unicode scalar.</summary>
    bool TryEncode(int unicodeScalar, out ushort melcomCode);
}

/// <summary>An immutable custom MELCOM extension mapping.</summary>
public sealed class MelcomExtensionMapping : IMelcomExtensionMapping
{
    private readonly Dictionary<ushort, int> decode;
    private readonly Dictionary<int, ushort> encode;

    /// <summary>Creates a mapping from MELCOM-code/Unicode-scalar pairs.</summary>
    public MelcomExtensionMapping(IEnumerable<KeyValuePair<ushort, int>> entries)
    {
        if (entries == null) throw new ArgumentNullException(nameof(entries));
        this.decode = new(); this.encode = new();
        foreach (var entry in entries)
        {
            if (entry.Key is >= 0xA1A1 and <= 0xFEFE && (entry.Key & 0xFF) is >= 0xA1 and <= 0xFE)
                throw new ArgumentException("Extension mappings must not replace the standard JIS area.", nameof(entries));
            if (!this.decode.TryAdd(entry.Key, entry.Value) || !this.encode.TryAdd(entry.Value, entry.Key))
                throw new ArgumentException("Codes and Unicode scalars must be unique.", nameof(entries));
        }
    }

    /// <summary>Loads a UTF-8 CSV containing a header followed by hexadecimal MELCOM code and U+scalar columns.</summary>
    public static MelcomExtensionMapping LoadFromCsv(Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        var entries = new List<KeyValuePair<ushort, int>>();
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), true, 1024, true);
        string? line; bool first = true;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#", StringComparison.Ordinal)) continue;
            string[] fields = line.Split(',');
            if (first && fields[0].Trim().Equals("MelcomCode", StringComparison.OrdinalIgnoreCase)) { first = false; continue; }
            first = false;
            if (fields.Length < 2 || !ushort.TryParse(fields[0].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort code))
                throw new FormatException("Invalid MELCOM code in CSV.");
            string scalarText = fields[1].Trim();
            if (scalarText.StartsWith("U+", StringComparison.OrdinalIgnoreCase)) scalarText = scalarText[2..];
            if (!int.TryParse(scalarText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int scalar) || scalar < 0 || scalar > 0x10FFFF || scalar is >= 0xD800 and <= 0xDFFF)
                throw new FormatException("Invalid Unicode scalar in CSV.");
            entries.Add(new(code, scalar));
        }
        return new(entries);
    }

    /// <inheritdoc/>
    public bool TryDecode(ushort melcomCode, out int unicodeScalar) => this.decode.TryGetValue(melcomCode, out unicodeScalar);
    /// <inheritdoc/>
    public bool TryEncode(int unicodeScalar, out ushort melcomCode) => this.encode.TryGetValue(unicodeScalar, out melcomCode);
}

internal sealed class EmptyMelcomExtensionMapping : IMelcomExtensionMapping
{
    internal static readonly EmptyMelcomExtensionMapping Instance = new();
    public bool TryDecode(ushort melcomCode, out int unicodeScalar) { unicodeScalar = default; return false; }
    public bool TryEncode(int unicodeScalar, out ushort melcomCode) { melcomCode = default; return false; }
}

/// <summary>Options for <see cref="MelcomEncoding"/>.</summary>
public sealed class MelcomOptions
{
    /// <summary>Gets or sets the JIS generation. JIS83 is a convenience default, not a claim about all MELCOM systems.</summary>
    public MelcomJisVersion JisVersion { get; set; } = MelcomJisVersion.Jis83;
    /// <summary>Gets or sets an explicitly supplied extension mapping.</summary>
    public IMelcomExtensionMapping? ExtensionMapping { get; set; }
    /// <summary>Gets or sets the encoder fallback.</summary>
    public EncoderFallback EncoderFallback { get; set; } = EncoderFallback.ReplacementFallback;
    /// <summary>Gets or sets the decoder fallback.</summary>
    public DecoderFallback DecoderFallback { get; set; } = DecoderFallback.ReplacementFallback;
}

/// <summary>Encodes MELCOM/JSII DBCS standard characters in JIS+0x8080 form.</summary>
/// <remarks>MELCOM vendor extensions have no verified complete public mapping and are not converted unless explicitly supplied.</remarks>
public sealed class MelcomEncoding : Encoding
{
    private readonly MelcomJisVersion version;
    private readonly IMelcomExtensionMapping extensions;

    /// <summary>Creates an encoding using the convenience JIS83 default.</summary>
    public MelcomEncoding() : this(new MelcomOptions()) { }
    /// <summary>Creates an encoding with explicit options.</summary>
    public MelcomEncoding(MelcomOptions options)
        : base(0, (options ?? throw new ArgumentNullException(nameof(options))).EncoderFallback, options.DecoderFallback)
    { this.version = options.JisVersion; this.extensions = options.ExtensionMapping ?? EmptyMelcomExtensionMapping.Instance; }

    /// <inheritdoc/>
    public override string EncodingName => $"MELCOM/JSII {this.version}";
    /// <inheritdoc/>
    public override string WebName => this.version == MelcomJisVersion.Jis78 ? "x-melcom-jsii-jis78" : "x-melcom-jsii-jis83";
    /// <inheritdoc/>
    public override byte[] GetPreamble() => Array.Empty<byte>();

    /// <inheritdoc/>
    public override Decoder GetDecoder() => new MelcomDecoder(this);
    /// <inheritdoc/>
    public override int GetMaxByteCount(int charCount) => checked((charCount + 1) * Math.Max(1, this.EncoderFallback.MaxCharCount) * 2);
    /// <inheritdoc/>
    public override int GetMaxCharCount(int byteCount) => checked((byteCount + 1) * Math.Max(1, this.DecoderFallback.MaxCharCount));

    internal bool TryDecode(ushort code, out int unicodeScalar)
    {
        if ((code >> 8) is >= 0xA1 and <= 0xFE && (code & 255) is >= 0xA1 and <= 0xFE)
        {
            ulong key = Mapping.Decode(true, this.version == MelcomJisVersion.Jis78 ? 0 : 1, code, false);
            if (key != 0) { unicodeScalar = Mapping.Scalar(key); return true; }
        }
        return this.extensions.TryDecode(code, out unicodeScalar);
    }

    internal bool TryEncode(int unicodeScalar, out ushort code)
    {
        int value = Mapping.Encode(true, this.version == MelcomJisVersion.Jis78 ? 0 : 1, Mapping.Key(unicodeScalar), false);
        if (unicodeScalar == 0x3000) value = 0xA1A1;
        if (value >= 0 && (value >> 8) is >= 0xA1 and <= 0xFE && (value & 255) is >= 0xA1 and <= 0xFE) { code = (ushort)value; return true; }
        return this.extensions.TryEncode(unicodeScalar, out code);
    }

    private byte[] Encode(string text)
    {
        var result = new List<byte>(); var fb = this.EncoderFallback.CreateFallbackBuffer();
        for (int i = 0; i < text.Length;)
        {
            int unicodeScalar; int used;
            if (!char.IsSurrogate(text[i])) { unicodeScalar = text[i]; used = 1; }
            else if (i + 1 < text.Length && char.IsSurrogatePair(text[i], text[i + 1])) { unicodeScalar = char.ConvertToUtf32(text[i], text[i + 1]); used = 2; }
            else { fb.Fallback(text[i], i); used = 1; while (fb.Remaining > 0) Append(fb.GetNextChar()); i += used; continue; }
            if (!Append(unicodeScalar)) { if (used == 2) fb.Fallback(text[i], text[i + 1], i); else fb.Fallback(text[i], i); while (fb.Remaining > 0) Append(fb.GetNextChar()); }
            i += used;
        }
        return result.ToArray();
        bool Append(int r) { if (!this.TryEncode(r, out ushort c)) return false; result.Add((byte)(c >> 8)); result.Add((byte)c); return true; }
    }

    private string DecodeBytes(ReadOnlySpan<byte> bytes)
    {
        var result = new StringBuilder(); var fb = this.DecoderFallback.CreateFallbackBuffer(); int i = 0;
        while (i < bytes.Length)
        {
            int n = Math.Min(2, bytes.Length - i); byte[] bad = bytes.Slice(i, n).ToArray();
            if (n == 2 && this.TryDecode((ushort)((bytes[i] << 8) | bytes[i + 1]), out int unicodeScalar)) result.Append(char.ConvertFromUtf32(unicodeScalar));
            else { fb.Fallback(bad, i); while (fb.Remaining > 0) result.Append(fb.GetNextChar()); }
            i += n;
        }
        return result.ToString();
    }

    /// <inheritdoc/>
    public override int GetByteCount(char[] chars, int index, int count) => this.Encode(new string(chars, index, count)).Length;
    /// <inheritdoc/>
    public override int GetByteCount(string s) => this.Encode(s ?? throw new ArgumentNullException(nameof(s))).Length;
    /// <inheritdoc/>
    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) => Copy(this.Encode(new string(chars, charIndex, charCount)), bytes, byteIndex);
    /// <inheritdoc/>
    public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) => Copy(this.Encode((s ?? throw new ArgumentNullException(nameof(s))).Substring(charIndex, charCount)), bytes, byteIndex);
    /// <inheritdoc/>
    public override int GetCharCount(byte[] bytes, int index, int count) => this.DecodeBytes(bytes.AsSpan(index, count)).Length;
    /// <inheritdoc/>
    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { string s = this.DecodeBytes(bytes.AsSpan(byteIndex, byteCount)); s.CopyTo(0, chars, charIndex, s.Length); return s.Length; }
    private static int Copy(byte[] source, byte[] destination, int index) { source.CopyTo(destination, index); return source.Length; }
}

internal sealed class MelcomDecoder : Decoder
{
    private readonly MelcomEncoding encoding;
    private byte? pending;

    internal MelcomDecoder(MelcomEncoding encoding)
    {
        this.encoding = encoding;
        this.Fallback = encoding.DecoderFallback;
    }

    public override int GetCharCount(byte[] bytes, int index, int count)
    {
        byte? saved = this.pending;
        int result = this.GetChars(bytes, index, count, Array.Empty<char>(), 0, false);
        this.pending = saved;
        return result;
    }

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        => this.GetChars(bytes, byteIndex, byteCount, chars, charIndex, false);

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
    {
        var input = new List<byte>(byteCount + 1);
        if (this.pending.HasValue) input.Add(this.pending.Value);
        input.AddRange(bytes.AsSpan(byteIndex, byteCount).ToArray());
        this.pending = null;
        if (!flush && (input.Count & 1) != 0)
        {
            this.pending = input[^1];
            input.RemoveAt(input.Count - 1);
        }

        string value = this.encoding.GetString(input.ToArray());
        if (chars.Length == 0) return value.Length;
        value.CopyTo(0, chars, charIndex, value.Length);
        return value.Length;
    }

    public override void Reset()
    {
        this.pending = null;
        base.Reset();
    }
}
