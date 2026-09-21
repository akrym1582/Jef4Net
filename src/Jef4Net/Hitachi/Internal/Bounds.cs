namespace Jef4Net.Hitachi.Internal;

/// <summary>Validates array ranges used by the encoding adapters.</summary>
internal static class Bounds
{
    /// <summary>Validates an index and count within an array length.</summary>
    /// <param name="length">The length of the array.</param>
    /// <param name="index">The starting index.</param>
    /// <param name="count">The number of elements.</param>
    internal static void Check(int length, int index, int count)
    {
        if (index < 0 || index > length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (count < 0 || count > length - index)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
    }

    /// <summary>Returns a span over a validated array range.</summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="index">The starting index.</param>
    /// <param name="count">The number of elements.</param>
    /// <returns>A span over the requested range.</returns>
    internal static Span<T> Slice<T>(T[] array, int index, int count)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        Check(array.Length, index, count);
        return array.AsSpan(index, count);
    }

    /// <summary>Returns a span from a validated index to the end of an array.</summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="index">The starting index.</param>
    /// <returns>A span over the array tail.</returns>
    internal static Span<T> Tail<T>(T[] array, int index)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        Check(array.Length, index, 0);
        return array.AsSpan(index);
    }
}
