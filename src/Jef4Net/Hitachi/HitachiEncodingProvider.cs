#pragma warning disable SA1107, SA1501, SA1502, SA1503, SA1513, SA1516, SA1520, SA1600
namespace Jef4Net.Hitachi;

using System.Text;
using Jef4Net.Hitachi.Internal;

/// <summary>Provides the named Hitachi EBCDIC, EBCDIK, KEIS78, and KEIS83 encodings.</summary>
public sealed class HitachiEncodingProvider : EncodingProvider
{
    private HitachiEncodingProvider() { }

    /// <summary>Gets the provider instance to register with <see cref="Encoding.RegisterProvider"/>.</summary>
    public static HitachiEncodingProvider Instance { get; } = new HitachiEncodingProvider();

    /// <inheritdoc/>
    public override Encoding? GetEncoding(int codepage) => null;

    /// <inheritdoc/>
    public override Encoding? GetEncoding(string name)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (!name.StartsWith("x-Hitachi-", StringComparison.OrdinalIgnoreCase)) return null;
        string suffix = name.Substring(10);
        string[] parts = suffix.Split('+');
        if (parts.Length is < 1 or > 2) return null;

        bool firstKeis = TryParseKeis(parts[0], out int keisKind, out bool shiftSpace, out bool hanyo);
        bool firstSbcs = TryParseSbcs(parts[0], out int sbcsKind);
        if (parts.Length == 1)
        {
            if (firstKeis) return Create(name, false, true, 0, keisKind, shiftSpace, hanyo);
            if (firstSbcs) return Create(name, false, false, sbcsKind, 0, false, false);
            return null;
        }

        if (firstSbcs && TryParseKeis(parts[1], out keisKind, out shiftSpace, out hanyo))
            return Create(name, true, false, sbcsKind, keisKind, shiftSpace, hanyo);
        if (firstKeis && TryParseSbcs(parts[1], out sbcsKind))
            return Create(name, true, true, sbcsKind, keisKind, shiftSpace, hanyo);
        return null;
    }

    /// <inheritdoc/>
    public override Encoding? GetEncoding(string name, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
    {
        if (encoderFallback == null) throw new ArgumentNullException(nameof(encoderFallback));
        if (decoderFallback == null) throw new ArgumentNullException(nameof(decoderFallback));
        Encoding? encoding = this.GetEncoding(name);
        if (encoding == null) return null;
        var clone = (Encoding)encoding.Clone();
        clone.EncoderFallback = encoderFallback;
        clone.DecoderFallback = decoderFallback;
        return clone;
    }

    private static HitachiEncoding Create(string requested, bool mixed, bool initialKeis, int sbcs, int keis, bool space, bool hanyo)
        => new HitachiEncoding(Canonical(mixed, initialKeis, sbcs, keis, space, hanyo), new Configuration(mixed, initialKeis, sbcs, keis, space, hanyo));

    private static string Canonical(bool mixed, bool initialKeis, int sbcs, int keis, bool space, bool hanyo)
    {
        string s = sbcs == 0 ? "EBCDIC" : "EBCDIK";
        string k = keis == 0 ? "KEIS78" : "KEIS83";
        if (space) k += "-ShiftSpaceSingle";
        if (hanyo) k += "-HanyoDenshi";
        return "x-Hitachi-" + (mixed ? (initialKeis ? k + "+" + s : s + "+" + k) : initialKeis ? k : s);
    }

    private static bool TryParseSbcs(string value, out int kind)
    {
        if (value.Equals("EBCDIC", StringComparison.OrdinalIgnoreCase)) { kind = 0; return true; }
        if (value.Equals("EBCDIK", StringComparison.OrdinalIgnoreCase)) { kind = 1; return true; }
        kind = 0; return false;
    }

    private static bool TryParseKeis(string value, out int kind, out bool space, out bool hanyo)
    {
        hanyo = value.EndsWith("-HanyoDenshi", StringComparison.OrdinalIgnoreCase);

        // The pinned Hitachi mapping files contain no HanyoDenshi/IVS data. Do not expose a
        // profile that would silently behave exactly like the normal KEIS mapping.
        if (hanyo) { kind = 0; space = false; return false; }
        space = value.EndsWith("-ShiftSpaceSingle", StringComparison.OrdinalIgnoreCase);
        if (space) value = value.Substring(0, value.Length - 17);
        if (value.Equals("KEIS78", StringComparison.OrdinalIgnoreCase)) { kind = 0; return true; }
        if (value.Equals("KEIS83", StringComparison.OrdinalIgnoreCase)) { kind = 1; return true; }
        kind = 0; return false;
    }
}
