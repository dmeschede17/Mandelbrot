using MandelbrotLib.Utils;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MandelbrotLib.Coloring;

public static class Colorizer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Vector128<int> Mod(Vector128<int> vLeft, Vector128<int> vRight)
    {
        Vector128<float> vLeftAsFloat = Sse2.ConvertToVector128Single(vLeft);
        Vector128<float> vRightAsFloat = Sse2.ConvertToVector128Single(vRight);
        Vector128<float> vQuotientAsFloat = Sse.Divide(vLeftAsFloat, vRightAsFloat);
        Vector128<int> vQuotient = Sse2.ConvertToVector128Int32WithTruncation(vQuotientAsFloat);
        Vector128<int> vProduct = Sse41.MultiplyLow(vQuotient, vRight);
        Vector128<int> vRemainder = Sse2.Subtract(vLeft, vProduct);

        return vRemainder;
    }

    [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "It's volatile")]
    static volatile int VolatileConst0 = 0;

    /// <summary>
    /// Applies a color palette to an array of iteration counts to generate a colored image.
    /// </summary>
    /// <param name="iterationsArray">A two-dimensional array containing the iteration counts for each pixel.</param>
    /// <param name="maxIterations">The maximum number of iterations used in the Mandelbrot set calculation.</param>
    /// <param name="colorPalette">A read-only span of colors used to colorize the pixels based on their iteration counts.</param>
    /// <param name="pixelsBgr32Array">A reference to a two-dimensional array that will be filled with the resulting colored pixels in BGR32 format.</param>
    /// <remarks>
    /// This method uses SIMD (Single Instruction, Multiple Data) instructions to optimize the colorization process.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    unsafe public static void Colorize(in TwoDimensionalGrowingArray<int> iterationsArray, int maxIterations, ReadOnlySpan<UInt32> colorPalette, double offset, ref TwoDimensionalGrowingArray<UInt32> pixelsBgr32Array)
    {
        Debug.Assert(Vector128<int>.Count == Vector128<uint>.Count);

        pixelsBgr32Array.SetSize(iterationsArray);

        if (iterationsArray.WidthAlignment % Vector128<int>.Count != 0)
        {
            throw new ArgumentException($"WidthAlignment must be a multiple of {Vector128<int>.Count}", nameof(iterationsArray));
        }

        if (colorPalette.Length < 1)
        {
            throw new ArgumentException("Color palette must have at least one element", nameof(colorPalette));
        }

        if (pixelsBgr32Array.WidthAlignment % Vector128<uint>.Count != 0)
        {
            throw new ArgumentException("WidthAlignment must be a multiple of ${Vector128<UInt32>.Count}", nameof(pixelsBgr32Array));
        }

        nint widthDivNumVectorElements = (iterationsArray.Width + Vector128<int>.Count - 1) / Vector128<int>.Count;

        nint iterationsRowPadding = iterationsArray.RowSize - Vector128<int>.Count * widthDivNumVectorElements;
        nint pixelsRowPadding = pixelsBgr32Array.RowSize - Vector128<int>.Count * widthDivNumVectorElements;

        Debug.Assert(iterationsRowPadding >= 0);
        Debug.Assert(pixelsRowPadding >= 0);

        Vector128<int> vZero = Vector128.Create(VolatileConst0);
        Vector128<int> vMaxIterations = Vector128.Create(maxIterations);

        Vector128<int> vOffset = Vector128.Create((int)(colorPalette.Length * offset));

        Vector128<int> vColorPaletteLength = Vector128.Create(colorPalette.Length);

        fixed (int* pIterationsArray = iterationsArray.GetArray())
        fixed (uint* pColorPalette = colorPalette)
        fixed (uint* pPixelsBgr32Array = pixelsBgr32Array.GetArray())
        {
            int* pIterations = pIterationsArray;
            uint* pPixelsBgr32 = pPixelsBgr32Array;

            for (nint j = iterationsArray.Height; j > 0; j--)
            {
                for (nint i = widthDivNumVectorElements; i > 0; i--)
                {
                    Vector128<int> vIterations = Sse41.Max(vZero, Sse2.LoadVector128(pIterations));

                    Vector128<uint> vIsNotMaxIterations = ~Sse2.CompareEqual(vIterations, vMaxIterations).AsUInt32();

                    Vector128<int> vColorIndex = Mod(vIterations + vOffset, vColorPaletteLength);

                    Vector128<uint> vPixelBgr32 = Avx2.GatherMaskVector128(vZero.AsUInt32(), pColorPalette, vColorIndex, vIsNotMaxIterations, sizeof(uint));

                    vPixelBgr32.Store(pPixelsBgr32);

                    pIterations += Vector128<int>.Count;
                    pPixelsBgr32 += Vector128<uint>.Count;
                }

                pIterations += iterationsRowPadding;
                pPixelsBgr32 += pixelsRowPadding;
            }

            Debug.Assert(pIterations == pIterationsArray + iterationsArray.Height * iterationsArray.RowSize);
            Debug.Assert(pPixelsBgr32 == pPixelsBgr32Array + pixelsBgr32Array.Height * pixelsBgr32Array.RowSize);
        }
    }
}
