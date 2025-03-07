using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MandelbrotLib.Utils;

public struct TwoDimensionalGrowingArray<T> : ITwoDimensionalArray<T> where T : unmanaged
{
    public int WidthAlignment { get; }
    public int HeightAlignment { get; }

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int RowSize { get; private set; }
    public int NumRows { get; private set; }

    public readonly Span<T> AsSpan() => array.AsSpan();

    T[] array = [];

    public TwoDimensionalGrowingArray(int widthAlignment = 1, int heightAlignment = 1)
    {
        TwoDimensionalArray.ThrowIfInvalidAlignment(widthAlignment, nameof(widthAlignment));
        TwoDimensionalArray.ThrowIfInvalidAlignment(heightAlignment, nameof(heightAlignment));

        WidthAlignment = widthAlignment;
        HeightAlignment = heightAlignment;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T[] GetArray() => array;

    public bool SetSize(int width, int height, int preferedRowSize = -1)
    {
        int rowSize = TwoDimensionalArray.AlignValue(width, WidthAlignment, preferedRowSize);

        if (Width == width && Height == height && RowSize == rowSize)
        {
            return false;
        }

        Width = width;
        Height = height;
        RowSize = rowSize;
        NumRows = TwoDimensionalArray.AlignValue(height, HeightAlignment);

        int arrayLength = NumRows * RowSize;

        if (arrayLength > array.Length)
        {
            array = new T[arrayLength];

            return true;
        }
        else
        {
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SetSize(ITwoDimensionalArray other)
    {
        ArgumentNullException.ThrowIfNull(other, nameof(other));
        return SetSize(other.Width, other.Height, other.RowSize);
    }

    public void CopyFrom(TwoDimensionalUnmanagedArray<T> other)
    {
        SetSize(other);

        Debug.Assert(Width == other.Width);
        Debug.Assert(Height == other.Height);

        if (RowSize == other.RowSize)
        {
            other.AsSpan().CopyTo(array);
        }
        else
        {
            int rowSize = RowSize;
            int otherRowSize = other.RowSize;
            int minRowSize = Math.Min(rowSize, otherRowSize);

            int offset = 0;
            int otherOffset = 0;

            Span<T> otherSpan = other.AsSpan();

            for (nint j = Height; j > 0; j--)
            {
                otherSpan.Slice(otherOffset, minRowSize).CopyTo(array.AsSpan(offset));

                offset += rowSize;
                otherOffset += otherRowSize;
            }
        }
    }
}
