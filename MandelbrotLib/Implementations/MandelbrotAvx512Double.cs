using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MandelbrotLib.Implementations;

[Mandelbrot(Name = "AVX-512 Double", Priority = 1, Avx512FRequired = true)]
public unsafe class MandelbrotAvx512Double() : MandelbrotBase(widthAlignment: 8, heightAlignment: 2)
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

        Vector512<long> vConst1Int64 = Vector512.Create<long>(1);

        Vector512<double> vConst4 = Vector512.Create(4.0);
        Vector512<double> vConst0To7 = Vector512.Create(0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0);

        Vector512<double> dx = Vector512.Create(GetDX(rectangle));
        Vector512<double> dy = Vector512.Create(GetDY(rectangle));

        Vector512<double> dxTimes8 = dx * 8.0;
        Vector512<double> dyTimesDJ = dy * Vector512.Create((double)dj);

        Vector512<double> x0 = Avx512F.FusedMultiplyAdd(dx, vConst0To7, Vector512.Create(rectangle.X0));
        Vector512<double> y = Avx512F.FusedMultiplyAdd(dy, Vector512.Create((double)firstRow), Vector512.Create(rectangle.Y0));
        Vector512<double> yPlusDY = y + dy;

        for (nint j = numRows - firstRow; j > 0; j -= dj)
        {
            Vector512<double> x = x0;

            for (nint i = rowSizeDiv8; i > 0; i--)
            {
                // c = x + yi
                // Zn = a + bi

                // Zn+1 = Zn^2 + c = (a + bi)^2 + x + yi = a^2 - b^2 + x + (2ab + y)i 

                nint n = maxIterationsDiv2;

                Vector512<long> iterations1 = Vector512<long>.Zero;
                Vector512<long> iterations2 = Vector512<long>.Zero;

                var a1 = x;
                var b1 = y;

                var a2 = x;
                var b2 = yPlusDY;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                Vector512<double> Iterate(ref Vector512<long> iterations, ref Vector512<double> a, ref Vector512<double> b)
                {
                    Vector512<double> aTimes2 = Avx512F.Add(a, a);
                    Vector512<double> minusBBPlusX = Avx512F.FusedMultiplyAddNegated(b, b, x);
                    Vector512<double> compareValue = Avx512F.FusedMultiplyAdd(b, b, Avx512F.Multiply(a, a));
                    b = Avx512F.FusedMultiplyAdd(aTimes2, b, y);
                    a = Avx512F.FusedMultiplyAdd(a, a, minusBBPlusX);
                    Vector512<double> compareResult = Avx512F.CompareLessThan(compareValue, vConst4);
                    iterations = Vector512.ConditionalSelect(compareResult.AsInt64(), iterations + vConst1Int64, iterations);

                    return compareValue;
                }

                do
                {
                    // It's important to do two independent iterations in parallel to get the best performance

                    // 1. Iteration...

                    Iterate(ref iterations1, ref a1, ref b1);
                    Iterate(ref iterations2, ref a2, ref b2);

                    // 2. Iteration...

                    Vector512<double> compareValue1 = Iterate(ref iterations1, ref a1, ref b1);
                    Vector512<double> compareValue2 = Iterate(ref iterations2, ref a2, ref b2);

                    // Calling Avx512F.CompareLessThan() again helps the JIT to generate better code

                    var compareResult1 = Avx512F.CompareLessThan(compareValue1, vConst4);
                    var compareResult2 = Avx512F.CompareLessThan(compareValue2, vConst4);

                    if ((compareResult1 | compareResult2) == Vector512<double>.Zero)
                    {
                        break; // ### BREAK ###
                    }
                }
                while (--n > 0);

                Avx512F.ConvertToVector256Int32(iterations1).Store(iterationsPtr);
                Avx512F.ConvertToVector256Int32(iterations2).Store(iterationsPtr + rowSize);

                iterationsPtr += 8;

                x += dxTimes8;
            }

            iterationsPtr += rowSizeTimesDJMinus1;

            y = Avx512F.Add(y, dyTimesDJ);
            yPlusDY = Avx512F.Add(yPlusDY, dyTimesDJ);

            if (IsCalculationCancellationRequested)
            {
                return; // ### RETURN ###
            }
        }
    }
}
