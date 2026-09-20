namespace Jef4Net.Fujitsu.Internal;

/// <summary>Resolves the generated character mappings.</summary>
internal static class Mapping
{
    /// <summary>Creates a mapping key from a scalar and optional suffix.</summary>
    /// <param name="scalar">The Unicode scalar value.</param>
    /// <param name="suffix">The optional mapping suffix.</param>
    /// <returns>The packed mapping key.</returns>
    internal static ulong Key(int scalar, int suffix = 0) => ((ulong)(scalar + 1) << 21) | (uint)suffix;

    /// <summary>Extracts the scalar value from a mapping key.</summary>
    /// <param name="key">The packed mapping key.</param>
    /// <returns>The Unicode scalar value.</returns>
    internal static int Scalar(ulong key) => (int)(key >> 21) - 1;

    /// <summary>Extracts the suffix from a mapping key.</summary>
    /// <param name="key">The packed mapping key.</param>
    /// <returns>The mapping suffix.</returns>
    internal static int Suffix(ulong key) => (int)(key & 0x1FFFFF);

    /// <summary>Decodes a Fujitsu code into a mapping key.</summary>
    /// <param name="jef">Whether the code is in JEF form.</param>
    /// <param name="kind">The EBCDIC table kind.</param>
    /// <param name="code">The encoded value.</param>
    /// <param name="profile">The JEF mapping profile.</param>
    /// <returns>The packed mapping key, or zero when the value is unmapped.</returns>
    internal static ulong Decode(bool jef, int kind, int code, JefProfile profile)
    {
        if (jef && code >> 8 is >= 0x80 and <= 0xA0 && (code & 255) is >= 0xA1 and <= 0xFE)
        {
            return Key(0xE000 + (((code >> 8) - 0x80) * 94) + (code & 255) - 0xA1);
        }

        if (jef)
        {
            return profile switch
        {
            JefProfile.Roundtrip => FujitsuTables.JefRoundtripEntries[FujitsuTables.JefRoundtripIds[code]],
            JefProfile.HanyoDenshi => FujitsuTables.JefHanyoEntries[FujitsuTables.JefHanyoIds[code]],
            _ => FujitsuTables.JefEntries[FujitsuTables.JefIds[code]],
        };
        }

        return (kind == 0 ? FujitsuTables.LowerDecode : kind == 1 ? FujitsuTables.KanaDecode : FujitsuTables.AsciiDecode)[code];
    }

    /// <summary>Encodes a mapping key into a Fujitsu code.</summary>
    /// <param name="jef">Whether to use JEF form.</param>
    /// <param name="kind">The EBCDIC table kind.</param>
    /// <param name="key">The packed mapping key.</param>
    /// <param name="profile">The JEF mapping profile.</param>
    /// <returns>The encoded value, or -1 when the value is unmapped.</returns>
    internal static int Encode(bool jef, int kind, ulong key, JefProfile profile)
    {
        int scalar = Scalar(key);
        if (jef && Suffix(key) == 0 && scalar is >= 0xE000 and <= 0xEC1D)
        {
            return ((0x80 + ((scalar - 0xE000) / 94)) << 8) | (0xA1 + ((scalar - 0xE000) % 94));
        }

        var keys = jef ? profile switch
        {
            JefProfile.Roundtrip => FujitsuTables.JefRoundtripKeys,
            JefProfile.HanyoDenshi => FujitsuTables.JefHanyoKeys,
            _ => FujitsuTables.JefKeys,
        } : kind == 0 ? FujitsuTables.LowerKeys : kind == 1 ? FujitsuTables.KanaKeys : FujitsuTables.AsciiKeys;
        var codes = jef ? profile switch
        {
            JefProfile.Roundtrip => FujitsuTables.JefRoundtripCodes,
            JefProfile.HanyoDenshi => FujitsuTables.JefHanyoCodes,
            _ => FujitsuTables.JefCodes,
        } : kind == 0 ? FujitsuTables.LowerCodes : kind == 1 ? FujitsuTables.KanaCodes : FujitsuTables.AsciiCodes;
        int lo = 0, hi = keys.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) / 2);
            if (keys[mid] == key)
            {
                return codes[mid];
            }

            if (keys[mid] < key)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return -1;
    }

    /// <summary>Determines whether a scalar is a JEF prefix.</summary>
    /// <param name="scalar">The Unicode scalar value.</param>
    /// <param name="profile">The JEF mapping profile.</param>
    /// <returns>true when the scalar begins a prefixed mapping.</returns>
    internal static bool IsPrefix(int scalar, JefProfile profile) => (profile switch
    {
        JefProfile.Roundtrip => FujitsuTables.JefRoundtripPrefixes,
        JefProfile.HanyoDenshi => FujitsuTables.JefHanyoPrefixes,
        _ => FujitsuTables.JefPrefixes,
    }).IndexOf(scalar) >= 0;
}
