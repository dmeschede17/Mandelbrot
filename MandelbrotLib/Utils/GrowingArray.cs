namespace MandelbrotLib.Utils;

public struct GrowingArray<T> where T : unmanaged
{
    T[] array;

    public int Length { get; set { if (value > array.Length) array = new T[value]; field = value; } }

    public GrowingArray()
    {
        array = [];
    }

    public readonly ref T this[int index] => ref array[index];

    public readonly Span<T> AsSpan() => array.AsSpan(0, Length);
    public readonly ReadOnlySpan<T> AsReadOnlySpan() => array.AsSpan(0, Length);
}
