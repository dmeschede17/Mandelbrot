using System.Runtime.CompilerServices;

namespace MandelbrotLib.Utils;

public interface ITwoDimensionalArray
{
    int Width { get; }
    int Height { get; }

    int RowSize { get; }
}

public interface ITwoDimensionalArray<T> : ITwoDimensionalArray where T : unmanaged
{
    Span<T> AsSpan();
}

internal static class TwoDimensionalArray
{
    internal static void ThrowIfInvalidAlignment(int alignment, string paramName)
    {
        if (alignment < 1)
        {
            throw new ArgumentException($"Alignment must be at least 1!", nameof(paramName));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int AlignValue(int value, int alignment) => (value + alignment - 1) / alignment * alignment;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static int AlignValue(int value, int alignment, int preferedAlignedValue)
    {
        int alignedValue = AlignValue(value, alignment);

        return preferedAlignedValue >= alignedValue && preferedAlignedValue % alignment == 0 ? preferedAlignedValue : alignedValue;
    }
}
