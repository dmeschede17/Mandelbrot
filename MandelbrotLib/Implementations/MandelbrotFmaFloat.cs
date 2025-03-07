using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MandelbrotLib.Implementations;

[Mandelbrot(Name = "AVX2 Float", Priority = 5)]
public unsafe class MandelbrotFmaFloat() : MandelbrotBase(widthAlignment: 8, heightAlignment: 2)
{
    // Calculates two consecutive rows at once
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected override void Calculate(MandelbrotRegion rectangle, int maxIterations, int firstRowDiv2, int numRows, int rowDeltaDiv2)
    {
        nint firstRow = 2 * firstRowDiv2;
        nint dj = 2 * rowDeltaDiv2;

        nint rowSize = RowSize;
        nint rowSizeDiv8 = rowSize / 8;
        nint rowSizeTimesDJMinus1 = rowSize * (dj - 1);

        int* iterationsPtr = IterationsPtr + rowSize * firstRow;

        nint maxIterationsDiv2 = (maxIterations + 1) / 2;

        if (rowSizeDiv8 <= 0)
        {
            return; // ### RETURN ###
        }

        Vector256<float> vConst4 = Vector256.Create(4.0f);
        Vector256<float> vConst0To7 = Vector256.Create(0.0f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f);

        Vector256<float> dx = Vector256.Create((float)GetDX(rectangle));
        Vector256<float> dy = Vector256.Create((float)GetDY(rectangle));

        Vector256<float> dxTimes8 = dx * 8.0f;
        Vector256<float> dyTimesDJ = dy * Vector256.Create((float)dj);

        Vector256<float> x0 = Fma.MultiplyAdd(dx, vConst0To7, Vector256.Create((float)rectangle.X0));
        Vector256<float> y = Fma.MultiplyAdd(dy, Vector256.Create((float)firstRow), Vector256.Create((float)rectangle.Y0));
        Vector256<float> yPlusDY = y + dy;

        Vector256<int> vIterationsPermuteMask = Vector256.Create(0, 2, 4, 6, 0, 0, 0, 0);

        for (nint j = numRows - firstRow; j > 0; j -= dj)
        {
            Vector256<float> x = x0;

            for (nint i = rowSizeDiv8; i > 0; --i)
            {
                // c = x + yi
                // Zn = a + bi

                // Zn+1 = Zn^2 + c = (a + bi)^2 + x + yi = a^2 - b^2 + x + (2ab + y)i 

                nint n = maxIterationsDiv2;

                Vector256<int> iterations1 = Vector256<int>.Zero;
                Vector256<int> iterations2 = Vector256<int>.Zero;

                Vector256<float> a1 = x;
                Vector256<float> b1 = y;

                Vector256<float> a2 = x;
                Vector256<float> b2 = yPlusDY;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                Vector256<float> Iterate(ref Vector256<int> iterations, ref Vector256<float> a, ref Vector256<float> b)
                {
                    Vector256<float> a1Times2 = Avx.Add(a, a);
                    var minusBB1PlusX1 = Fma.MultiplyAddNegated(b, b, x);
                    Vector256<float> compareValue = Fma.MultiplyAdd(b, b, Avx.Multiply(a, a));
                    b = Fma.MultiplyAdd(a1Times2, b, y);
                    a = Fma.MultiplyAdd(a, a, minusBB1PlusX1);
                    Vector256<float> compareResult = Avx.Compare(compareValue, vConst4, FloatComparisonMode.OrderedLessThanNonSignaling);
                    iterations = Avx2.Subtract(iterations, compareResult.AsInt32());
                    return compareResult;
                }

                do
                {
                    // 1. Iteration...

                    Iterate(ref iterations1, ref a1, ref b1);
                    Iterate(ref iterations2, ref a2, ref b2);

                    // 2. Iteration...

                    Vector256<float> compareResult1 = Iterate(ref iterations1, ref a1, ref b1);
                    Vector256<float> compareResult2 = Iterate(ref iterations2, ref a2, ref b2);

                    if ((Avx.MoveMask(Avx.Or(compareResult1, compareResult2)) & 0xFF) == 0)
                    {
                        break; // ### BREAK ###
                    }
                }
                while (--n > 0);

                Vector128<int> iterations1Int32 = Avx2.PermuteVar8x32(iterations1.AsInt32(), vIterationsPermuteMask).GetLower();
                Vector128<int> iterations2Int32 = Avx2.PermuteVar8x32(iterations2.AsInt32(), vIterationsPermuteMask).GetLower();

                Avx.Store(iterationsPtr, iterations1);
                Avx.Store(iterationsPtr + rowSize, iterations2);

                iterationsPtr += 8;

                x = Avx.Add(x, dxTimes8);
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
