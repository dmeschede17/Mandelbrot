using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MandelbrotLib.Utils;

public unsafe struct TwoDimensionalUnmanagedArray<T> : ITwoDimensionalArray<T> where T : unmanaged
{
    int ArrayCapacity { get; set; }
    int ArrayLength { get; set; }

    public T* ArrayPtr { get; private set; }
    public T* ArrayEndPtr { get; private set; }

    public int WidthAlignment { get; }
    public int HeightAlignment { get; }

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int RowSize { get; private set; }
    public int NumRows { get; private set; }

    public TwoDimensionalUnmanagedArray(int widthAlignment = 1, int heightAlignment = 1)
    {
        TwoDimensionalArray.ThrowIfInvalidAlignment(widthAlignment, nameof(widthAlignment));
        TwoDimensionalArray.ThrowIfInvalidAlignment(heightAlignment, nameof(heightAlignment));

        WidthAlignment = widthAlignment;
        HeightAlignment = heightAlignment;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan() => new(ArrayPtr, ArrayLength);

    public bool SetSize(int width, int height)
    {
        if (width == Width || height == Height)
        {
            return false;
        }

        Width = width;
        Height = height;

        RowSize = TwoDimensionalArray.AlignValue(width, WidthAlignment);
        NumRows = TwoDimensionalArray.AlignValue(height, HeightAlignment);

        int arrayLength = NumRows * RowSize;

        if (arrayLength > ArrayCapacity)
        {
            if (ArrayPtr != null)
            {
                Marshal.FreeHGlobal((nint)ArrayPtr);
            }

            ArrayPtr = (T*)Marshal.AllocHGlobal(arrayLength * sizeof(T));
            ArrayEndPtr = ArrayPtr + arrayLength;

            ArrayCapacity = arrayLength;
            ArrayLength = arrayLength;

            return true;
        }
        else
        {
            ArrayEndPtr = ArrayPtr + arrayLength;
            ArrayLength = arrayLength;

            return false;
        }
    }

    public void FreeIfAllocated()
    {
        if (ArrayPtr != null)
        {
            Marshal.FreeHGlobal((nint)ArrayPtr);

            ArrayPtr = null;
            ArrayEndPtr = null;

            ArrayCapacity = 0;
            ArrayLength = 0;
        }
    }
}
