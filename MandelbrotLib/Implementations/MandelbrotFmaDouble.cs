using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MandelbrotLib.Implementations;

[Mandelbrot(Name = "AVX2 Double", Priority = 2)]
public unsafe class MandelbrotFmaDouble() : MandelbrotBase(widthAlignment: 4, heightAlignment: 2)
{
    // Calculates two consecutive rows at once
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected override void Calculate(MandelbrotRegion rectangle, int maxIterations, int firstRowDiv2, int numRows, int rowDeltaDiv2)
    {
        nint firstRow = 2 * firstRowDiv2;
        nint dj = 2 * rowDeltaDiv2;

        nint rowSize = RowSize;
        nint rowSizeDiv4 = rowSize / 4;
        nint rowSizeTimesDJMinus1 = rowSize * (dj - 1);

        int* iterationsPtr = IterationsPtr + rowSize * firstRow;

        nint maxIterationsDiv2 = (maxIterations + 1) / 2;

        if (rowSizeDiv4 <= 0)
        {
            return; // ### RETURN ###
        }

        Vector256<double> vConst4 = Vector256.Create(4.0);
        Vector256<double> vConst0To3 = Vector256.Create(0.0, 1.0, 2.0, 3.0);

        Vector256<double> dx = Vector256.Create(GetDX(rectangle));
        Vector256<double> dy = Vector256.Create(GetDY(rectangle));

        Vector256<double> dxTimes4 = dx * 4.0;
        Vector256<double> dyTimesDJ = dy * Vector256.Create((double)dj);

        Vector256<double> x0 = Fma.MultiplyAdd(dx, vConst0To3, Vector256.Create(rectangle.X0));
        Vector256<double> y = Fma.MultiplyAdd(dy, Vector256.Create((double)firstRow), Vector256.Create(rectangle.Y0));
        Vector256<double> yPlusDY = y + dy;

        Vector256<int> vIterationsPermuteMask = Vector256.Create(0, 2, 4, 6, 0, 0, 0, 0);

        for (nint j = numRows - firstRow; j > 0; j -= dj)
        {
            Vector256<double> x = x0;

            for (nint i = rowSizeDiv4; i > 0; --i)
            {
                // c = x + yi
                // Zn = a + bi

                // Zn+1 = Zn^2 + c = (a + bi)^2 + x + yi = a^2 - b^2 + x + (2ab + y)i 

                nint n = maxIterationsDiv2;

                Vector256<long> iterations1 = Vector256<long>.Zero;
                Vector256<long> iterations2 = Vector256<long>.Zero;

                Vector256<double> a1 = x;
                Vector256<double> b1 = y;

                Vector256<double> a2 = x;
                Vector256<double> b2 = yPlusDY;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                Vector256<double> Iterate(ref Vector256<long> iterations, ref Vector256<double> a, ref Vector256<double> b)
                {
                    Vector256<double> a1Times2 = Avx.Add(a, a);
                    var minusBB1PlusX1 = Fma.MultiplyAddNegated(b, b, x);
                    Vector256<double> compareValue = Fma.MultiplyAdd(b, b, Avx.Multiply(a, a));
                    b = Fma.MultiplyAdd(a1Times2, b, y);
                    a = Fma.MultiplyAdd(a, a, minusBB1PlusX1);
                    Vector256<double> compareResult = Avx.Compare(compareValue, vConst4, FloatComparisonMode.OrderedLessThanNonSignaling);
                    iterations = Avx2.Subtract(iterations, compareResult.AsInt64());
                    return compareResult;
                }

                do
                {
                    // 1. Iteration...

                    Iterate(ref iterations1, ref a1, ref b1);
                    Iterate(ref iterations2, ref a2, ref b2);

                    // 2. Iteration...

                    Vector256<double> compareResult1 = Iterate(ref iterations1, ref a1, ref b1);
                    Vector256<double> compareResult2 = Iterate(ref iterations2, ref a2, ref b2);

                    if ((Avx.MoveMask(Avx.Or(compareResult1, compareResult2)) & 0x0F) == 0)
                    {
                        break; // ### BREAK ###
                    }
                }
                while (--n > 0);

                Vector128<int> iterations1Int32 = Avx2.PermuteVar8x32(iterations1.AsInt32(), vIterationsPermuteMask).GetLower();
                Vector128<int> iterations2Int32 = Avx2.PermuteVar8x32(iterations2.AsInt32(), vIterationsPermuteMask).GetLower();

                Sse2.Store(iterationsPtr, iterations1Int32);
                Sse2.Store(iterationsPtr + rowSize, iterations2Int32);

                iterationsPtr += 4;

                x = Avx.Add(x, dxTimes4);
            }

            iterationsPtr += rowSizeTimesDJMinus1;

            y = Avx.Add(y, dyTimesDJ);
            yPlusDY = Avx.Add(yPlusDY, dyTimesDJ);

            if (IsCalculationCancellationRequested)
            {
                return; // ### RETURN ###
            }
        }
    }
}
