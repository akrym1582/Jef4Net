using System.Text;
using Jef4Net.Fujitsu.Internal;

namespace Jef4Net.Fujitsu;

/// <summary>A Fujitsu encoding. Obtain instances from <see cref="FujitsuEncodingProvider"/>.</summary>
public sealed class FujitsuEncoding : Encoding
{
    internal Configuration Configuration { get; }
    private readonly string name;
    internal FujitsuEncoding(string name, Configuration config)
        : base(0, new EncoderReplacementFallback(config.InitialJef || config.Mixed ? "\u3000" : "?"), new DecoderReplacementFallback("\uFFFD"))
    { this.name = name; Configuration = config; }
    /// <inheritdoc/>
    public override string EncodingName => name;
    /// <inheritdoc/>
    public override string WebName => name;
    /// <inheritdoc/>
    public override bool IsSingleByte => !Configuration.Mixed && !Configuration.InitialJef;
    /// <inheritdoc/>
    public override byte[] GetPreamble() => Array.Empty<byte>();
    /// <inheritdoc/>
    public override Encoder GetEncoder() => new FujitsuEncoder(this);
    /// <inheritdoc/>
    public override Decoder GetDecoder() => new FujitsuDecoder(this);
    /// <inheritdoc/>
    public override int GetByteCount(char[] chars, int index, int count) => GetByteCount(Bounds.Slice(chars, index, count));
    /// <inheritdoc/>
    public override int GetByteCount(string s)
    { if (s == null) throw new ArgumentNullException(nameof(s)); return GetByteCount(s.AsSpan()); }
    /// <inheritdoc/>
    public override int GetByteCount(ReadOnlySpan<char> chars)
    {
        var state = new EncoderState { Jef = Configuration.InitialJef };
        FujitsuEncoderCore.Convert(Configuration, EncoderFallback, ref state, chars, default, true, true, out _, out int count, out _);
        return count;
    }
    /// <inheritdoc/>
    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        => GetBytes(Bounds.Slice(chars, charIndex, charCount), Bounds.Tail(bytes, byteIndex));
    /// <inheritdoc/>
    public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        if (s == null) throw new ArgumentNullException(nameof(s));
        Bounds.Check(s.Length, charIndex, charCount);
        return GetBytes(s.AsSpan(charIndex, charCount), Bounds.Tail(bytes, byteIndex));
    }
    /// <inheritdoc/>
    public override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
    {
        if (GetByteCount(chars) > bytes.Length) throw new ArgumentException("Output buffer is too small.", nameof(bytes));
        var state = new EncoderState { Jef = Configuration.InitialJef };
        FujitsuEncoderCore.Convert(Configuration, EncoderFallback, ref state, chars, bytes, true, false, out _, out int written, out _);
        return written;
    }
    /// <inheritdoc/>
    public override int GetCharCount(byte[] bytes, int index, int count) => GetCharCount(Bounds.Slice(bytes, index, count));
    /// <inheritdoc/>
    public override int GetCharCount(ReadOnlySpan<byte> bytes)
    {
        var state = new DecoderState { Jef = Configuration.InitialJef };
        FujitsuDecoderCore.Convert(Configuration, DecoderFallback, ref state, bytes, default, true, true, out _, out int count, out _);
        return count;
    }
    /// <inheritdoc/>
    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        => GetChars(Bounds.Slice(bytes, byteIndex, byteCount), Bounds.Tail(chars, charIndex));
    /// <inheritdoc/>
    public override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars)
    {
        if (GetCharCount(bytes) > chars.Length) throw new ArgumentException("Output buffer is too small.", nameof(chars));
        var state = new DecoderState { Jef = Configuration.InitialJef };
        FujitsuDecoderCore.Convert(Configuration, DecoderFallback, ref state, bytes, chars, true, false, out _, out int written, out _);
        return written;
    }
    /// <inheritdoc/>
    public override int GetMaxByteCount(int charCount)
    {
        if (charCount < 0) throw new ArgumentOutOfRangeException(nameof(charCount));
        long result;
        try { result = checked(checked(((long)charCount + 4) * Math.Max(1, EncoderFallback.MaxCharCount)) * 3 + 1); }
        catch (OverflowException) { throw new ArgumentOutOfRangeException(nameof(charCount)); }
        if (result > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(charCount));
        return (int)result;
    }
    /// <inheritdoc/>
    public override int GetMaxCharCount(int byteCount)
    {
        if (byteCount < 0) throw new ArgumentOutOfRangeException(nameof(byteCount));
        long result = ((long)byteCount + 2) * Math.Max(4, DecoderFallback.MaxCharCount);
        if (result > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(byteCount));
        return (int)result;
    }
}
