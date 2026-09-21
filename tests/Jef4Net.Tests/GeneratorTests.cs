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

    [Fact]
    public void UnisysRealDataIsDeterministic()
    {
        string data = Path.Combine(AppContext.BaseDirectory, "data", "unisys");
        Assert.Equal(UnisysMappingGenerator.Generate(data), UnisysMappingGenerator.Generate(data));
    }

    [Fact]
    public void JbisRealDataIsDeterministic()
    {
        string data = Path.Combine(AppContext.BaseDirectory, "data", "jbis");
        Assert.Equal(JbisMappingGenerator.Generate(data), JbisMappingGenerator.Generate(data));
    }

    [Fact]
    public void IbmRealDataIsDeterministic()
    {
        string data = Path.Combine(AppContext.BaseDirectory, "data", "icu", "ibm");
        Assert.Equal(IbmMappingGenerator.Generate(data), IbmMappingGenerator.Generate(data));
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
    [InlineData("[{\"code\":\"4040\",\"unicode\":\"0041\",\"sp\":\"3099\",\"hd\":\"E0100\",\"options\":[]}]")]
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

    [Fact]
    public void HitachiGeneratorRejectsUnknownSbcsDecodeConflicts()
    {
        string dir = Path.Combine(Path.GetTempPath(), "jef4net-hitachi-generator-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        try
        {
            const string conflict = "[{\"code\":\"8F\",\"unicode\":\"0041\",\"options\":[]},{\"code\":\"8F\",\"unicode\":\"0042\",\"options\":[]}]";
            const string empty = "[]";
            File.WriteAllText(Path.Combine(dir, "hitachi_ebcdic_mapping.json"), conflict);
            File.WriteAllText(Path.Combine(dir, "hitachi_ebcdik_mapping.json"), empty);
            File.WriteAllText(Path.Combine(dir, "hitachi_keis78_mapping.json"), empty);
            File.WriteAllText(Path.Combine(dir, "hitachi_keis83_mapping.json"), empty);
            Assert.Throws<InvalidDataException>(() => HitachiMappingGenerator.Generate(dir));
        }
        finally { Directory.Delete(dir, true); }
    }
}
