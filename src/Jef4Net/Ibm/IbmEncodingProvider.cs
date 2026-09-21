#pragma warning disable SA1107, SA1501, SA1502, SA1503, SA1513, SA1516, SA1520, SA1600, SA1514
namespace Jef4Net.Ibm;

using System.Text;
using Jef4Net.Ibm.Internal;

/// <summary>Provides IBM Japanese Host CCSID 8482, 5123, 16684, 1390, and 1399 encodings.</summary>
public sealed class IbmEncodingProvider : EncodingProvider
{
    private IbmEncodingProvider() { }
    /// <summary>Gets the provider instance.</summary>
    public static IbmEncodingProvider Instance { get; } = new IbmEncodingProvider();
    /// <inheritdoc/>
    public override Encoding? GetEncoding(int codepage) => codepage switch { 8482 => Create(false, false, 8482, 0), 5123 => Create(false, false, 5123, 0), 16684 => Create(false, true, 0, 16684), 1390 => Create(true, false, 8482, 16684), 1399 => Create(true, false, 5123, 16684), _ => null };
    /// <inheritdoc/>
    public override Encoding? GetEncoding(string name)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (!name.StartsWith("x-IBM-", StringComparison.OrdinalIgnoreCase)) return null;
        string suffix = name.Substring(6).Replace("11684", "16684", StringComparison.OrdinalIgnoreCase);
        if (suffix.Equals("1390", StringComparison.OrdinalIgnoreCase)) return Create(true, false, 8482, 16684);
        if (suffix.Equals("1399", StringComparison.OrdinalIgnoreCase)) return Create(true, false, 5123, 16684);
        string[] parts = suffix.Split('+');
        if (parts.Length == 1 && int.TryParse(parts[0], out int value)) return this.GetEncoding(value);
        if (parts.Length != 2) return null;
        if (TrySbcs(parts[0], out int sbcs) && IsDbcs(parts[1])) return Create(true, false, sbcs, 16684);
        if (IsDbcs(parts[0]) && TrySbcs(parts[1], out sbcs)) return Create(true, true, sbcs, 16684);
        return null;
    }
    /// <inheritdoc/>
    public override Encoding? GetEncoding(string name, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
    {
        if (encoderFallback == null) throw new ArgumentNullException(nameof(encoderFallback));
        if (decoderFallback == null) throw new ArgumentNullException(nameof(decoderFallback));
        Encoding? value = this.GetEncoding(name); if (value == null) return null;
        var clone = (Encoding)value.Clone(); clone.EncoderFallback = encoderFallback; clone.DecoderFallback = decoderFallback; return clone;
    }
    private static Encoding Create(bool mixed, bool initialDbcs, int sbcs, int dbcs)
    {
        string name = !mixed ? $"x-IBM-{(initialDbcs ? dbcs : sbcs)}" : $"x-IBM-{(initialDbcs ? dbcs : sbcs)}+{(initialDbcs ? sbcs : dbcs)}";
        return new IbmEncoding(name, new Configuration(mixed, initialDbcs, sbcs, dbcs));
    }
    private static bool TrySbcs(string value, out int kind) { bool ok = int.TryParse(value, out kind) && kind is 8482 or 5123; return ok; }
    private static bool IsDbcs(string value) => value.Equals("16684", StringComparison.OrdinalIgnoreCase);
}
