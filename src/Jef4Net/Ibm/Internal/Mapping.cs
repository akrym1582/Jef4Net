#pragma warning disable SA1107, SA1501, SA1502, SA1503, SA1513, SA1516, SA1520, SA1600
namespace Jef4Net.Ibm.Internal;

internal static class Mapping
{
    internal static ulong Key(int scalar, int suffix = 0) => ((ulong)(scalar + 1) << 21) | (uint)suffix;
    internal static int Scalar(ulong key) => (int)(key >> 21) - 1;
    internal static int Suffix(ulong key) => (int)(key & 0x1FFFFF);
    internal static ulong Decode(bool dbcs, int kind, int code, bool unused = false)
        => dbcs ? IbmTables.Dbcs16684Decode[code] : (kind == 8482 ? IbmTables.Sbcs8482Decode : IbmTables.Sbcs5123Decode)[code];
    internal static int Encode(bool dbcs, int kind, ulong key, bool unused = false)
    {
        var keys = dbcs ? IbmTables.Dbcs16684Keys : kind == 8482 ? IbmTables.Sbcs8482Keys : IbmTables.Sbcs5123Keys;
        var codes = dbcs ? IbmTables.Dbcs16684Codes : kind == 8482 ? IbmTables.Sbcs8482Codes : IbmTables.Sbcs5123Codes;
        int lo = 0, hi = keys.Length - 1;
        while (lo <= hi) { int mid = lo + ((hi - lo) / 2); if (keys[mid] == key) return codes[mid]; if (keys[mid] < key) lo = mid + 1; else hi = mid - 1; }
        return -1;
    }
    internal static bool IsPrefix(int scalar, bool unused = false)
    {
        var values = IbmTables.Dbcs16684Prefixes;
        int lo = 0, hi = values.Length - 1;
        while (lo <= hi) { int mid = lo + ((hi - lo) / 2); if (values[mid] == scalar) return true; if (values[mid] < scalar) lo = mid + 1; else hi = mid - 1; }
        return false;
    }
}
