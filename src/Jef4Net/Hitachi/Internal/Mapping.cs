#pragma warning disable SA1107, SA1501, SA1502, SA1503, SA1513, SA1516, SA1520, SA1600
namespace Jef4Net.Hitachi.Internal;

internal static class Mapping
{
    internal static ulong Key(int scalar, int suffix = 0) => ((ulong)(scalar + 1) << 21) | (uint)suffix;
    internal static int Scalar(ulong key) => (int)(key >> 21) - 1;
    internal static int Suffix(ulong key) => (int)(key & 0x1FFFFF);

    internal static ulong Decode(bool keis, int kind, int code, bool hanyoDenshi)
    {
        if (keis && code >> 8 is >= 0x81 and <= 0xA0 && (code & 255) is >= 0xA1 and <= 0xFE)
            return Key(0xE000 + (((code >> 8) - 0x81) * 94) + (code & 255) - 0xA1);
        if (keis && code == 0x4040) return Key(0x3000);
        return keis
            ? (kind == 0 ? HitachiTables.Keis78Decode : HitachiTables.Keis83Decode)[code]
            : (kind == 0 ? HitachiTables.EbcdicDecode : HitachiTables.EbcdikDecode)[code];
    }

    internal static int Encode(bool keis, int kind, ulong key, bool hanyoDenshi)
    {
        int scalar = Scalar(key);
        if (keis && Suffix(key) == 0 && scalar is >= 0xE000 and <= 0xEBBF)
            return ((0x81 + ((scalar - 0xE000) / 94)) << 8) | (0xA1 + ((scalar - 0xE000) % 94));
        var keys = keis ? (kind == 0 ? HitachiTables.Keis78Keys : HitachiTables.Keis83Keys)
                        : (kind == 0 ? HitachiTables.EbcdicKeys : HitachiTables.EbcdikKeys);
        var codes = keis ? (kind == 0 ? HitachiTables.Keis78Codes : HitachiTables.Keis83Codes)
                         : (kind == 0 ? HitachiTables.EbcdicCodes : HitachiTables.EbcdikCodes);
        int lo = 0, hi = keys.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) / 2);
            if (keys[mid] == key) return codes[mid];
            if (keys[mid] < key) lo = mid + 1; else hi = mid - 1;
        }
        return -1;
    }

    // The fixed Hitachi JSON contains scalar mappings only. Kept for the shared longest-match state machine.
    internal static bool IsPrefix(int scalar, bool hanyoDenshi) => false;
}
