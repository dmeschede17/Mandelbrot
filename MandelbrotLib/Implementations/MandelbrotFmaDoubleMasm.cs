using System.Runtime.CompilerServices;

namespace MandelbrotLib.Implementations;

#if MANDELBROT_MASM

[Mandelbrot(Name = "AVX2 Double (MASM)", Priority = 2)]
public unsafe partial class MandelbrotFmaDoubleMasm() : MandelbrotBase(widthAlignment: 8, heightAlignment: 2)
{
    // Calculates two consecutive rows at once
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected override void Calculate(MandelbrotRegion rectangle, int maxIterations, int firstRowDiv2, int numRows, int rowDeltaDiv2)
    {
        double deltaX = GetDX(rectangle);
        double deltaY = GetDY(rectangle);

        MandelbrotMasm.FmaDoubleCalculate
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