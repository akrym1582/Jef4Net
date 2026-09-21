#pragma warning disable SA1107, SA1501, SA1502, SA1503, SA1513, SA1514, SA1516, SA1520, SA1600
namespace Jef4Net.Unisys;

using System.Text;
using Jef4Net.Unisys.Internal;

/// <summary>Provides the mixed and DBCS-only Unisys LETS-J encodings.</summary>
public sealed class UnisysEncodingProvider : EncodingProvider
{
    private UnisysEncodingProvider() { }
    /// <summary>Gets the provider instance.</summary>
    public static UnisysEncodingProvider Instance { get; } = new UnisysEncodingProvider();
    /// <inheritdoc/>
    public override Encoding? GetEncoding(int codepage) => null;
    /// <inheritdoc/>
    public override Encoding? GetEncoding(string name)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (name.Equals("x-Unisys-LETSJ", StringComparison.OrdinalIgnoreCase)) return new UnisysEncoding("x-Unisys-LETSJ", new Configuration(true));
        if (name.Equals("x-Unisys-LETSJ-Kanji", StringComparison.OrdinalIgnoreCase)) return new UnisysEncoding("x-Unisys-LETSJ-Kanji", new Configuration(false));
        return null;
    }
    /// <inheritdoc/>
    public override Encoding? GetEncoding(string name, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
    {
        if (encoderFallback == null) throw new ArgumentNullException(nameof(encoderFallback));
        if (decoderFallback == null) throw new ArgumentNullException(nameof(decoderFallback));
        Encoding? result = this.GetEncoding(name);
        if (result == null) return null;
        result = (Encoding)result.Clone();
        result.EncoderFallback = encoderFallback;
        result.DecoderFallback = decoderFallback;
        return result;
    }
}
