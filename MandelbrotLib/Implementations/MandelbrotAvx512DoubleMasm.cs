using System.Runtime.CompilerServices;

namespace MandelbrotLib.Implementations;

#if MANDELBROT_MASM

[Mandelbrot(Name = "AVX-512 Double (MASM)", Priority = 1, Avx512FRequired = true)]
public unsafe partial class MandelbrotAvx512DoubleMasm() : MandelbrotBase(widthAlignment: 8, heightAlignment: 2)
{
    // Calculates two consecutive rows at once
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected override void Calculate(MandelbrotRegion rectangle, int maxIterations, int firstRowDiv2, int numRows, int rowDeltaDiv2)
    {
        double deltaX = GetDX(rectangle);
        double deltaY = GetDY(rectangle);

        MandelbrotMasm.Avx512DoubleCalculate
        (
            rectangle.X0,
            rectangle.Y0,
            deltaX,
            deltaY,
            IterationsPtr,
            RowSize,
            maxIterations,
            firstRowDiv2,
            numRows,
            rowDeltaDiv2
        );
    }
}

#endif
