using System.Text;
using Jef4Net.Fujitsu.Internal;

namespace Jef4Net.Fujitsu;

/// <summary>Provides named Fujitsu JEF and EBCDIC encodings.</summary>
public sealed class FujitsuEncodingProvider : EncodingProvider
{
    /// <summary>Gets the provider to register with <see cref="Encoding.RegisterProvider"/>.</summary>
    public static FujitsuEncodingProvider Instance { get; } = new FujitsuEncodingProvider();
    private FujitsuEncodingProvider() { }
    /// <summary>Returns null; this provider assigns no numeric code pages.</summary>
    public override Encoding? GetEncoding(int codepage) => null;
    /// <summary>Resolves a canonical name or an alias, ignoring case.</summary>
    public override Encoding? GetEncoding(string name)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        string suffix = name.StartsWith("x-Fujitsu-", StringComparison.OrdinalIgnoreCase) ? name.Substring(10) : name;
        if (suffix.Equals("JEF", StringComparison.OrdinalIgnoreCase))
            return new FujitsuEncoding("x-Fujitsu-JEF", new Configuration(false, true, 0));
        if (suffix.Equals("JEF-Roundtrip", StringComparison.OrdinalIgnoreCase))
            return new FujitsuEncoding("x-Fujitsu-JEF-Roundtrip", new Configuration(false, true, 0, JefProfile.Roundtrip));
        if (suffix.Equals("JEF-HanyoDenshi", StringComparison.OrdinalIgnoreCase))
            return new FujitsuEncoding("x-Fujitsu-JEF-HanyoDenshi", new Configuration(false, true, 0, JefProfile.HanyoDenshi));
        string[] kinds = { "Lower", "Kana", "Ascii" };
        for (int i = 0; i < kinds.Length; i++)
        {
            string ebcdic = "EBCDIC-" + kinds[i];
            if (suffix.Equals(ebcdic, StringComparison.OrdinalIgnoreCase))
                return new FujitsuEncoding("x-Fujitsu-" + ebcdic, new Configuration(false, false, i));
            if (suffix.Equals(ebcdic + "+JEF", StringComparison.OrdinalIgnoreCase))
                return new FujitsuEncoding("x-Fujitsu-" + ebcdic + "+JEF", new Configuration(true, false, i));
            if (suffix.Equals("JEF+" + ebcdic, StringComparison.OrdinalIgnoreCase))
                return new FujitsuEncoding("x-Fujitsu-JEF+" + ebcdic, new Configuration(true, true, i));
            if (suffix.Equals(ebcdic + "+JEF-HanyoDenshi", StringComparison.OrdinalIgnoreCase))
                return new FujitsuEncoding("x-Fujitsu-" + ebcdic + "+JEF-HanyoDenshi", new Configuration(true, false, i, JefProfile.HanyoDenshi));
            if (suffix.Equals("JEF-HanyoDenshi+" + ebcdic, StringComparison.OrdinalIgnoreCase))
                return new FujitsuEncoding("x-Fujitsu-JEF-HanyoDenshi+" + ebcdic, new Configuration(true, true, i, JefProfile.HanyoDenshi));
        }
        return null;
    }
    /// <summary>Resolves a name with the supplied .NET fallback policies.</summary>
    public override Encoding? GetEncoding(string name, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
    {
        if (encoderFallback == null) throw new ArgumentNullException(nameof(encoderFallback));
        if (decoderFallback == null) throw new ArgumentNullException(nameof(decoderFallback));
        var encoding = GetEncoding(name);
        if (encoding == null) return null;
        var clone = (Encoding)encoding.Clone();
        clone.EncoderFallback = encoderFallback; clone.DecoderFallback = decoderFallback;
        return clone;
    }
}
