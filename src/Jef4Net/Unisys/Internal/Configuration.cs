#pragma warning disable SA1107, SA1501, SA1502, SA1503, SA1513, SA1516, SA1520, SA1600
namespace Jef4Net.Unisys.Internal;

internal readonly struct Configuration
{
    internal Configuration(bool mixed) => this.Mixed = mixed;
    internal bool Mixed { get; }
}

internal struct EncoderState
{
    internal bool Dbcs;
    internal char High;
    internal ulong Output;
    internal int OutputCount;
    internal string? Replacement;
    internal int ReplacementIndex;
    internal bool Pending => this.High != 0 || this.OutputCount != 0 || this.Replacement != null;
    internal void Push(byte value) { this.Output |= (ulong)value << (this.OutputCount * 8); this.OutputCount++; }
}

internal struct DecoderState
{
    internal bool Dbcs;
    internal int Pending;
    internal string? Replacement;
    internal int ReplacementIndex;
    internal bool PendingData => this.Pending != 0 || this.Replacement != null;
}
