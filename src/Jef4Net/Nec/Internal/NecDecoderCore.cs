namespace Jef4Net.Nec.Internal;

using System.Text;

/// <summary>Implements the shared stateful decoding conversion.</summary>
internal static class NecDecoderCore
{
    /// <summary>Converts Nec bytes to UTF-16 output.</summary>
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

            if (used == input.Length && (!flush || (state.Lead == 0 && state.ShiftPrefix == 0)))
            {
                break;
            }

            if (state.ShiftPrefix != 0)
            {
                byte prefix = (byte)(state.ShiftPrefix - 1);
                state.ShiftPrefix = 0;
                byte kanji = config.IsE ? (byte)0x75 : (byte)0x70;
                byte single = config.IsE ? (byte)0x76 : (byte)0x71;
                if (used < input.Length && (input[used] == kanji || input[used] == single))
                {
                    state.Jips = input[used++] == kanji;
                    continue;
                }

                Replace(fallback, ref state, new[] { prefix }, used - 1);
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

                byte trail = input[used];
                used++;

                ulong key = Mapping.Decode(true, config.IsE, (lead << 8) | trail, config.HanyoDenshi);
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
            byte shiftPrefix = config.IsE ? (byte)0x3F : (byte)0x1A;
            if (config.Mixed && b == shiftPrefix)
            {
                state.ShiftPrefix = b + 1;
                continue;
            }

            if (state.Jips)
            {
                state.Lead = b + 1;
            }
            else
            {
                ulong key = Mapping.Decode(false, config.IsE, b, config.HanyoDenshi);
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
            state.Jips = config.InitialJips;
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
