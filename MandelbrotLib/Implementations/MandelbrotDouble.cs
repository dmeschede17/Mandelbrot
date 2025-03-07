using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MandelbrotLib.Implementations;

[Mandelbrot(Name = "Double", Priority = 3)]
public class MandelbrotDouble() : MandelbrotBase(widthAlignment: 1, heightAlignment: 1)
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "It's necessary ;-)")]
    [SuppressMessage("Naming", "CA1725:Parameter names should match base declaration", Justification = "This method calculates only one consecutive row at once")]
    unsafe protected override void Calculate(MandelbrotRegion rectangle, int maxIterations, int firstRow, int numRows, int rowDelta)
    {
        Debug.Assert(Width == RowSize);

        nint width = Width;

        if (width <= 0)
        {
            return; // ### RETURN ###
        }

        (double x0, double y0) = (rectangle.X0, rectangle.Y0);
        (double dx, double dy) = (GetDX(rectangle), GetDY(rectangle));

        nint maxIterationsAsNInt = maxIterations;
        nint dj = rowDelta;

        double dyTimesDJ = dy * rowDelta;
        nint iterationsRowIncrement = width * (rowDelta - 1);

        double limit2 = 4.0;

        int* pIterations = IterationsPtr + width * firstRow;

        double y = y0 + firstRow * dy;

        for (nint j = numRows - firstRow; j > 0; j -= dj)
        {
            double x = x0;

            for (nint i = width; i > 0; --i)
            {
                // c = x + yi
                // Zn = a + bi

                // Zn+1 = Zn^2 + c = (a + bi)^2 + x + yi = a^2 - b^2 + x + (2ab + y)i 

                double a = x;
                double b = y;

                nint n = 0;

                do
                {
                    var aa = a * a;
                    var bb = b * b;

                    if (aa + bb > limit2)
                    {
                        break; // ### BREAK ###
                    }

                    b = a * b + a * b + y;
                    a = aa - bb + x;
                }
                while (++n < maxIterationsAsNInt);

                *pIterations++ = (int)n;

                x += dx;
            }

            pIterations += iterationsRowIncrement;

            y += dyTimesDJ;

            if (IsCalculationCancellationRequested)
            {
                return; // ### RETURN ###
            }
        }
    }
}
