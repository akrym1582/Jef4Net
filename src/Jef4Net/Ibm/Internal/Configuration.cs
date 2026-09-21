#pragma warning disable SA1107, SA1501, SA1502, SA1503, SA1513, SA1516, SA1520, SA1600
namespace Jef4Net.Ibm.Internal;

internal readonly struct Configuration
{
    internal readonly bool Mixed;
    internal readonly bool InitialDbcs;
    internal readonly int SbcsKind;
    internal readonly int DbcsKind;
    internal Configuration(bool mixed, bool initialDbcs, int sbcsKind, int dbcsKind)
    { this.Mixed = mixed; this.InitialDbcs = initialDbcs; this.SbcsKind = sbcsKind; this.DbcsKind = dbcsKind; }
}

internal struct EncoderState
{
    internal bool Dbcs; internal char High; internal int Prefix; internal ulong Output; internal int OutputCount;
    internal string? Replacement; internal int ReplacementIndex;
    internal bool Pending => this.High != 0 || this.Prefix != 0 || this.OutputCount != 0 || this.Replacement != null;
    internal void Push(byte value) { this.Output |= (ulong)value << (this.OutputCount * 8); this.OutputCount++; }
}

internal struct DecoderState
{
    internal bool Dbcs; internal int Lead; internal ulong Output; internal int OutputCount;
    internal string? Replacement; internal int ReplacementIndex;
    internal bool Pending => this.Lead != 0 || this.OutputCount != 0 || this.Replacement != null;
    internal void Push(int scalar)
    {
        if (scalar <= 0xFFFF) this.PushChar((char)scalar);
        else { scalar -= 0x10000; this.PushChar((char)(0xD800 + (scalar >> 10))); this.PushChar((char)(0xDC00 + (scalar & 1023))); }
    }
    private void PushChar(char value) { this.Output |= (ulong)value << (this.OutputCount * 16); this.OutputCount++; }
}
