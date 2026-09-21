#pragma warning disable
namespace Jef4Net.Nec;
using System.Text;
using Jef4Net.Nec.Internal;
/// <summary>Provides the named NEC JIS8, EBCDIK, JIPS(J), and JIPS(E) encodings.</summary>
public sealed class NecEncodingProvider : EncodingProvider
{
    private NecEncodingProvider() { }
    /// <summary>Gets the provider instance.</summary>
    public static NecEncodingProvider Instance { get; } = new NecEncodingProvider();
    /// <inheritdoc/>
    public override Encoding? GetEncoding(int codepage) => null;
    /// <inheritdoc/>
    public override Encoding? GetEncoding(string name)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (!name.StartsWith("x-NEC-", StringComparison.OrdinalIgnoreCase)) return null;
        string[] p=name.Substring(6).Split('+'); if(p.Length is < 1 or > 2)return null;
        bool aS=Sbcs(p[0],out bool ae), aJ=Jips(p[0],out bool aje,out bool ah);
        if(p.Length==1){if(aS)return Create(false,false,ae,false);if(aJ)return Create(false,true,aje,ah);return null;}
        if(aS&&Jips(p[1],out bool bje,out bool bh)&&ae==bje)return Create(true,false,ae,bh);
        if(aJ&&Sbcs(p[1],out bool be)&&aje==be)return Create(true,true,be,ah);
        return null;
    }
    /// <inheritdoc/>
    public override Encoding? GetEncoding(string name, EncoderFallback encoderFallback, DecoderFallback decoderFallback) { if(encoderFallback==null)throw new ArgumentNullException(nameof(encoderFallback));if(decoderFallback==null)throw new ArgumentNullException(nameof(decoderFallback));var e=this.GetEncoding(name);if(e==null)return null;var c=(Encoding)e.Clone();c.EncoderFallback=encoderFallback;c.DecoderFallback=decoderFallback;return c; }
    private static Encoding Create(bool mixed,bool initial,bool isE,bool hd){string s=isE?"EBCDIK":"JIS8",j=isE?"JIPSE":"JIPSJ";if(hd)j+="-HanyoDenshi";string n="x-NEC-"+(mixed?(initial?j+"+"+s:s+"+"+j):initial?j:s);return new NecEncoding(n,new Configuration(mixed,initial,isE,hd));}
    private static bool Sbcs(string s,out bool e){if(s.Equals("JIS8",StringComparison.OrdinalIgnoreCase)){e=false;return true;}if(s.Equals("EBCDIK",StringComparison.OrdinalIgnoreCase)){e=true;return true;}e=false;return false;}
    private static bool Jips(string s,out bool e,out bool hd){hd=s.EndsWith("-HanyoDenshi",StringComparison.OrdinalIgnoreCase);if(hd)s=s.Substring(0,s.Length-12);if(s.Equals("JIPSJ",StringComparison.OrdinalIgnoreCase)){e=false;return true;}if(s.Equals("JIPSE",StringComparison.OrdinalIgnoreCase)){e=true;return true;}e=false;return false;}
}
