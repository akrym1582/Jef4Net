namespace Jef4Net.Fujitsu.Internal;

internal static class Bounds
{
    internal static void Check(int length, int index, int count)
    {
        if (index < 0 || index > length) throw new ArgumentOutOfRangeException(nameof(index));
        if (count < 0 || count > length - index) throw new ArgumentOutOfRangeException(nameof(count));
    }
    internal static Span<T> Slice<T>(T[] array, int index, int count)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        Check(array.Length, index, count);
        return array.AsSpan(index, count);
    }
    internal static Span<T> Tail<T>(T[] array, int index)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        Check(array.Length, index, 0);
        return array.AsSpan(index);
    }
}
