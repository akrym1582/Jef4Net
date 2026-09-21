#pragma warning disable SA1107, SA1501, SA1502, SA1503, SA1513, SA1516, SA1520, SA1600
namespace Jef4Net.Unisys.Internal;

using System.Text;

internal static class UnisysDecoderCore
{
    internal static void Convert(Configuration config, DecoderFallback fallback, ref DecoderState state, ReadOnlySpan<byte> input, Span<char> output, bool flush, bool count, out int used, out int written, out bool completed)
    {
        used = written = 0;
        while (true)
        {
            while (state.Replacement != null && state.ReplacementIndex < state.Replacement.Length && (count || written < output.Length))
            { if (!count) output[written] = state.Replacement[state.ReplacementIndex]; written = checked(written + 1); state.ReplacementIndex++; }
            if (state.Replacement != null)
            {
                if (state.ReplacementIndex < state.Replacement.Length) break;
                state.Replacement = null; state.ReplacementIndex = 0;
            }
            if (used == input.Length)
            {
                if (!flush || state.Pending == 0) break;
                byte pending = (byte)(state.Pending - 1); state.Pending = 0; Replace(fallback, ref state, new[] { pending }, used - 1); continue;
            }
            if (state.Pending != 0)
            {
                byte first = (byte)(state.Pending - 1); state.Pending = 0;
                byte second = input[used++];
                if (config.Mixed && first == 0x93) { state.Dbcs = (second & 1) == 0; continue; }
                int scalar = Mapping.DecodeDbcs((first << 8) | second);
                if (scalar < 0) Replace(fallback, ref state, new[] { first, second }, used - 2);
                else if (count || written < output.Length) { if (!count) output[written] = (char)scalar; written++; }
                else { used--; state.Pending = first + 1; break; }
                continue;
            }
            byte value = input[used++];
            if (config.Mixed && value == 0x93) { state.Pending = value + 1; continue; }
            if (state.Dbcs) { state.Pending = value + 1; continue; }
            int decoded = Mapping.DecodeSbcs(value);
            if (decoded < 0) Replace(fallback, ref state, new[] { value }, used - 1);
            else if (count || written < output.Length) { if (!count) output[written] = (char)decoded; written++; }
            else { used--; break; }
        }
        completed = used == input.Length && state.Replacement == null && (!flush || state.Pending == 0);
        if (flush && completed) state.Dbcs = !config.Mixed;
    }
    private static void Replace(DecoderFallback fallback, ref DecoderState state, byte[] bytes, int index)
    {
        DecoderFallbackBuffer buffer = fallback.CreateFallbackBuffer(); buffer.Fallback(bytes, index);
        var text = new StringBuilder(); while (buffer.Remaining > 0) text.Append(buffer.GetNextChar());
        state.Replacement = text.ToString(); state.ReplacementIndex = 0;
    }
}
