using DiLib.Threading;
using MandelbrotLib.Implementations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MandelbrotLib;

public sealed class MandelbrotGenerator : IDisposable
{
    const ThreadPriority DefaultThreadPriority = ThreadPriority.Normal;
    const ThreadPriority BenchmarkThreadPriority = ThreadPriority.AboveNormal;

    Task? task;
    ThreadCluster? threadCluster;
    MandelbrotBase mandelbrot = new MandelbrotNull();

    public MandelbrotBase Mandelbrot => mandelbrot;
    public MandelbrotResult Result => result;

    MandelbrotResult result = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void ThrowIfNotPositive(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentException($"Value must be > 0!", paramName);
        }
    }

    public void Dispose()
    {
        threadCluster?.Dispose();
        mandelbrot?.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    void PrepareCalculation(Type mandelbrotType, int width, int height, int numTasks, in MandelbrotRegion region, int maxIterations, ThreadPriority? threadPriority = null)
    {
        ArgumentNullException.ThrowIfNull(mandelbrotType, nameof(mandelbrotType));
        ThrowIfNotPositive(width, nameof(width));
        ThrowIfNotPositive(height, nameof(height));
        ThrowIfNotPositive(numTasks, nameof(numTasks));

        if (task != null)
        {
            if (!task.IsCompleted)
            {
                mandelbrot?.RequestCalculationCancellation();
                task.Wait();
                mandelbrot?.ResetCalculationCancellation();
            }

            task.Dispose();
            task = null;
        }

        if (mandelbrot?.GetType() != mandelbrotType)
        {
            mandelbrot?.Dispose();
            mandelbrot = MandelbrotFactory.CreateMandelbrot(mandelbrotType);
        }

        mandelbrot.SetSize(width, height);

        Debug.Assert(mandelbrot != null);

        if (numTasks > 1)
        {
            if (threadCluster?.NumThreads != numTasks)
            {
                threadCluster?.Dispose();
                threadCluster = new ThreadCluster(numTasks, threadPriority: threadPriority ?? DefaultThreadPriority);
            }
            else
            {
                if (threadPriority != null)
                {
                    threadCluster.SetAllThreadPriority(threadPriority.Value);
                }
            }
        }

        result = new MandelbrotResult()
        {
            ImplementationName = mandelbrotType.GetMandelbrotName(),
            Width = width,
            Height = height,
            MaxIterations = maxIterations,
            Region = region,
            NumTasks = numTasks
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void StartCalculation(Type mandelbrotType, int width, int height, MandelbrotRegion region, int maxIterations, int numTasks)
    {
        ThrowIfNotPositive(maxIterations, nameof(maxIterations));

        PrepareCalculation(mandelbrotType, width, height, numTasks, in region, maxIterations);

        task = Task.Run(() => CalculationAction());
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    void Calculate(int maxIterations = -1)
    {
        if (mandelbrot == null)
        {
            return;
        }

        maxIterations = maxIterations > 0 ? maxIterations : result.MaxIterations;

        Stopwatch stopwatch = new();

        if (threadCluster == null || result.NumTasks == 1)
        {
            stopwatch.Start();
            mandelbrot.Calculate(rectangle: result.Region, maxIterations: maxIterations);
            stopwatch.Stop();
        }
        else
        {
            stopwatch.Start();
            mandelbrot.Calculate(rectangle: result.Region, maxIterations: maxIterations, threadCluster);
            stopwatch.Stop();
        }

        result.ElapsedTime = stopwatch.Elapsed;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    void CalculationAction()
    {
        Calculate();

        if (mandelbrot?.IsCalculationCancellationRequested != false)
        {
            return;
        }

        CalculationCompleted?.Invoke(mandelbrot, result);
    }

    [SuppressMessage("Design", "CA1003:Use generic event handler instances", Justification = "We don't care")]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "It's necessary")]
    public event Action<MandelbrotBase, MandelbrotResult>? CalculationCompleted;

    public void Calculate(Type mandelbrotType, int width, int height, in MandelbrotRegion region, int maxIterations, int numTasks, bool benchmark = false)
    {
        ThrowIfNotPositive(maxIterations, nameof(maxIterations));

        PrepareCalculation(mandelbrotType, width, height, numTasks, in region, maxIterations, benchmark ? BenchmarkThreadPriority : null);

        if (benchmark)
        {
            Calculate(maxIterations: 16);
        }

        bool noGCRegion = false;

        try
        {
            noGCRegion = benchmark && GC.TryStartNoGCRegion(1024 * 1024);

            Calculate();
        }
        finally
        {
            if (noGCRegion)
            {
                GC.EndNoGCRegion();
            }

            if (benchmark)
            {
                threadCluster?.SetAllThreadPriority(DefaultThreadPriority);
            }
        }
    }
}
