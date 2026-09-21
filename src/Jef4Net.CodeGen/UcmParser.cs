using System.Globalization;
using System.Text.RegularExpressions;

namespace Jef4Net.CodeGen;

internal enum MappingDirection { RoundTrip, Fallback, Substitution, DecodeOnly, EncodeOnly }
internal sealed record UcmMapping(int[] Scalars, byte[] Bytes, MappingDirection Direction);
internal sealed record UcmFile(IReadOnlyDictionary<string, string> Headers, IReadOnlyList<string> States, IReadOnlyList<UcmMapping> Mappings);

internal static partial class UcmParser
{
    internal static UcmFile Parse(string path)
    {
        var headers = new Dictionary<string, string>(StringComparer.Ordinal);
        var states = new List<string>(); var mappings = new List<UcmMapping>(); bool charmap = false;
        foreach (string raw in File.ReadLines(path))
        {
            string line = raw.Split('#')[0].Trim(); if (line.Length == 0) continue;
            if (line == "CHARMAP") { charmap = true; continue; } if (line == "END CHARMAP") break;
            if (!charmap)
            {
                Match header = HeaderRegex().Match(line); if (!header.Success) continue;
                if (header.Groups[1].Value == "icu:state") states.Add(header.Groups[2].Value.Trim()); else headers[header.Groups[1].Value] = header.Groups[2].Value.Trim().Trim('"');
                continue;
            }
            Match match = MappingRegex().Match(line); if (!match.Success) throw new InvalidDataException($"Invalid UCM mapping: {raw}");
            int[] scalars = ScalarRegex().Matches(match.Groups[1].Value).Select(x => int.Parse(x.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            byte[] bytes = ByteRegex().Matches(match.Groups[2].Value).Select(x => byte.Parse(x.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            if (scalars.Length is < 1 or > 2 || bytes.Length is < 1 or > 2 || scalars.Any(x => x > 0x10FFFF || x is >= 0xD800 and <= 0xDFFF)) throw new InvalidDataException($"Unsupported UCM mapping: {raw}");
            int precision = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            mappings.Add(new UcmMapping(scalars, bytes, (MappingDirection)precision));
        }
        return new UcmFile(headers, states, mappings);
    }
    [GeneratedRegex(@"^<([^>]+)>\s+(.+)$")] private static partial Regex HeaderRegex();
    [GeneratedRegex(@"^((?:<U[0-9A-Fa-f]{4,6}>)+)\s+((?:\\x[0-9A-Fa-f]{2}){1,2})\s+\|([0-4])$")] private static partial Regex MappingRegex();
    [GeneratedRegex(@"<U([0-9A-Fa-f]{4,6})>")] private static partial Regex ScalarRegex();
    [GeneratedRegex(@"\\x([0-9A-Fa-f]{2})")] private static partial Regex ByteRegex();
}
