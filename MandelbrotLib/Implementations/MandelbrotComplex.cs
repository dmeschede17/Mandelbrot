using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace MandelbrotLib.Implementations;

[Mandelbrot(Name = "Complex", Priority = 4)]
public class MandelbrotComplex() : MandelbrotBase(widthAlignment: 1, heightAlignment: 1)
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "It's necessary ;-)")]
    [SuppressMessage("Naming", "CA1725:Parameter names should match base declaration", Justification = "This method calculates only one consecutive row at once")]
    protected override void Calculate(MandelbrotRegion rectangle, int maxIterations, int firstRow, int numRows, int rowDelta)
    {
        Span<int> iterations = IterationsArray.AsSpan();
        (double dx, double dy) = (GetDX(rectangle), GetDY(rectangle));

        for (int j = firstRow; j < numRows; j += rowDelta)
        {
            for (int i = 0; i < Width; i++)
            {
                Complex c = new(rectangle.X0 + i * dx, rectangle.Y0 + j * dy);
                Complex z = c;

                int n = 0;

                for (; n < maxIterations; n++)
                {
                    if (Complex.Abs(z) > 2.0)
                    {
                        break; // ### BREAK ###
                    }
                    z = z * z + c;
                }

                iterations[i + RowSize * j] = n;
            }

            if (IsCalculationCancellationRequested)
            {
                return; // ### RETURN ###
            }
        }
    }
}
