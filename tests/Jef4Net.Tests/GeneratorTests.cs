using Jef4Net.CodeGen;

namespace Jef4Net.Tests;

public class GeneratorTests
{
    [Fact]
    public void RealDataIsDeterministic()
    {
        string data = Path.Combine(AppContext.BaseDirectory, "data");
        Assert.Equal(MappingGenerator.Generate(data), MappingGenerator.Generate(data));
    }
    [Theory]
    [InlineData("[{\"code\":\"10000\",\"unicode\":\"0041\",\"options\":[]}]")]
    [InlineData("[{\"code\":\"4040\",\"unicode\":\"110000\",\"options\":[]}]")]
    [InlineData("[{\"code\":\"4040\",\"unicode\":\"D800\",\"options\":[]}]")]
    [InlineData("[{\"code\":\"4040\",\"unicode\":\"0041\",\"options\":[\"encode_only\",\"decode_only\"]}]")]
    [InlineData("[{\"code\":\"4040\",\"unicode\":\"0041\",\"options\":[\"unknown\"]}]")]
    [InlineData("[{\"code\":\"4040\",\"unicode\":\"0041\",\"options\":[]},{\"code\":\"4040\",\"unicode\":\"0041\",\"options\":[]}]")]
    [InlineData("[{\"code\":\"4040\",\"unicode\":\"0041\",\"options\":[]},{\"code\":\"4040\",\"unicode\":\"0042\",\"options\":[]}]")]
    [InlineData("[{\"code\":\"4040\",\"unicode\":\"0041\",\"options\":[]},{\"code\":\"41A1\",\"unicode\":\"0041\",\"options\":[]}]")]
    public void InvalidDataIsRejected(string json)
    {
        string dir = Path.Combine(Path.GetTempPath(), "jef4net-generator-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "fujitsu_jef_mapping.json"), json);
            Assert.Throws<InvalidDataException>(() => MappingGenerator.Generate(dir));
        }
        finally { Directory.Delete(dir, true); }
    }
}
