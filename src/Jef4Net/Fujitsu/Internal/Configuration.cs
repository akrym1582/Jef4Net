namespace Jef4Net.Fujitsu.Internal;

/// <summary>Stores the immutable configuration for an encoding.</summary>
internal readonly struct Configuration
{
    /// <summary>Indicates whether shift sequences are enabled.</summary>
    internal readonly bool Mixed;

    /// <summary>Indicates whether the initial state is JEF.</summary>
    internal readonly bool InitialJef;

    /// <summary>Gets the EBCDIC table kind.</summary>
    internal readonly int Kind;

    /// <summary>Gets the JEF mapping profile.</summary>
    internal readonly JefProfile Profile;

    /// <summary>Initializes a new instance of the <see cref="Configuration"/> struct.</summary>
    /// <param name="mixed">Whether shift sequences are enabled.</param>
    /// <param name="jef">Whether the initial state is JEF.</param>
    /// <param name="kind">The EBCDIC table kind.</param>
    /// <param name="profile">The JEF mapping profile.</param>
    internal Configuration(bool mixed, bool jef, int kind, JefProfile profile = JefProfile.Normal)
    {
        this.Mixed = mixed;
        this.InitialJef = jef;
        this.Kind = kind;
        this.Profile = profile;
    }
}

/// <summary>Stores state held by an encoder between conversions.</summary>
internal struct EncoderState
{
    /// <summary>Indicates the active JEF state.</summary>
    internal bool Jef;

    /// <summary>Stores a pending high surrogate.</summary>
    internal char High;

    /// <summary>Stores a pending JEF prefix.</summary>
    internal int Prefix; // scalar+1

    /// <summary>Stores queued output bytes.</summary>
    internal ulong Output;

    /// <summary>Gets the number of queued output bytes.</summary>
    internal int OutputCount;

    /// <summary>Stores fallback replacement text.</summary>
    internal string? Replacement;

    /// <summary>Gets the current replacement position.</summary>
    internal int ReplacementIndex;

    /// <summary>Gets a value indicating whether conversion state is pending.</summary>
    internal bool Pending => this.High != 0 || this.Prefix != 0 || this.OutputCount != 0 || this.Replacement != null;

    /// <summary>Queues an output byte.</summary>
    /// <param name="value">The byte to queue.</param>
    internal void Push(byte value)
    {
        this.Output |= (ulong)value << (this.OutputCount * 8);
        this.OutputCount++;
    }
}

/// <summary>Stores state held by a decoder between conversions.</summary>
internal struct DecoderState
{
    /// <summary>Indicates the active JEF state.</summary>
    internal bool Jef;

    /// <summary>Stores a pending JEF lead byte plus one.</summary>
    internal int Lead; // byte+1

    /// <summary>Indicates a pending K2 prefix.</summary>
    internal bool K2;

    /// <summary>Stores queued UTF-16 units.</summary>
    internal ulong Output; // queued UTF-16 units

    /// <summary>Gets the number of queued output units.</summary>
    internal int OutputCount;

    /// <summary>Stores fallback replacement text.</summary>
    internal string? Replacement;

    /// <summary>Gets the current replacement position.</summary>
    internal int ReplacementIndex;

    /// <summary>Gets a value indicating whether conversion state is pending.</summary>
    internal bool Pending => this.Lead != 0 || this.K2 || this.OutputCount != 0 || this.Replacement != null;

    /// <summary>Queues the UTF-16 representation of a scalar.</summary>
    /// <param name="scalar">The Unicode scalar value.</param>
    internal void Push(int scalar)
    {
        if (scalar <= 0xFFFF)
        {
            this.PushChar((char)scalar);
        }
        else
        {
            scalar -= 0x10000;
            this.PushChar((char)(0xD800 + (scalar >> 10)));
            this.PushChar((char)(0xDC00 + (scalar & 1023)));
        }
    }

    private void PushChar(char value)
    {
        this.Output |= (ulong)value << (this.OutputCount * 16);
        this.OutputCount++;
    }
}
