using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DiLib.Threading;

public sealed class ThreadCluster : IDisposable
{
    readonly ManualResetEvent[] startWaitHandles;
    readonly CountdownEvent actionFinishedCountdownEvent;

    readonly ThreadClusterThread[] threads;

    readonly Stopwatch stopwatch;
    int startWaitHandleIndex;

    public int NumThreads { get; }

    public TimeSpan Elapsed => stopwatch.Elapsed;

    public ThreadCluster(int numThreads, ThreadPriority threadPriority = ThreadPriority.Normal, ThreadPriority? thread0Priority = null, bool warmUp = false)
    {
        this.NumThreads = numThreads;

        startWaitHandles = new[] { new ManualResetEvent(false), new ManualResetEvent(false) };
        startWaitHandleIndex = 0;

        actionFinishedCountdownEvent = new CountdownEvent(numThreads);

        threads = new ThreadClusterThread[numThreads];

        for (int i = 0; i < numThreads; i++)
        {
            threads[i] = new ThreadClusterThread(i == 0 && thread0Priority != null ? thread0Priority.Value : threadPriority, startWaitHandles[0], startWaitHandles[1], actionFinishedCountdownEvent, i);
        }

        stopwatch = new Stopwatch();

        if (warmUp)
        {
            Run();
        }
    }

    public ref Action ThreadAction(int threadIndex) => ref threads[threadIndex].Action;

    public void SetAllThreadActions(Action action)
    {
        for (int i = 0; i < NumThreads; i++)
        {
            threads[i].Action = action;
        }
    }

    public void SetAllThreadPriority(ThreadPriority threadPriority)
    {
        for (int i = 0; i < NumThreads; i++)
        {
            threads[i].Thread.Priority = threadPriority;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Run()
    {
        ObjectDisposedException.ThrowIf(disposed, GetType());

        stopwatch.Restart();
        startWaitHandles[startWaitHandleIndex].Set();
        actionFinishedCountdownEvent.Wait();
        stopwatch.Stop();

        startWaitHandles[startWaitHandleIndex].Reset();
        actionFinishedCountdownEvent.Reset();

        startWaitHandleIndex = 1 - startWaitHandleIndex;
    }

    bool disposed;

    public void Dispose()
    {
        if (!disposed)
        {
            if (threads != null)
            {
                for (int i = 0; i < NumThreads; ++i)
                {
                    if (threads[i] != null)
                    {
                        threads[i].Action = threads[i].ExitThreadAction;
                    }
                }

                Run();

                for (int i = 0; i < NumThreads; ++i)
                {
                    threads[i].Thread?.Join();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type. It's ok, because object is disposed.
                    threads[i] = null;
#pragma warning restore CS8625
                }
            }

            for (int i = 0; i < startWaitHandles.Length; i++)
            {
                startWaitHandles[i].Dispose();
            }

            actionFinishedCountdownEvent.Dispose();

            disposed = true;
        }
    }
}
