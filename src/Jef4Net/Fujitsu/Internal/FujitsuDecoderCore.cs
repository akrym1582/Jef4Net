using System.Text;

namespace Jef4Net.Fujitsu.Internal;

internal static class FujitsuDecoderCore
{
    internal static void Convert(Configuration config, DecoderFallback fallback, ref DecoderState state,
        ReadOnlySpan<byte> input, Span<char> output, bool flush, bool count,
        out int used, out int written, out bool completed)
    {
        used = written = 0;
        while (true)
        {
            while (state.OutputCount != 0 && (count || written < output.Length))
            {
                if (!count) output[written] = (char)state.Output;
                written = checked(written + 1); state.Output >>= 16; state.OutputCount--;
            }
            if (state.OutputCount != 0) break;
            while (state.Replacement != null && state.ReplacementIndex < state.Replacement.Length && (count || written < output.Length))
            {
                if (!count) output[written] = state.Replacement[state.ReplacementIndex];
                written = checked(written + 1); state.ReplacementIndex++;
            }
            if (state.Replacement != null)
            {
                if (state.ReplacementIndex != state.Replacement.Length) break;
                state.Replacement = null; state.ReplacementIndex = 0;
            }
            if (used == input.Length && (!flush || (state.Lead == 0 && !state.K2))) break;
            if (state.K2)
            {
                state.K2 = false;
                if (used < input.Length && input[used] == 0xE2) { used++; state.Jef = true; continue; }
                Replace(fallback, ref state, new byte[] { 0x30 }, used - 1); continue;
            }
            if (state.Lead != 0)
            {
                byte lead = (byte)(state.Lead - 1); state.Lead = 0;
                if (used == input.Length) { Replace(fallback, ref state, new[] { lead }, used - 1); continue; }
                // Invalid trails are left for the next iteration, so a following shift can resynchronize.
                byte trail = input[used];
                if (!(trail is >= 0xA1 and <= 0xFE || (lead == 0x40 && trail == 0x40)))
                { Replace(fallback, ref state, new[] { lead }, used - 1); continue; }
                used++;
                ulong key = Mapping.Decode(true, config.Kind, (lead << 8) | trail);
                if (key == 0) Replace(fallback, ref state, new[] { lead, trail }, used - 2);
                else Push(ref state, key);
                continue;
            }
            byte b = input[used++];
            if (config.Mixed)
            {
                if (b is 0x28 or 0x38) { state.Jef = true; continue; }
                if (b == 0x29) { state.Jef = false; continue; }
                if (b == 0x30) { state.K2 = true; continue; }
            }
            if (state.Jef)
            {
                if (b is >= 0x40 and <= 0xFE) state.Lead = b + 1;
                else Replace(fallback, ref state, new[] { b }, used - 1);
            }
            else
            {
                ulong key = Mapping.Decode(false, config.Kind, b);
                if (key == 0) Replace(fallback, ref state, new[] { b }, used - 1);
                else Push(ref state, key);
            }
        }
        completed = used == input.Length && state.OutputCount == 0 && state.Replacement == null && (!flush || !state.Pending);
        if (flush && completed) state.Jef = config.InitialJef;
    }
    private static void Push(ref DecoderState state, ulong key)
    {
        state.Push(Mapping.Scalar(key));
        int suffix = Mapping.Suffix(key);
        if (suffix != 0) state.Push(suffix);
    }
    private static void Replace(DecoderFallback fallback, ref DecoderState state, byte[] bytes, int index)
    {
        var buffer = fallback.CreateFallbackBuffer();
        buffer.Fallback(bytes, index);
        var text = new StringBuilder();
        while (buffer.Remaining > 0) text.Append(buffer.GetNextChar());
        state.Replacement = text.ToString(); state.ReplacementIndex = 0;
    }
}
