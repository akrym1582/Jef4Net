#pragma warning disable
namespace Jef4Net.Nec.Internal;
internal static class Mapping
{
    internal static ulong Key(int scalar, int suffix = 0) => ((ulong)(scalar + 1) << 21) | (uint)suffix;
    internal static int Scalar(ulong key) => (int)(key >> 21) - 1;
    internal static int Suffix(ulong key) => (int)(key & 0x1FFFFF);
    internal static ulong Decode(bool jips, bool isE, int code, bool hd)
    {
        if (!jips) return (isE ? NecTables.EbcdikDecode : NecTables.Jis8Decode)[code];
        int jcode = code;
        if (isE) { int a = NecTables.EbcdikToJis8[code >> 8], b = NecTables.EbcdikToJis8[code & 255]; if (a < 0 || b < 0) return 0; jcode = (a << 8) | b; }
        int lead = jcode >> 8, trail = jcode & 255;
        if (lead is >= 0x74 and <= 0x7E && trail is >= 0x21 and <= 0x7E) return Key(0xE000 + ((lead - 0x74) * 94) + trail - 0x21);
        if (lead is >= 0xE0 and <= 0xFE && trail is >= 0xA1 and <= 0xFE) return Key(0xE40A + ((lead - 0xE0) * 94) + trail - 0xA1);
        return (hd ? NecTables.JipsHdDecode : NecTables.JipsDecode)[jcode];
    }
    internal static int Encode(bool jips, bool isE, ulong key, bool hd)
    {
        int scalar = Scalar(key), code;
        if (jips && Suffix(key) == 0 && scalar is >= 0xE000 and <= 0xE409) code = ((0x74 + ((scalar - 0xE000) / 94)) << 8) | (0x21 + ((scalar - 0xE000) % 94));
        else if (jips && Suffix(key) == 0 && scalar is >= 0xE40A and <= 0xEF6B) code = ((0xE0 + ((scalar - 0xE40A) / 94)) << 8) | (0xA1 + ((scalar - 0xE40A) % 94));
        else { var keys = jips ? (hd ? NecTables.JipsHdKeys : NecTables.JipsKeys) : (isE ? NecTables.EbcdikKeys : NecTables.Jis8Keys); var codes = jips ? (hd ? NecTables.JipsHdCodes : NecTables.JipsCodes) : (isE ? NecTables.EbcdikCodes : NecTables.Jis8Codes); int lo=0,hi=keys.Length-1; code=-1; while(lo<=hi){int m=lo+((hi-lo)/2); if(keys[m]==key){code=codes[m];break;} if(keys[m]<key)lo=m+1;else hi=m-1;} }
        if (code < 0 || !jips || !isE) return code;
        int a = NecTables.Jis8ToEbcdik[code >> 8], b = NecTables.Jis8ToEbcdik[code & 255]; return a < 0 || b < 0 ? -1 : (a << 8) | b;
    }
    internal static bool IsPrefix(int scalar, bool hd) { var keys = hd ? NecTables.JipsHdKeys : NecTables.JipsKeys; ulong min=Key(scalar), max=Key(scalar,0x1FFFFF); int lo=0,hi=keys.Length-1; while(lo<=hi){int m=lo+((hi-lo)/2);if(keys[m]<=min)lo=m+1;else hi=m-1;} return lo<keys.Length && keys[lo]<=max; }
}
