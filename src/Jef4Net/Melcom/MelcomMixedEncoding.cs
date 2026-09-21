#pragma warning disable
namespace Jef4Net.Melcom;

using System.Text;

/// <summary>Configures the explicit byte sequences that enter and leave MELCOM DBCS mode.</summary>
public sealed class MelcomShiftOptions
{
    /// <summary>Gets or sets the one-to-three byte Kanji-in sequence.</summary>
    public ReadOnlyMemory<byte> KanjiIn { get; set; }
    /// <summary>Gets or sets the one-to-three byte Kanji-out sequence.</summary>
    public ReadOnlyMemory<byte> KanjiOut { get; set; }
}

/// <summary>Options for an explicitly configured SBCS/MELCOM mixed host format.</summary>
public sealed class MelcomMixedOptions
{
    /// <summary>Gets or sets the SBCS encoding.</summary>
    public Encoding SbcsEncoding { get; set; } = null!;
    /// <summary>Gets or sets the MELCOM DBCS encoding.</summary>
    public MelcomEncoding DbcsEncoding { get; set; } = null!;
    /// <summary>Gets or sets the shift sequences.</summary>
    public MelcomShiftOptions Shift { get; set; } = null!;
    /// <summary>Gets or sets whether encoding flushes DBCS mode with Kanji-out. This is a library policy, not a verified MELCOM default.</summary>
    public bool EmitKanjiOutAtEnd { get; set; } = true;
}

/// <summary>Combines a caller-selected SBCS encoding with MELCOM DBCS and caller-selected shift sequences.</summary>
public sealed class MelcomMixedEncoding : Encoding
{
    private readonly Encoding sbcs;
    private readonly MelcomEncoding dbcs;
    private readonly byte[] ki;
    private readonly byte[] ko;
    private readonly bool emitKo;

    /// <summary>Creates a mixed encoding. No KI, KO, or EBCDIC profile is assumed.</summary>
    public MelcomMixedEncoding(MelcomMixedOptions options)
        : base(0, options?.DbcsEncoding?.EncoderFallback ?? EncoderFallback.ReplacementFallback, options?.DbcsEncoding?.DecoderFallback ?? DecoderFallback.ReplacementFallback)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        this.sbcs = options.SbcsEncoding ?? throw new ArgumentNullException(nameof(options.SbcsEncoding));
        this.dbcs = options.DbcsEncoding ?? throw new ArgumentNullException(nameof(options.DbcsEncoding));
        if (options.Shift == null) throw new ArgumentNullException(nameof(options.Shift));
        this.ki = options.Shift.KanjiIn.ToArray(); this.ko = options.Shift.KanjiOut.ToArray();
        if (this.ki.Length is < 1 or > 3 || this.ko.Length is < 1 or > 3) throw new ArgumentException("Shift sequences must contain one to three bytes.", nameof(options));
        if (this.ki.SequenceEqual(this.ko)) throw new ArgumentException("Kanji-in and Kanji-out must differ.", nameof(options));
        this.emitKo = options.EmitKanjiOutAtEnd;
    }

    /// <inheritdoc/>
    public override string EncodingName => "MELCOM/JSII mixed (configured profile)";
    /// <inheritdoc/>
    public override string WebName => "x-melcom-jsii-mixed";
    /// <inheritdoc/>
    public override byte[] GetPreamble() => Array.Empty<byte>();
    /// <inheritdoc/>
    public override int GetMaxByteCount(int charCount) => checked((charCount + 1) * (2 + this.ki.Length + this.ko.Length));
    /// <inheritdoc/>
    public override int GetMaxCharCount(int byteCount) => checked((byteCount + 1) * Math.Max(this.sbcs.GetMaxCharCount(1), this.dbcs.GetMaxCharCount(2)));

    private byte[] Encode(string value)
    {
        var output = new List<byte>(); bool inDbcs = false;
        foreach (char character in value)
        {
            string s = character.ToString(); byte[]? encoded = TryEncode(this.sbcs, s);
            bool nextDbcs = encoded == null;
            if (nextDbcs) encoded = TryEncode(this.dbcs, s);
            if (encoded == null) encoded = this.dbcs.GetBytes(s); // applies configured fallback
            if (nextDbcs != inDbcs) { output.AddRange(nextDbcs ? this.ki : this.ko); inDbcs = nextDbcs; }
            output.AddRange(encoded);
        }
        if (inDbcs && this.emitKo) output.AddRange(this.ko);
        return output.ToArray();
    }

    private string Decode(byte[] bytes, int index, int count)
    {
        var output = new StringBuilder(); bool inDbcs = false; int end = index + count; int start = index;
        for (int i = index; i < end;)
        {
            byte[] shift = inDbcs ? this.ko : this.ki;
            if (i + shift.Length <= end && bytes.AsSpan(i, shift.Length).SequenceEqual(shift))
            {
                Append(start, i - start, inDbcs); i += shift.Length; start = i; inDbcs = !inDbcs; continue;
            }
            i += inDbcs ? Math.Min(2, end - i) : 1;
        }
        Append(start, end - start, inDbcs); return output.ToString();
        void Append(int p, int n, bool dbcsMode) { if (n != 0) output.Append((dbcsMode ? this.dbcs : this.sbcs).GetString(bytes, p, n)); }
    }

    private static byte[]? TryEncode(Encoding encoding, string value)
    {
        try { var clone = (Encoding)encoding.Clone(); clone.EncoderFallback = EncoderFallback.ExceptionFallback; return clone.GetBytes(value); }
        catch (EncoderFallbackException) { return null; }
    }

    /// <inheritdoc/>
    public override int GetByteCount(char[] chars, int index, int count) => this.Encode(new string(chars, index, count)).Length;
    /// <inheritdoc/>
    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { byte[] value = this.Encode(new string(chars, charIndex, charCount)); value.CopyTo(bytes, byteIndex); return value.Length; }
    /// <inheritdoc/>
    public override int GetCharCount(byte[] bytes, int index, int count) => this.Decode(bytes, index, count).Length;
    /// <inheritdoc/>
    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { string value = this.Decode(bytes, byteIndex, byteCount); value.CopyTo(0, chars, charIndex, value.Length); return value.Length; }
}
