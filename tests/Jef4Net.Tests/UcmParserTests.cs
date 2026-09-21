using Jef4Net.CodeGen;

namespace Jef4Net.Tests;

public sealed class UcmParserTests
{
    [Fact]
    public void ParsesScalarsSequencesByteLengthsAndEveryPrecision()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "<mb_cur_min> 1\n<mb_cur_max> 2\n<uconv_class> \"EBCDIC_STATEFUL\"\n<icu:state> 0-ff\nCHARMAP\n<U0041> \\xC1 |0\n<U2000B> \\xB3\\x42 |1\n<U0042> \\xC2 |2\n<U0043><U0300> \\x44\\x43 |3\n<U0044> \\x44\\x44 |4\nEND CHARMAP\n");
            UcmFile file = UcmParser.Parse(path);
            Assert.Equal(5, file.Mappings.Count);
            Assert.Equal(new[] { 0x43, 0x300 }, file.Mappings[3].Scalars);
            Assert.Equal(new byte[] { 0x44, 0x43 }, file.Mappings[3].Bytes);
            Assert.Equal(Enum.GetValues<MappingDirection>(), file.Mappings.Select(x => x.Direction));
        }
        finally { File.Delete(path); }
    }
}
