namespace Jef4Net.Fujitsu;

using System.Text;
using Jef4Net.Fujitsu.Internal;

/// <summary>Provides stateful encoding for a Fujitsu encoding.</summary>
internal sealed class FujitsuEncoder : Encoder
{
    private readonly Configuration config;
    private EncoderState state;

    /// <summary>Initializes a new instance of the <see cref="FujitsuEncoder"/> class.</summary>
    /// <param name="encoding">The owning Fujitsu encoding.</param>
    internal FujitsuEncoder(FujitsuEncoding encoding)
    {
        this.config = encoding.GetConfiguration();
        this.Fallback = encoding.EncoderFallback;
        this.Reset();
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        this.state = new EncoderState { Jef = this.config.InitialJef };
        base.Reset();
    }

    /// <inheritdoc/>
    public override int GetByteCount(char[] chars, int index, int count, bool flush) => this.GetByteCount(Bounds.Slice(chars, index, count), flush);

    /// <inheritdoc/>
    public override int GetByteCount(ReadOnlySpan<char> chars, bool flush)
    {
        var copy = this.state;
        FujitsuEncoderCore.Convert(this.config, this.Fallback!, ref copy, chars, default, flush, true, out _, out int count, out _);
        return count;
    }

    /// <inheritdoc/>
    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
        => this.GetBytes(Bounds.Slice(chars, charIndex, charCount), Bounds.Tail(bytes, byteIndex), flush);

    /// <inheritdoc/>
    public override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, bool flush)
    {
        if (this.GetByteCount(chars, flush) > bytes.Length)
        {
            throw new ArgumentException("Output buffer is too small.", nameof(bytes));
        }

        FujitsuEncoderCore.Convert(this.config, this.Fallback!, ref this.state, chars, bytes, flush, false, out _, out int count, out _);
        return count;
    }

    /// <inheritdoc/>
    public override void Convert(
        char[] chars,
        int charIndex,
        int charCount,
        byte[] bytes,
        int byteIndex,
        int byteCount,
        bool flush,
        out int charsUsed,
        out int bytesUsed,
        out bool completed)
        => this.Convert(Bounds.Slice(chars, charIndex, charCount), Bounds.Slice(bytes, byteIndex, byteCount), flush, out charsUsed, out bytesUsed, out completed);

    /// <inheritdoc/>
    public override void Convert(ReadOnlySpan<char> chars, Span<byte> bytes, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
        => FujitsuEncoderCore.Convert(this.config, this.Fallback!, ref this.state, chars, bytes, flush, false, out charsUsed, out bytesUsed, out completed);
}
