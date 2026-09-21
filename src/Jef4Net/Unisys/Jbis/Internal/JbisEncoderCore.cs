#pragma warning disable SA1107, SA1501, SA1502, SA1503, SA1513, SA1516, SA1520, SA1600
namespace Jef4Net.Unisys.Jbis.Internal;
using System.Text;
internal static class JbisEncoderCore
{
    internal static void Convert(Configuration config, EncoderFallback fallback, ref EncoderState state, ReadOnlySpan<char> input, Span<byte> output, bool flush, bool count, out int used, out int written, out bool completed)
    {
        used = written = 0;
        while (true)
        {
            while (state.OutputCount > 0 && (count || written < output.Length)) { if (!count) output[written] = (byte)state.Output; written = checked(written + 1); state.Output >>= 8; state.OutputCount--; }
            if (state.OutputCount > 0) break;
            if (state.Replacement != null && state.ReplacementIndex == state.Replacement.Length) { state.Replacement = null; state.ReplacementIndex = 0; }
            bool replacing = state.Replacement != null; ReadOnlySpan<char> source = replacing ? state.Replacement.AsSpan(state.ReplacementIndex) : input.Slice(used);
            if (source.IsEmpty && state.High == 0)
            {
                if (flush && config.Mixed && state.Dbcs) { state.Push(config.Edo); state.Dbcs = false; continue; }
                break;
            }
            int scalar, consumed = 0; bool malformed = false; char high = state.High;
            if (high != 0)
            {
                if (source.IsEmpty && !flush) break;
                if (!source.IsEmpty && char.IsLowSurrogate(source[0])) { scalar = char.ConvertToUtf32(high, source[0]); consumed = 1; } else { scalar = high; malformed = true; }
            }
            else
            {
                char ch = source[0]; scalar = ch; consumed = 1;
                if (char.IsHighSurrogate(ch)) { if (source.Length > 1 && char.IsLowSurrogate(source[1])) { scalar = char.ConvertToUtf32(ch, source[1]); consumed = 2; } else if (source.Length == 1 && !flush && !replacing) { state.High = ch; used++; break; } else malformed = true; }
                else if (char.IsLowSurrogate(ch)) malformed = true;
            }
            state.High = '\0'; int index = high != 0 ? used - 1 : used; if (replacing) state.ReplacementIndex += consumed; else used += consumed;
            int sbcs = !malformed && config.Mixed ? Mapping.EncodeSbcs(config.Sbcs, scalar) : -1;
            int dbcs = !malformed ? Mapping.EncodeDbcs(config.Dbcs, scalar) : -1;
            if (sbcs >= 0) Emit(config, ref state, false, sbcs); else if (dbcs >= 0) Emit(config, ref state, true, dbcs); else Replace(fallback, ref state, scalar, index, replacing);
        }
        completed = used == input.Length && state.OutputCount == 0 && state.Replacement == null && (!flush || !state.Pending);
    }
    private static void Emit(Configuration config, ref EncoderState state, bool dbcs, int code)
    { if (config.Mixed && state.Dbcs != dbcs) { state.Push(dbcs ? config.Sdo : config.Edo); state.Dbcs = dbcs; } if (dbcs) state.Push((byte)(code >> 8)); state.Push((byte)code); }
    private static void Replace(EncoderFallback fallback, ref EncoderState state, int scalar, int index, bool recursive)
    { if (recursive) throw new ArgumentException("Encoder fallback contains an unencodable character."); EncoderFallbackBuffer buffer = fallback.CreateFallbackBuffer(); if (scalar > 0xFFFF) { int n = scalar - 0x10000; buffer.Fallback((char)(0xD800 + (n >> 10)), (char)(0xDC00 + (n & 1023)), index); } else buffer.Fallback((char)scalar, index); var text = new StringBuilder(); while (buffer.Remaining > 0) text.Append(buffer.GetNextChar()); state.Replacement = text.ToString(); state.ReplacementIndex = 0; }
}
