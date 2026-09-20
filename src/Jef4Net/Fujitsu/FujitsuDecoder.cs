using System.Text;
using Jef4Net.Fujitsu.Internal;

namespace Jef4Net.Fujitsu;

internal sealed class FujitsuDecoder : Decoder
{
    private readonly Configuration config;
    private DecoderState state;
    internal FujitsuDecoder(FujitsuEncoding encoding)
    { config = encoding.Configuration; Fallback = encoding.DecoderFallback; Reset(); }
    public override void Reset() { state = new DecoderState { Jef = config.InitialJef }; base.Reset(); }
    public override int GetCharCount(byte[] bytes, int index, int count) => GetCharCount(bytes, index, count, false);
    public override int GetCharCount(byte[] bytes, int index, int count, bool flush) => GetCharCount(Bounds.Slice(bytes, index, count), flush);
    public override int GetCharCount(ReadOnlySpan<byte> bytes, bool flush)
    {
        var copy = state;
        FujitsuDecoderCore.Convert(config, Fallback!, ref copy, bytes, default, flush, true, out _, out int count, out _);
        return count;
    }
    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) => GetChars(bytes, byteIndex, byteCount, chars, charIndex, false);
    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
        => GetChars(Bounds.Slice(bytes, byteIndex, byteCount), Bounds.Tail(chars, charIndex), flush);
    public override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars, bool flush)
    {
        if (GetCharCount(bytes, flush) > chars.Length) throw new ArgumentException("Output buffer is too small.", nameof(chars));
        FujitsuDecoderCore.Convert(config, Fallback!, ref state, bytes, chars, flush, false, out _, out int count, out _);
        return count;
    }
    public override void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount,
        bool flush, out int bytesUsed, out int charsUsed, out bool completed)
        => Convert(Bounds.Slice(bytes, byteIndex, byteCount), Bounds.Slice(chars, charIndex, charCount), flush, out bytesUsed, out charsUsed, out completed);
    public override void Convert(ReadOnlySpan<byte> bytes, Span<char> chars, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
        => FujitsuDecoderCore.Convert(config, Fallback!, ref state, bytes, chars, flush, false, out bytesUsed, out charsUsed, out completed);
}
