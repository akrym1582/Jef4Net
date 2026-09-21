#pragma warning disable SA1107, SA1128, SA1136, SA1402, SA1500, SA1502, SA1503, SA1513, SA1514, SA1516, SA1600, SA1602, SA1642, SA1649
#pragma warning disable SA1107, SA1501, SA1502, SA1503, SA1513, SA1516, SA1520, SA1600
namespace Jef4Net.Unisys.Jbis.Internal;

internal enum JbisKind { Jbis7, Jbis8 }
internal enum SbcsKind { None, JisAscii, JapanEbcdic, JapanV24 }
internal readonly struct Configuration
{
    internal Configuration(JbisKind dbcs, SbcsKind sbcs = SbcsKind.None, byte sdo = 0, byte edo = 0)
    { this.Dbcs = dbcs; this.Sbcs = sbcs; this.Sdo = sdo; this.Edo = edo; }
    internal JbisKind Dbcs { get; }
    internal SbcsKind Sbcs { get; }
    internal byte Sdo { get; }
    internal byte Edo { get; }
    internal bool Mixed => this.Sbcs != SbcsKind.None;
}
internal struct EncoderState
{
    internal bool Dbcs; internal char High; internal ulong Output; internal int OutputCount;
    internal string? Replacement; internal int ReplacementIndex;
    internal bool Pending => this.High != 0 || this.OutputCount != 0 || this.Replacement != null;
    internal void Push(byte value) { this.Output |= (ulong)value << (this.OutputCount * 8); this.OutputCount++; }
}
internal struct DecoderState
{
    internal bool Dbcs; internal int Pending; internal string? Replacement; internal int ReplacementIndex;
}
