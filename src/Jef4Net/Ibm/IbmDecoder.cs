namespace Jef4Net.Ibm;

using System.Text;
using Jef4Net.Ibm.Internal;

/// <summary>Provides stateful decoding for a Ibm encoding.</summary>
internal sealed class IbmDecoder : Decoder
{
    private readonly Configuration config;
    private DecoderState state;

    /// <summary>Initializes a new instance of the <see cref="IbmDecoder"/> class.</summary>
    /// <param name="encoding">The owning Ibm encoding.</param>
    internal IbmDecoder(IbmEncoding encoding)
    {
        this.config = encoding.GetConfiguration();
        this.Fallback = encoding.DecoderFallback;
        this.Reset();
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        this.state = new DecoderState { Dbcs = this.config.InitialDbcs };
        base.Reset();
    }

    /// <inheritdoc/>
    public override int GetCharCount(byte[] bytes, int index, int count) => this.GetCharCount(bytes, index, count, false);

    /// <inheritdoc/>
    public override int GetCharCount(byte[] bytes, int index, int count, bool flush) => this.GetCharCount(Bounds.Slice(bytes, index, count), flush);

    /// <inheritdoc/>
    public override int GetCharCount(ReadOnlySpan<byte> bytes, bool flush)
    {
        var copy = this.state;
        IbmDecoderCore.Convert(this.config, this.Fallback!, ref copy, bytes, default, flush, true, out _, out int count, out _);
        return count;
    }

    /// <inheritdoc/>
    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) => this.GetChars(bytes, byteIndex, byteCount, chars, charIndex, false);

    /// <inheritdoc/>
    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
        => this.GetChars(Bounds.Slice(bytes, byteIndex, byteCount), Bounds.Tail(chars, charIndex), flush);

    /// <inheritdoc/>
    public override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars, bool flush)
    {
        if (this.GetCharCount(bytes, flush) > chars.Length)
        {
            throw new ArgumentException("Output buffer is too small.", nameof(chars));
        }

        IbmDecoderCore.Convert(this.config, this.Fallback!, ref this.state, bytes, chars, flush, false, out _, out int count, out _);
        return count;
    }

    /// <inheritdoc/>
    public override void Convert(
        byte[] bytes,
        int byteIndex,
        int byteCount,
        char[] chars,
        int charIndex,
        int charCount,
        bool flush,
        out int bytesUsed,
        out int charsUsed,
        out bool completed)
        => this.Convert(Bounds.Slice(bytes, byteIndex, byteCount), Bounds.Slice(chars, charIndex, charCount), flush, out bytesUsed, out charsUsed, out completed);

    /// <inheritdoc/>
    public override void Convert(ReadOnlySpan<byte> bytes, Span<char> chars, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
        => IbmDecoderCore.Convert(this.config, this.Fallback!, ref this.state, bytes, chars, flush, false, out bytesUsed, out charsUsed, out completed);
}
