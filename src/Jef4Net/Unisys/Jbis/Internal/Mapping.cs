#pragma warning disable SA1107, SA1128, SA1136, SA1402, SA1500, SA1502, SA1503, SA1513, SA1514, SA1516, SA1600, SA1602, SA1642, SA1649
namespace Jef4Net.Unisys.Jbis.Internal;

internal enum JbisPlane { Invalid, JisX0208, JisX0212, Custom }
internal static class JbisCodeClassifier
{
    internal static JbisPlane Classify(JbisKind kind, byte lead, byte trail)
    {
        if (kind == JbisKind.Jbis7 && lead is >= 0x21 and <= 0x7E && trail is >= 0x21 and <= 0x7E) return JbisPlane.JisX0208;
        if (kind == JbisKind.Jbis8 && lead is >= 0xA1 and <= 0xFE && trail is >= 0xA1 and <= 0xFE) return JbisPlane.JisX0208;
        if (lead is >= 0xA1 and <= 0xFE && trail is >= 0x41 and <= 0x9E) return JbisPlane.JisX0212;
        if (lead is >= 0x41 and <= 0x9E && trail is >= 0xA1 and <= 0xFE) return JbisPlane.Custom;
        return JbisPlane.Invalid;
    }
}
internal static class Mapping
{
    internal static int DecodeDbcs(JbisKind kind, int code)
    {
        byte lead = (byte)(code >> 8), trail = (byte)code; JbisPlane plane = JbisCodeClassifier.Classify(kind, lead, trail);
        if (plane == JbisPlane.JisX0208)
        {
            int lets = kind == JbisKind.Jbis7 ? code + 0x8080 : code; if (lets == 0xA1A1) lets = 0x2020;
            return global::Jef4Net.Unisys.Internal.Mapping.DecodeDbcs(lets);
        }
        if (plane == JbisPlane.JisX0212) return global::Jef4Net.Unisys.Internal.Mapping.DecodeDbcs(code - 0x20);
        return -1;
    }
    internal static int EncodeDbcs(JbisKind kind, int scalar)
    {
        int lets = global::Jef4Net.Unisys.Internal.Mapping.EncodeDbcs(scalar); if (lets < 0) return -1;
        if (lets == 0x2020) return kind == JbisKind.Jbis7 ? 0x2121 : 0xA1A1;
        byte lead = (byte)(lets >> 8), trail = (byte)lets;
        if (lead is >= 0xA1 and <= 0xFE && trail is >= 0xA1 and <= 0xFE) return kind == JbisKind.Jbis7 ? lets - 0x8080 : lets;
        if (lead is >= 0xA1 and <= 0xFE && trail is >= 0x21 and <= 0x7E) return lets + 0x20;
        return -1;
    }
    internal static int DecodeSbcs(SbcsKind kind, byte code) => kind switch
    {
        SbcsKind.JisAscii => JbisTables.JisAsciiDecode[code],
        SbcsKind.JapanEbcdic => JbisTables.JapanEbcdicDecode[code],
        SbcsKind.JapanV24 => JbisTables.JapanV24Decode[code],
        _ => -1,
    };
    internal static int EncodeSbcs(SbcsKind kind, int scalar) => kind switch
    {
        SbcsKind.JisAscii => Find(JbisTables.JisAsciiEncodeKeys, JbisTables.JisAsciiEncodeCodes, scalar),
        SbcsKind.JapanEbcdic => Find(JbisTables.JapanEbcdicEncodeKeys, JbisTables.JapanEbcdicEncodeCodes, scalar),
        SbcsKind.JapanV24 => Find(JbisTables.JapanV24EncodeKeys, JbisTables.JapanV24EncodeCodes, scalar),
        _ => -1,
    };
    private static int Find(ReadOnlySpan<int> keys, ReadOnlySpan<ushort> codes, int scalar)
    { int index = keys.BinarySearch(scalar); return index < 0 ? -1 : codes[index]; }
}
