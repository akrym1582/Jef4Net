#pragma warning disable SA1107, SA1128, SA1136, SA1402, SA1500, SA1502, SA1503, SA1513, SA1514, SA1516, SA1600, SA1602, SA1642, SA1649
namespace Jef4Net.Unisys;
using System.Text;
/// <summary>Resolves the five canonical JBIS encoding names.</summary>
public sealed class JbisEncodingProvider : EncodingProvider
{
    private JbisEncodingProvider() { }
    /// <summary>Gets the shared provider instance.</summary>
    public static JbisEncodingProvider Instance { get; } = new JbisEncodingProvider();
    /// <inheritdoc/>
    public override Encoding? GetEncoding(int codepage) => null;
    /// <inheritdoc/>
    public override Encoding? GetEncoding(string name)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (name.Equals("jbis7", StringComparison.OrdinalIgnoreCase)) return new Jbis7Encoding();
        if (name.Equals("jbis8", StringComparison.OrdinalIgnoreCase)) return new Jbis8Encoding();
        if (name.Equals("jis-ascii-jbis7", StringComparison.OrdinalIgnoreCase)) return new JisAsciiJbis7Encoding();
        if (name.Equals("japan-ebcdic-jbis8", StringComparison.OrdinalIgnoreCase)) return new JapanEbcdicJbis8Encoding();
        if (name.Equals("japan-v24-jbis8", StringComparison.OrdinalIgnoreCase)) return new JapanV24Jbis8Encoding();
        return null;
    }
    /// <inheritdoc/>
    public override Encoding? GetEncoding(string name, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
    {
        if (encoderFallback == null) throw new ArgumentNullException(nameof(encoderFallback));
        if (decoderFallback == null) throw new ArgumentNullException(nameof(decoderFallback));
        Encoding? value = this.GetEncoding(name); if (value == null) return null;
        value = (Encoding)value.Clone(); value.EncoderFallback = encoderFallback; value.DecoderFallback = decoderFallback; return value;
    }
}
