namespace Jef4Net.Fujitsu.Internal;

internal static class Mapping
{
    internal static ulong Key(int scalar, int suffix = 0) => ((ulong)(scalar + 1) << 21) | (uint)suffix;
    internal static int Scalar(ulong key) => (int)(key >> 21) - 1;
    internal static int Suffix(ulong key) => (int)(key & 0x1FFFFF);
    internal static ulong Decode(bool jef, int kind, int code, JefProfile profile)
    {
        if (jef && code >> 8 is >= 0x80 and <= 0xA0 && (code & 255) is >= 0xA1 and <= 0xFE)
            return Key(0xE000 + ((code >> 8) - 0x80) * 94 + (code & 255) - 0xA1);
        if (jef) return profile switch
        {
            JefProfile.Roundtrip => FujitsuTables.JefRoundtripEntries[FujitsuTables.JefRoundtripIds[code]],
            JefProfile.HanyoDenshi => FujitsuTables.JefHanyoEntries[FujitsuTables.JefHanyoIds[code]],
            _ => FujitsuTables.JefEntries[FujitsuTables.JefIds[code]]
        };
        return (kind == 0 ? FujitsuTables.LowerDecode : kind == 1 ? FujitsuTables.KanaDecode : FujitsuTables.AsciiDecode)[code];
    }
    internal static int Encode(bool jef, int kind, ulong key, JefProfile profile)
    {
        int scalar = Scalar(key);
        if (jef && Suffix(key) == 0 && scalar is >= 0xE000 and <= 0xEC1D)
            return ((0x80 + (scalar - 0xE000) / 94) << 8) | (0xA1 + (scalar - 0xE000) % 94);
        var keys = jef ? profile switch
        {
            JefProfile.Roundtrip => FujitsuTables.JefRoundtripKeys,
            JefProfile.HanyoDenshi => FujitsuTables.JefHanyoKeys,
            _ => FujitsuTables.JefKeys
        } : kind == 0 ? FujitsuTables.LowerKeys : kind == 1 ? FujitsuTables.KanaKeys : FujitsuTables.AsciiKeys;
        var codes = jef ? profile switch
        {
            JefProfile.Roundtrip => FujitsuTables.JefRoundtripCodes,
            JefProfile.HanyoDenshi => FujitsuTables.JefHanyoCodes,
            _ => FujitsuTables.JefCodes
        } : kind == 0 ? FujitsuTables.LowerCodes : kind == 1 ? FujitsuTables.KanaCodes : FujitsuTables.AsciiCodes;
        int lo = 0, hi = keys.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (keys[mid] == key) return codes[mid];
            if (keys[mid] < key) lo = mid + 1; else hi = mid - 1;
        }
        return -1;
    }
    internal static bool IsPrefix(int scalar, JefProfile profile) => (profile switch
    {
        JefProfile.Roundtrip => FujitsuTables.JefRoundtripPrefixes,
        JefProfile.HanyoDenshi => FujitsuTables.JefHanyoPrefixes,
        _ => FujitsuTables.JefPrefixes
    }).IndexOf(scalar) >= 0;
}

internal enum JefProfile { Normal, Roundtrip, HanyoDenshi }

internal readonly struct Configuration
{
    internal readonly bool Mixed, InitialJef;
    internal readonly int Kind;
    internal readonly JefProfile Profile;
    internal Configuration(bool mixed, bool jef, int kind, JefProfile profile = JefProfile.Normal)
    { Mixed = mixed; InitialJef = jef; Kind = kind; Profile = profile; }
}

internal struct EncoderState
{
    internal bool Jef;
    internal char High;
    internal int Prefix; // scalar+1
    internal ulong Output;
    internal int OutputCount;
    internal string? Replacement;
    internal int ReplacementIndex;
    internal bool Pending => High != 0 || Prefix != 0 || OutputCount != 0 || Replacement != null;
    internal void Push(byte value) { Output |= (ulong)value << (OutputCount * 8); OutputCount++; }
}

internal struct DecoderState
{
    internal bool Jef;
    internal int Lead; // byte+1
    internal bool K2;
    internal ulong Output; // queued UTF-16 units
    internal int OutputCount;
    internal string? Replacement;
    internal int ReplacementIndex;
    internal bool Pending => Lead != 0 || K2 || OutputCount != 0 || Replacement != null;
    internal void Push(int scalar)
    {
        if (scalar <= 0xFFFF) PushChar((char)scalar);
        else { scalar -= 0x10000; PushChar((char)(0xD800 + (scalar >> 10))); PushChar((char)(0xDC00 + (scalar & 1023))); }
    }
    private void PushChar(char value) { Output |= (ulong)value << (OutputCount * 16); OutputCount++; }
}
