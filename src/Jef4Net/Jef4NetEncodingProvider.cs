namespace Jef4Net;

using System.Text;
using Jef4Net.Fujitsu;
using Jef4Net.Hitachi;
using Jef4Net.Ibm;
using Jef4Net.Nec;
using Jef4Net.Unisys;

/// <summary>Provides all named encodings exposed by Jef4Net encoding providers.</summary>
/// <remarks>
/// MELCOM encodings are not included because their options, including optional external
/// extension tables, must be supplied when constructing them.
/// </remarks>
public sealed class Jef4NetEncodingProvider : EncodingProvider
{
    private static readonly EncodingProvider[] Providers =
    {
        FujitsuEncodingProvider.Instance,
        HitachiEncodingProvider.Instance,
        NecEncodingProvider.Instance,
        IbmEncodingProvider.Instance,
        UnisysEncodingProvider.Instance,
        JbisEncodingProvider.Instance,
    };

    private Jef4NetEncodingProvider()
    {
    }

    /// <summary>Gets the provider to register with <see cref="Encoding.RegisterProvider"/>.</summary>
    public static Jef4NetEncodingProvider Instance { get; } = new Jef4NetEncodingProvider();

    /// <summary>Resolves a numeric code page supported by any Jef4Net provider.</summary>
    /// <param name="codepage">The code page to resolve.</param>
    /// <returns>The matching encoding, or null when the code page is unknown.</returns>
    public override Encoding? GetEncoding(int codepage)
    {
        foreach (EncodingProvider provider in Providers)
        {
            Encoding? encoding = provider.GetEncoding(codepage);
            if (encoding != null)
            {
                return encoding;
            }
        }

        return null;
    }

    /// <summary>Resolves a name supported by any Jef4Net provider.</summary>
    /// <param name="name">The encoding name or alias.</param>
    /// <returns>The matching encoding, or null when the name is unknown.</returns>
    public override Encoding? GetEncoding(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        foreach (EncodingProvider provider in Providers)
        {
            Encoding? encoding = provider.GetEncoding(name);
            if (encoding != null)
            {
                return encoding;
            }
        }

        return null;
    }

    /// <summary>Resolves a name with the supplied .NET fallback policies.</summary>
    /// <param name="name">The encoding name or alias.</param>
    /// <param name="encoderFallback">The fallback used for encoding failures.</param>
    /// <param name="decoderFallback">The fallback used for decoding failures.</param>
    /// <returns>The matching encoding with the supplied fallbacks, or null when the name is unknown.</returns>
    public override Encoding? GetEncoding(string name, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (encoderFallback == null)
        {
            throw new ArgumentNullException(nameof(encoderFallback));
        }

        if (decoderFallback == null)
        {
            throw new ArgumentNullException(nameof(decoderFallback));
        }

        foreach (EncodingProvider provider in Providers)
        {
            Encoding? encoding = provider.GetEncoding(name, encoderFallback, decoderFallback);
            if (encoding != null)
            {
                return encoding;
            }
        }

        return null;
    }
}
