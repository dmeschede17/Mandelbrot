using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace DiLib.Threading;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "It's a DTO")]
public struct ThreadSynchronizationData
{
    public int ThreadIndex { get; init; }
    public int NumThreads { get; init; }
    public IntPtr SynchronizationData { get; internal set; }
    public int SynchronizationDataSize { get; internal set; }
    public Action<int> SynchronizationAction { get; internal set; }

    public void Synchronize() => SynchronizationAction(ThreadIndex);
}

public abstract class ThreadSynchronization : IDisposable
{
    public abstract void Initialize();

    protected static void NoThreadSynchronization(int threadIndex) { }

    public static ThreadSynchronizationData OneThreadSynchronizationData { get; } = new ThreadSynchronizationData { ThreadIndex = 0, NumThreads = 1, SynchronizationAction = NoThreadSynchronization };

    int numThreads;
    Action<int>[] synchronizationActions = Array.Empty<Action<int>>();
    ThreadSynchronizationData[] synchronizationData = Array.Empty<ThreadSynchronizationData>();

    public int NumThreads
    {
        get => numThreads;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(NumThreads), $"{nameof(NumThreads)} must be > 0");
            }

            if (value != numThreads)
            {
                numThreads = value;
                synchronizationActions = new Action<int>[numThreads];

                synchronizationData = new ThreadSynchronizationData[numThreads];

                for (int i = 0; i < numThreads; i++)
                {
                    synchronizationData[i] = new ThreadSynchronizationData { ThreadIndex = i, NumThreads = numThreads };
                }

                NumThreadsChanged();
            }
        }
    }

    public Action<int> GetSynchronizationAction(int threadIndex) => synchronizationActions[threadIndex];

    public ref readonly ThreadSynchronizationData GetSynchronizationData(int threadIndex) => ref synchronizationData[threadIndex];

    protected void SetSynchronizationAction(int threadIndex, Action<int> action)
    { 
        synchronizationActions[threadIndex] = action;
        synchronizationData[threadIndex].SynchronizationAction = action;
    }

    protected void SetSynchronizationData(int threadIndex, IntPtr data, int size)
    {
        synchronizationData[threadIndex].SynchronizationData = data;
        synchronizationData[threadIndex].SynchronizationDataSize = size;
    }

    protected abstract void NumThreadsChanged();

    protected abstract void Dispose(bool disposing);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    ~ThreadSynchronization()
    {
        Dispose(disposing: false);
    }
}
