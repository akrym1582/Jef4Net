namespace Jef4Net.Unisys;

using System.Text;
using Jef4Net.Unisys.Jbis.Internal;

/// <summary>A Jbis encoding. Use one of the concrete JBIS profile classes or <see cref="JbisEncodingProvider"/>.</summary>
public abstract class JbisEncoding : Encoding
{
    private readonly string name;
    private readonly Configuration configuration;

    /// <summary>Initializes a new instance of the <see cref="JbisEncoding"/> class.</summary>
    /// <param name="name">The canonical name of the encoding.</param>
    /// <param name="config">The conversion configuration.</param>
    internal JbisEncoding(string name, Configuration config)
        : base(0, new EncoderReplacementFallback(config.Mixed ? "?" : "\u3000"), new DecoderReplacementFallback("\uFFFD"))
    {
        this.name = name;
        this.configuration = config;
    }

    /// <inheritdoc/>
    public override string EncodingName => this.name;

    /// <inheritdoc/>
    public override string WebName => this.name;

    /// <inheritdoc/>
    public override bool IsSingleByte => false;

    /// <inheritdoc/>
    public override byte[] GetPreamble() => Array.Empty<byte>();

    /// <inheritdoc/>
    public override Encoder GetEncoder() => new JbisEncoder(this);

    /// <inheritdoc/>
    public override Decoder GetDecoder() => new JbisDecoder(this);

    /// <inheritdoc/>
    public override int GetByteCount(char[] chars, int index, int count) => this.GetByteCount(Bounds.Slice(chars, index, count));

    /// <inheritdoc/>
    public override int GetByteCount(string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        return this.GetByteCount(s.AsSpan());
    }

    /// <inheritdoc/>
    public override int GetByteCount(ReadOnlySpan<char> chars)
    {
        var state = new EncoderState { Dbcs = !this.configuration.Mixed };
        JbisEncoderCore.Convert(this.configuration, this.EncoderFallback, ref state, chars, default, true, true, out _, out int count, out _);
        return count;
    }

    /// <inheritdoc/>
    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        => this.GetBytes(Bounds.Slice(chars, charIndex, charCount), Bounds.Tail(bytes, byteIndex));

    /// <inheritdoc/>
    public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        Bounds.Check(s.Length, charIndex, charCount);
        return this.GetBytes(s.AsSpan(charIndex, charCount), Bounds.Tail(bytes, byteIndex));
    }

    /// <inheritdoc/>
    public override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
    {
        if (this.GetByteCount(chars) > bytes.Length)
        {
            throw new ArgumentException("Output buffer is too small.", nameof(bytes));
        }

        var state = new EncoderState { Dbcs = !this.configuration.Mixed };
        JbisEncoderCore.Convert(this.configuration, this.EncoderFallback, ref state, chars, bytes, true, false, out _, out int written, out _);
        return written;
    }

    /// <inheritdoc/>
    public override int GetCharCount(byte[] bytes, int index, int count) => this.GetCharCount(Bounds.Slice(bytes, index, count));

    /// <inheritdoc/>
    public override int GetCharCount(ReadOnlySpan<byte> bytes)
    {
        var state = new DecoderState { Dbcs = !this.configuration.Mixed };
        JbisDecoderCore.Convert(this.configuration, this.DecoderFallback, ref state, bytes, default, true, true, out _, out int count, out _);
        return count;
    }

    /// <inheritdoc/>
    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        => this.GetChars(Bounds.Slice(bytes, byteIndex, byteCount), Bounds.Tail(chars, charIndex));

    /// <inheritdoc/>
    public override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars)
    {
        if (this.GetCharCount(bytes) > chars.Length)
        {
            throw new ArgumentException("Output buffer is too small.", nameof(chars));
        }

        var state = new DecoderState { Dbcs = !this.configuration.Mixed };
        JbisDecoderCore.Convert(this.configuration, this.DecoderFallback, ref state, bytes, chars, true, false, out _, out int written, out _);
        return written;
    }

    /// <inheritdoc/>
    public override int GetMaxByteCount(int charCount)
    {
        if (charCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(charCount));
        }

        long result;
        try
        {
            result = checked((checked(((long)charCount + 4) * Math.Max(1, this.EncoderFallback.MaxCharCount)) * 4) + 2);
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(nameof(charCount));
        }

        if (result > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(charCount));
        }

        return (int)result;
    }

    /// <inheritdoc/>
    public override int GetMaxCharCount(int byteCount)
    {
        if (byteCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteCount));
        }

        long result = ((long)byteCount + 2) * Math.Max(4, this.DecoderFallback.MaxCharCount);
        if (result > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(byteCount));
        }

        return (int)result;
    }

    /// <summary>Gets the conversion configuration for this encoding.</summary>
    /// <returns>The conversion configuration for this encoding.</returns>
    internal Configuration GetConfiguration() => this.configuration;
}
