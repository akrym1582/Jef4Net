using System.Text;

namespace Jef4Net.Tests;

public sealed class Jef4NetEncodingProviderTests
{
    [Theory]
    [InlineData("x-Fujitsu-JEF")]
    [InlineData("x-Hitachi-KEIS83")]
    [InlineData("x-NEC-JIPSJ")]
    [InlineData("x-IBM-1390")]
    [InlineData("x-Unisys-LETSJ")]
    [InlineData("japan-ebcdic-jbis8")]
    public void ResolvesNamesFromEveryProvider(string name)
    {
        Encoding? encoding = Jef4NetEncodingProvider.Instance.GetEncoding(name);

        Assert.NotNull(encoding);
    }

    [Fact]
    public void ResolvesSupportedNumericCodePages()
    {
        Assert.Equal("x-IBM-8482+16684", Jef4NetEncodingProvider.Instance.GetEncoding(1390)!.WebName);
        Assert.Null(Jef4NetEncodingProvider.Instance.GetEncoding(12345));
    }

    [Fact]
    public void RegisteredProviderResolvesNamesAndFallbacks()
    {
        Encoding.RegisterProvider(Jef4NetEncodingProvider.Instance);

        Encoding encoding = Encoding.GetEncoding(
            "x-Unisys-LETSJ",
            EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);

        Assert.Equal("x-Unisys-LETSJ", encoding.WebName);
        Assert.Same(EncoderFallback.ExceptionFallback, encoding.EncoderFallback);
        Assert.Same(DecoderFallback.ExceptionFallback, encoding.DecoderFallback);
    }

    [Fact]
    public void RejectsUnknownNamesAndNullArguments()
    {
        Assert.Null(Jef4NetEncodingProvider.Instance.GetEncoding("x-unknown"));
        Assert.Throws<ArgumentNullException>(() => Jef4NetEncodingProvider.Instance.GetEncoding(null!));
        Assert.Throws<ArgumentNullException>(() => Jef4NetEncodingProvider.Instance.GetEncoding(
            "x-Fujitsu-JEF", null!, DecoderFallback.ExceptionFallback));
        Assert.Throws<ArgumentNullException>(() => Jef4NetEncodingProvider.Instance.GetEncoding(
            "x-Fujitsu-JEF", EncoderFallback.ExceptionFallback, null!));
    }
}
