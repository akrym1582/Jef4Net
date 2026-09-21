#pragma warning disable SA1107, SA1128, SA1136, SA1402, SA1500, SA1502, SA1503, SA1513, SA1514, SA1516, SA1600, SA1602, SA1642, SA1649
namespace Jef4Net.Unisys;
using Jef4Net.Unisys.Jbis.Internal;

/// <summary>Pure 16-bit JBIS7 encoding.</summary>
public sealed class Jbis7Encoding : JbisEncoding { /// <summary>Creates a JBIS7 encoding.</summary>
    public Jbis7Encoding() : base("jbis7", new Configuration(JbisKind.Jbis7)) { } }
/// <summary>Pure 16-bit JBIS8 encoding.</summary>
public sealed class Jbis8Encoding : JbisEncoding { /// <summary>Creates a JBIS8 encoding.</summary>
    public Jbis8Encoding() : base("jbis8", new Configuration(JbisKind.Jbis8)) { } }
/// <summary>JISASCII SBCS and JBIS7 DBCS mixed encoding.</summary>
public sealed class JisAsciiJbis7Encoding : JbisEncoding { /// <summary>Creates a JISASCIIJBIS7 encoding.</summary>
    public JisAsciiJbis7Encoding() : base("jis-ascii-jbis7", new Configuration(JbisKind.Jbis7, SbcsKind.JisAscii, 0x9E, 0x9F)) { } }
/// <summary>JapanEBCDIC SBCS and JBIS8 DBCS mixed encoding.</summary>
public sealed class JapanEbcdicJbis8Encoding : JbisEncoding { /// <summary>Creates a JapanEBCDICJBIS8 encoding.</summary>
    public JapanEbcdicJbis8Encoding() : base("japan-ebcdic-jbis8", new Configuration(JbisKind.Jbis8, SbcsKind.JapanEbcdic, 0x2B, 0x2C)) { } }
/// <summary>JapanV24 SBCS and JBIS8 DBCS mixed encoding.</summary>
public sealed class JapanV24Jbis8Encoding : JbisEncoding { /// <summary>Creates a JapanV24JBIS8 encoding.</summary>
    public JapanV24Jbis8Encoding() : base("japan-v24-jbis8", new Configuration(JbisKind.Jbis8, SbcsKind.JapanV24, 0x2B, 0x2C)) { } }
