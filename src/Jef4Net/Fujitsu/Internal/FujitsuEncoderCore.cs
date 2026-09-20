namespace Jef4Net.Fujitsu.Internal;

using System.Text;

/// <summary>Implements the shared stateful encoding conversion.</summary>
internal static class FujitsuEncoderCore
{
    /// <summary>Converts UTF-16 input to Fujitsu bytes.</summary>
    /// <param name="config">The encoding configuration.</param>
    /// <param name="fallback">The fallback used for unencodable input.</param>
    /// <param name="state">The conversion state.</param>
    /// <param name="input">The input characters.</param>
    /// <param name="output">The output buffer.</param>
    /// <param name="flush">Whether to flush pending state.</param>
    /// <param name="count">Whether to count output without writing.</param>
    /// <param name="used">The number of input characters consumed.</param>
    /// <param name="written">The number of output bytes written or counted.</param>
    /// <param name="completed">Whether the conversion completed.</param>
    internal static void Convert(
        Configuration config,
        EncoderFallback fallback,
        ref EncoderState state,
        ReadOnlySpan<char> input,
        Span<byte> output,
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
                    output[written] = (byte)state.Output;
                }

                written = checked(written + 1);
                state.Output >>= 8;
                state.OutputCount--;
            }

            if (state.OutputCount != 0)
            {
                break;
            }

            if (state.Replacement != null && state.ReplacementIndex == state.Replacement.Length)
            {
                state.Replacement = null;
                state.ReplacementIndex = 0;
            }

            bool replacing = state.Replacement != null;
            ReadOnlySpan<char> source = replacing ? state.Replacement.AsSpan(state.ReplacementIndex) : input.Slice(used);
            if (source.IsEmpty && state.High == 0 && state.Prefix == 0)
            {
                if (flush && config.Mixed && state.Jef != config.InitialJef)
                {
                    state.Push(config.InitialJef ? (byte)0x28 : (byte)0x29);
                    state.Jef = config.InitialJef;
                    continue;
                }

                break;
            }

            int consumed = 0, scalar;
            bool malformed = false;
            char high = state.High;
            if (high != 0)
            {
                if (source.IsEmpty && !flush)
                {
                    break;
                }

                if (!source.IsEmpty && char.IsLowSurrogate(source[0]))
                {
                    scalar = char.ConvertToUtf32(high, source[0]);
                    consumed = 1;
                }
                else
                {
                    scalar = high;
                    malformed = true;
                }
            }
            else if (source.IsEmpty)
            {
                if (!flush)
                {
                    break;
                }

                scalar = -1;
            }
            else
            {
                scalar = source[0];
                consumed = 1;
                if (char.IsHighSurrogate(source[0]))
                {
                    if (source.Length > 1 && char.IsLowSurrogate(source[1]))
                    {
                        scalar = char.ConvertToUtf32(source[0], source[1]);
                        consumed = 2;
                    }
                    else if (source.Length == 1 && !flush && !replacing)
                    {
                        state.High = source[0];
                        used++;
                        break;
                    }
                    else
                    {
                        malformed = true;
                    }
                }
                else if (char.IsLowSurrogate(source[0]))
                {
                    malformed = true;
                }
            }

            if (state.Prefix != 0)
            {
                int prefix = state.Prefix - 1;
                int pair = scalar > 0 && !malformed ? Mapping.Encode(true, config.Kind, Mapping.Key(prefix, scalar), config.Profile) : -1;
                if (pair >= 0)
                {
                    Emit(config, ref state, true, pair);
                    state.Prefix = 0;
                    state.High = '\0';
                    if (replacing)
                    {
                        state.ReplacementIndex += consumed;
                    }
                    else
                    {
                        used += consumed;
                    }

                    continue;
                }

                state.Prefix = 0;
                if (!TryEmit(config, ref state, Mapping.Key(prefix)))
                {
                    Replace(fallback, ref state, prefix, Math.Max(-2, used - (prefix > 0xFFFF ? 2 : 1)), replacing);
                }

                continue; // The lookahead belongs to the next character.
            }

            if (scalar == -1)
            {
                break;
            }

            state.High = '\0';
            int index = high != 0 ? used - 1 : used;
            if (replacing)
            {
                state.ReplacementIndex += consumed;
            }
            else
            {
                used += consumed;
            }

            if (!malformed && (config.Mixed || config.InitialJef) && Mapping.IsPrefix(scalar, config.Profile))
            {
                state.Prefix = scalar + 1;
                continue;
            }

            if (malformed || !TryEmit(config, ref state, Mapping.Key(scalar)))
            {
                Replace(fallback, ref state, scalar, index, replacing);
            }
        }

        completed = used == input.Length && state.OutputCount == 0 && state.Replacement == null && (!flush || !state.Pending);
    }

    private static bool TryEmit(Configuration config, ref EncoderState state, ulong key)
    {
        if (config.Mixed || !config.InitialJef)
        {
            int code = Mapping.Encode(false, config.Kind, key, config.Profile);

            // Mixed encodings reserve these bytes for shift syntax (including K2 prefix).
            if (code >= 0 && (!config.Mixed || code is not (0x28 or 0x29 or 0x30 or 0x38)))
            {
                Emit(config, ref state, false, code);
                return true;
            }
        }

        if (config.Mixed || config.InitialJef)
        {
            int code = Mapping.Encode(true, config.Kind, key, config.Profile);
            if (code >= 0)
            {
                Emit(config, ref state, true, code);
                return true;
            }
        }

        return false;
    }

    private static void Emit(Configuration config, ref EncoderState state, bool jef, int code)
    {
        if (config.Mixed && state.Jef != jef)
        {
            state.Push(jef ? (byte)0x28 : (byte)0x29);
            state.Jef = jef;
        }

        if (jef)
        {
            state.Push((byte)(code >> 8));
        }

        state.Push((byte)code);
    }

    private static void Replace(EncoderFallback fallback, ref EncoderState state, int scalar, int index, bool recursive)
    {
        if (recursive)
        {
            throw new ArgumentException("Encoder fallback contains an unencodable character.");
        }

        var buffer = fallback.CreateFallbackBuffer();
        if (scalar > 0xFFFF)
        {
            int n = scalar - 0x10000;
            buffer.Fallback((char)(0xD800 + (n >> 10)), (char)(0xDC00 + (n & 1023)), index);
        }
        else
        {
            buffer.Fallback((char)scalar, index);
        }

        var text = new StringBuilder();
        while (buffer.Remaining > 0)
        {
            text.Append(buffer.GetNextChar());
        }

        state.Replacement = text.ToString();
        state.ReplacementIndex = 0;
    }
}
