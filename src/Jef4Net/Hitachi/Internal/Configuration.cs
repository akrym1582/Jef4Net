#pragma warning disable SA1107, SA1501, SA1502, SA1503, SA1513, SA1516, SA1520, SA1600
namespace Jef4Net.Hitachi.Internal;

internal readonly struct Configuration
{
    internal readonly bool Mixed;
    internal readonly bool InitialKeis;
    internal readonly int SbcsKind;
    internal readonly int KeisKind;
    internal readonly bool ShiftSpaceSingle;
    internal readonly bool HanyoDenshi;

    internal Configuration(bool mixed, bool keis, int sbcsKind, int keisKind, bool shiftSpaceSingle = false, bool hanyoDenshi = false)
    {
        this.Mixed = mixed;
        this.InitialKeis = keis;
        this.SbcsKind = sbcsKind;
        this.KeisKind = keisKind;
        this.ShiftSpaceSingle = shiftSpaceSingle;
        this.HanyoDenshi = hanyoDenshi;
    }
}

internal struct EncoderState
{
    internal bool Keis;
    internal char High;
    internal int Prefix;
    internal ulong Output;
    internal int OutputCount;
    internal string? Replacement;
    internal int ReplacementIndex;
    internal bool Pending => this.High != 0 || this.Prefix != 0 || this.OutputCount != 0 || this.Replacement != null;
    internal void Push(byte value) { this.Output |= (ulong)value << (this.OutputCount * 8); this.OutputCount++; }
}

internal struct DecoderState
{
    internal bool Keis;
    internal int Lead;
    internal bool K2;
    internal ulong Output;
    internal int OutputCount;
    internal string? Replacement;
    internal int ReplacementIndex;
    internal bool Pending => this.Lead != 0 || this.K2 || this.OutputCount != 0 || this.Replacement != null;
    internal void Push(int scalar)
    {
        if (scalar <= 0xFFFF) this.PushChar((char)scalar);
        else { scalar -= 0x10000; this.PushChar((char)(0xD800 + (scalar >> 10))); this.PushChar((char)(0xDC00 + (scalar & 1023))); }
    }
    private void PushChar(char value) { this.Output |= (ulong)value << (this.OutputCount * 16); this.OutputCount++; }
}
