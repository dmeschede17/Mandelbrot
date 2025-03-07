using MandelbrotLib.Utils;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MandelbrotLib;

public readonly record struct MandelbrotIterationsInfo
{
    public long TotalNumberOfIterations { get; init; }
    public int MinIterations { get; init; }
    public int MaxIterations { get; init; }

    unsafe public static MandelbrotIterationsInfo Calculate(in TwoDimensionalUnmanagedArray<int> iterationsArray)
    {
        int width = iterationsArray.Width;

        if (width <= 0)
        {
            return new MandelbrotIterationsInfo();
        }
        
        int* pIterations = iterationsArray.ArrayPtr;

        nint widthDiv8 = Math.DivRem(width, 8, out int widthMod8);

        if (widthDiv8 > 0 && widthMod8 == 0)
        {
            widthDiv8--;
            widthMod8 = 8;
        }

        Vector256<long> vTotalNumberOfIterations1 = Vector256<long>.Zero;
        Vector256<long> vTotalNumberOfIterations2 = Vector256<long>.Zero;

        Vector256<int> vMinValue = Vector256.Create(int.MinValue);
        Vector256<int> vMaxValue = Vector256.Create(int.MaxValue);

        Vector256<int> vMinIterations = vMaxValue;
        Vector256<int> vMaxIterations = vMinValue;

        Vector256<int> vConst0To7 = Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7);
        Vector256<int> vMask = Avx2.CompareGreaterThan(Vector256.Create(widthMod8), vConst0To7);

        nint rowPadding = iterationsArray.RowSize - 8 * widthDiv8;

        for (nint j = iterationsArray.Height; j > 0; j--)
        {
            for (nint i = widthDiv8; i > 0; i--)
            {
                Vector256<int> vIterations = Avx2.LoadVector256(pIterations);

                vTotalNumberOfIterations1 += Avx2.ConvertToVector256Int64(vIterations.GetLower());
                vTotalNumberOfIterations2 += Avx2.ConvertToVector256Int64(vIterations.GetUpper());

                vMinIterations = Avx2.Min(vMinIterations, vIterations);
                vMaxIterations = Avx2.Max(vMaxIterations, vIterations);

                pIterations += 8;
            }

            Vector256<int> vIterationsEx = Avx2.MaskLoad(pIterations, vMask);

            vTotalNumberOfIterations1 += Avx2.ConvertToVector256Int64(vIterationsEx.GetLower());
            vTotalNumberOfIterations2 += Avx2.ConvertToVector256Int64(vIterationsEx.GetUpper());

            vMinIterations = Avx2.Min(vMinIterations, Vector256.ConditionalSelect(vMask, vIterationsEx, vMaxValue));
            vMaxIterations = Avx2.Max(vMaxIterations, Vector256.ConditionalSelect(vMask, vIterationsEx, vMinValue));

            pIterations += rowPadding;
        }

        Debug.Assert(pIterations == iterationsArray.ArrayPtr + iterationsArray.Height * iterationsArray.RowSize);
        Debug.Assert(pIterations <= iterationsArray.ArrayEndPtr);

        Vector256<long> vTotalNumberOfIterations = vTotalNumberOfIterations1 + vTotalNumberOfIterations2;
        Vector128<long> vTotalNumberOfIterations128 = vTotalNumberOfIterations.GetLower() + vTotalNumberOfIterations.GetUpper();
        long totalNumberOfIterations = vTotalNumberOfIterations128.GetElement(0) + vTotalNumberOfIterations128.GetElement(1);

        Vector128<int> vMinIterations128 = Avx2.Min(vMinIterations.GetLower(), vMinIterations.GetUpper());
        int minIterations = Avx2.Min(vMinIterations128, Avx2.Shuffle(vMinIterations128, 0b01)).GetElement(0);

        Vector128<int> vMaxIterations128 = Avx2.Max(vMaxIterations.GetLower(), vMaxIterations.GetUpper());
        int maxIterations = Avx2.Max(vMaxIterations128, Avx2.Shuffle(vMaxIterations128, 0b01)).GetElement(0);

        return new MandelbrotIterationsInfo 
        {
            TotalNumberOfIterations = totalNumberOfIterations,
            MinIterations = minIterations,
            MaxIterations = maxIterations
        };
    }
}
