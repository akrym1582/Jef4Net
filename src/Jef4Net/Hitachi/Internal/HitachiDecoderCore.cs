namespace Jef4Net.Hitachi.Internal;

using System.Text;

/// <summary>Implements the shared stateful decoding conversion.</summary>
internal static class HitachiDecoderCore
{
    /// <summary>Converts Hitachi bytes to UTF-16 output.</summary>
    /// <param name="config">The encoding configuration.</param>
    /// <param name="fallback">The fallback used for undecodable input.</param>
    /// <param name="state">The conversion state.</param>
    /// <param name="input">The input bytes.</param>
    /// <param name="output">The output buffer.</param>
    /// <param name="flush">Whether to flush pending state.</param>
    /// <param name="count">Whether to count output without writing.</param>
    /// <param name="used">The number of input bytes consumed.</param>
    /// <param name="written">The number of output characters written or counted.</param>
    /// <param name="completed">Whether the conversion completed.</param>
    internal static void Convert(
        Configuration config,
        DecoderFallback fallback,
        ref DecoderState state,
        ReadOnlySpan<byte> input,
        Span<char> output,
        bool flush,
        bool count,
        out int used,
        out int written,
        out bool completed)
    {
        used = written = 0;
        while (true)
        {
            while (state.OutputCount != 0 && (count || written < output.Length))
            {
                if (!count)
                {
                    output[written] = (char)state.Output;
                }

                written = checked(written + 1);
                state.Output >>= 16;
                state.OutputCount--;
            }

            if (state.OutputCount != 0)
            {
                break;
            }

            while (state.Replacement != null && state.ReplacementIndex < state.Replacement.Length && (count || written < output.Length))
            {
                if (!count)
                {
                    output[written] = state.Replacement[state.ReplacementIndex];
                }

                written = checked(written + 1);
                state.ReplacementIndex++;
            }

            if (state.Replacement != null)
            {
                if (state.ReplacementIndex != state.Replacement.Length)
                {
                    break;
                }

                state.Replacement = null;
                state.ReplacementIndex = 0;
            }

            if (used == input.Length && (!flush || (state.Lead == 0 && !state.K2)))
            {
                break;
            }

            if (state.K2)
            {
                state.K2 = false;
                if (used < input.Length && input[used] is 0x41 or 0x42)
                {
                    state.Keis = input[used++] == 0x42;
                    continue;
                }

                Replace(fallback, ref state, new byte[] { 0x0A }, used - 1);
                continue;
            }

            if (state.Lead != 0)
            {
                byte lead = (byte)(state.Lead - 1);
                state.Lead = 0;
                if (used == input.Length)
                {
                    Replace(fallback, ref state, new[] { lead }, used - 1);
                    continue;
                }

                // Invalid trails are left for the next iteration, so a following shift can resynchronize.
                byte trail = input[used];
                if (!(trail is >= 0xA1 and <= 0xFE || (lead == 0x40 && trail == 0x40)))
                {
                    Replace(fallback, ref state, new[] { lead }, used - 1);
                    continue;
                }

                used++;
                if (lead == 0x40 && trail == 0x40 && config.ShiftSpaceSingle)
                {
                    state.Push(0x20);
                    state.Push(0x20);
                    continue;
                }

                ulong key = Mapping.Decode(true, config.KeisKind, (lead << 8) | trail, config.HanyoDenshi);
                if (key == 0)
                {
                    Replace(fallback, ref state, new[] { lead, trail }, used - 2);
                }
                else
                {
                    Push(ref state, key);
                }

                continue;
            }

            byte b = input[used++];
            if (config.Mixed && b == 0x0A)
            {
                state.K2 = true;
                continue;
            }

            if (state.Keis)
            {
                if (b is >= 0x40 and <= 0xFE)
                {
                    state.Lead = b + 1;
                }
                else
                {
                    Replace(fallback, ref state, new[] { b }, used - 1);
                }
            }
            else
            {
                ulong key = Mapping.Decode(false, config.SbcsKind, b, config.HanyoDenshi);
                if (key == 0)
                {
                    Replace(fallback, ref state, new[] { b }, used - 1);
                }
                else
                {
                    Push(ref state, key);
                }
            }
        }

        completed = used == input.Length && state.OutputCount == 0 && state.Replacement == null && (!flush || !state.Pending);
        if (flush && completed)
        {
            state.Keis = config.InitialKeis;
        }
    }

    private static void Push(ref DecoderState state, ulong key)
    {
        state.Push(Mapping.Scalar(key));
        int suffix = Mapping.Suffix(key);
        if (suffix != 0)
        {
            state.Push(suffix);
        }
    }

    private static void Replace(DecoderFallback fallback, ref DecoderState state, byte[] bytes, int index)
    {
        var buffer = fallback.CreateFallbackBuffer();
        buffer.Fallback(bytes, index);
        var text = new StringBuilder();
        while (buffer.Remaining > 0)
        {
            text.Append(buffer.GetNextChar());
        }

        state.Replacement = text.ToString();
        state.ReplacementIndex = 0;
    }
}
