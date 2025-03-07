using DiLib.Threading;
using MandelbrotLib.Utils;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MandelbrotLib.Implementations;

unsafe public abstract class MandelbrotBase(int widthAlignment, int heightAlignment) : IDisposable
{
    TwoDimensionalUnmanagedArray<int> iterationsArray = new(widthAlignment, heightAlignment);

    protected int* IterationsPtr => iterationsArray.ArrayPtr;
    protected int* IterationsEndPtr => iterationsArray.ArrayEndPtr;

    public ref readonly TwoDimensionalUnmanagedArray<int> IterationsArray => ref iterationsArray;

    /// <summary> The MaxIterations value used for calculating the iterations </summary>
    int iterationsMaxIterations;

    public int Width => iterationsArray.Width;
    public int Height => iterationsArray.Height;

    public int RowSize => iterationsArray.RowSize;
    public int NumRows => iterationsArray.NumRows;

    public void SetSize(int width, int height) => iterationsArray.SetSize(width, height);

    protected virtual void Dispose(bool disposing)
    {
        iterationsArray.FreeIfAllocated();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~MandelbrotBase() => Dispose(false);

    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "It's necessary")]
    [SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Required to get address for assmbler code")]
    protected volatile int calculationCancellationRequested;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RequestCalculationCancellation() { calculationCancellationRequested = 1; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetCalculationCancellation() { calculationCancellationRequested = 0; }

    public bool IsCalculationCancellationRequested => calculationCancellationRequested != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetDX(in MandelbrotRegion rectangle) => (rectangle.X1 - rectangle.X0) / Width;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetDY(in MandelbrotRegion rectangle) => (rectangle.Y1 - rectangle.Y0) / Height;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public (long totalNumberOfIterations, int minIterations, int maxIterations) GetIterationsInfo()
    {
        long totalNumberOfIterations = 0;

        Vector128<int> vMinIterations = Vector128.Create(int.MaxValue);
        Vector128<int> vMaxIterations = Vector128<int>.Zero;

        int* iterationsPtr = IterationsPtr;

        nint width = Width;
        nint rowSizeMinusWidth = RowSize - width;

        for (nint j = Height; j > 0; j--)
        {
            for (nint i = width; i > 0; i--)
            {
                int iterations = *iterationsPtr;
                totalNumberOfIterations += iterations;
                Vector128<int> vIterations = Vector128.Create(iterations);
                vMinIterations = Sse41.Min(vMinIterations, vIterations);
                vMaxIterations = Sse41.Max(vMaxIterations, vIterations);
                iterationsPtr++;
            }

            iterationsPtr += rowSizeMinusWidth;
        }

        Debug.Assert(iterationsPtr <= IterationsEndPtr);

        int minIterations = vMinIterations.GetElement(0);
        int maxIterations = vMaxIterations.GetElement(0);

        return (totalNumberOfIterations, minIterations, maxIterations);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Calculate(MandelbrotRegion rectangle, int maxIterations)
    {
        iterationsMaxIterations = maxIterations;

        Calculate(rectangle, maxIterations, 0, Height, 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Calculate(MandelbrotRegion rectangle, int maxIterations, ThreadCluster threadCluster)
    {
        ArgumentNullException.ThrowIfNull(threadCluster, nameof(threadCluster));

        iterationsMaxIterations = maxIterations;

        var numRows = Height;
        var numThreads = threadCluster.NumThreads;

        for (int i = 0; i < numThreads; i++)
        {
            var firstRow = i;
            threadCluster.ThreadAction(i) = () => Calculate(rectangle, maxIterations, firstRow, numRows, numThreads);
        }

        threadCluster.Run();
    }

    public void CalculateFirstRow(MandelbrotRegion rectangle, int maxIterations) => Calculate(rectangle, maxIterations, 0, 1, 1);

    /// <summary>
    /// Calculate every "rowDelta"th row in range [firstRow, numRows[.
    /// Note: Most of the derived classes calculate two consecutive rows at once. In this case, firstRowDiv2 and rowDeltaDiv2 are multiplied by 2 inside the method.
    /// </summary>
    protected abstract void Calculate(MandelbrotRegion rectangle, int maxIterations, int firstRowDiv2, int numRows, int rowDeltaDiv2);

    static int AlignValue(int value, int alignment) => (value + alignment - 1) / alignment * alignment;
}
