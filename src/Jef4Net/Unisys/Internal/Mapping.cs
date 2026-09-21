#pragma warning disable SA1107, SA1501, SA1502, SA1503, SA1513, SA1516, SA1520, SA1600
namespace Jef4Net.Unisys.Internal;

internal static class Mapping
{
    internal static int DecodeSbcs(byte code) => UnisysTables.SbcsDecode[code];
    internal static int DecodeDbcs(int code) => UnisysTables.DbcsDecode[code];
    internal static int EncodeSbcs(int scalar) => Find(UnisysTables.SbcsEncodeKeys, UnisysTables.SbcsEncodeCodes, scalar);
    internal static int EncodeDbcs(int scalar) => Find(UnisysTables.DbcsEncodeKeys, UnisysTables.DbcsEncodeCodes, scalar);

    private static int Find(ReadOnlySpan<int> keys, ReadOnlySpan<ushort> codes, int scalar)
    {
        int index = keys.BinarySearch(scalar);
        return index < 0 ? -1 : codes[index];
    }
}
