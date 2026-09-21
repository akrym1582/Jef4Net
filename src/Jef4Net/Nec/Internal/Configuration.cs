#pragma warning disable
namespace Jef4Net.Nec.Internal;
internal readonly struct Configuration
{
    internal readonly bool Mixed, InitialJips, IsE, HanyoDenshi;
    internal Configuration(bool mixed, bool initialJips, bool isE, bool hanyoDenshi) { this.Mixed = mixed; this.InitialJips = initialJips; this.IsE = isE; this.HanyoDenshi = hanyoDenshi; }
}
internal struct EncoderState
{
    internal bool Jips; internal char High; internal int Prefix; internal ulong Output; internal int OutputCount; internal string? Replacement; internal int ReplacementIndex;
    internal bool Pending => this.High != 0 || this.Prefix != 0 || this.OutputCount != 0 || this.Replacement != null;
    internal void Push(byte value) { this.Output |= (ulong)value << (this.OutputCount * 8); this.OutputCount++; }
}
internal struct DecoderState
{
    internal bool Jips; internal int Lead; internal int ShiftPrefix; internal ulong Output; internal int OutputCount; internal string? Replacement; internal int ReplacementIndex;
    internal bool Pending => this.Lead != 0 || this.ShiftPrefix != 0 || this.OutputCount != 0 || this.Replacement != null;
    internal void Push(int scalar) { if (scalar <= 0xFFFF) this.PushChar((char)scalar); else { scalar -= 0x10000; this.PushChar((char)(0xD800 + (scalar >> 10))); this.PushChar((char)(0xDC00 + (scalar & 1023))); } }
    private void PushChar(char value) { this.Output |= (ulong)value << (this.OutputCount * 16); this.OutputCount++; }
}
