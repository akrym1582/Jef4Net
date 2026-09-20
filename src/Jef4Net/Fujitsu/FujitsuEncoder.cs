using System.Text;
using Jef4Net.Fujitsu.Internal;

namespace Jef4Net.Fujitsu;

internal sealed class FujitsuEncoder : Encoder
{
    private readonly Configuration config;
    private EncoderState state;
    internal FujitsuEncoder(FujitsuEncoding encoding)
    { config = encoding.Configuration; Fallback = encoding.EncoderFallback; Reset(); }
    public override void Reset() { state = new EncoderState { Jef = config.InitialJef }; base.Reset(); }
    public override int GetByteCount(char[] chars, int index, int count, bool flush) => GetByteCount(Bounds.Slice(chars, index, count), flush);
    public override int GetByteCount(ReadOnlySpan<char> chars, bool flush)
    {
        var copy = state;
        FujitsuEncoderCore.Convert(config, Fallback!, ref copy, chars, default, flush, true, out _, out int count, out _);
        return count;
    }
    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
        => GetBytes(Bounds.Slice(chars, charIndex, charCount), Bounds.Tail(bytes, byteIndex), flush);
    public override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, bool flush)
    {
        if (GetByteCount(chars, flush) > bytes.Length) throw new ArgumentException("Output buffer is too small.", nameof(bytes));
        FujitsuEncoderCore.Convert(config, Fallback!, ref state, chars, bytes, flush, false, out _, out int count, out _);
        return count;
    }
    public override void Convert(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, int byteCount,
        bool flush, out int charsUsed, out int bytesUsed, out bool completed)
        => Convert(Bounds.Slice(chars, charIndex, charCount), Bounds.Slice(bytes, byteIndex, byteCount), flush, out charsUsed, out bytesUsed, out completed);
    public override void Convert(ReadOnlySpan<char> chars, Span<byte> bytes, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
        => FujitsuEncoderCore.Convert(config, Fallback!, ref state, chars, bytes, flush, false, out charsUsed, out bytesUsed, out completed);
}
