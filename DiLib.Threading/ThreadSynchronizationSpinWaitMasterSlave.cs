using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace DiLib.Threading;

public class ThreadSynchronizationSpinWaitMasterSlave : ThreadSynchronizationSpinWaitNeighbors
{
    protected override void NumThreadsChanged()
    {
        base.NumThreadsChanged();

        SetSynchronizationAction(0, SynchronizeMasterThread);

        var useX86Pause = X86Base.IsSupported;
        for (int i = 1; i < NumThreads; ++i)
        {
            var threadIndex = i;
            Action<int> action = useX86Pause ? SynchronizeSlaveThread<bool> : SynchronizeSlaveThread<object>;
            SetSynchronizationAction(threadIndex, action);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    void SynchronizeMasterThread(int threadIndex)
    {
        var syncValue = syncCounters[0] + 1;

        int j0 = 1; int j1 = syncCounters.Length - 1;

        while (j0 < j1)
        {
            var check0 = Volatile.Read(ref syncCounters[j0]) == syncValue;
            j0 += Unsafe.As<bool, Byte>(ref check0);

            var check1 = Volatile.Read(ref syncCounters[j1]) == syncValue;
            j1 -= Unsafe.As<bool, Byte>(ref check1);
        }

        if (j0 == j1)
        {
            while (Volatile.Read(ref syncCounters[j0]) != syncValue) ;
        }

        syncCounters[0] = syncValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void SynchronizeSlaveThread<T>(int threadIndex)
    {
        var sleepTicks = Stopwatch.Frequency / 1000;

        var syncValue = ++syncCounters[threadIndex];

        var ticksStart = Stopwatch.GetTimestamp();

        int iterations = typeof(T) == typeof(bool) ? 1000 : 10000;

        while (true)
        {
            for (int j = iterations; j > 0; j--)
            {
                if (typeof(T) == typeof(bool))
                {
                   X86Base.Pause();
                }

                if (Volatile.Read(ref syncCounters[0]) == syncValue) goto ExitWait;
            }

            if (Stopwatch.GetTimestamp() - ticksStart > sleepTicks)
            {
                Thread.Sleep(0);
                ticksStart = Stopwatch.GetTimestamp();
            }
        }
    ExitWait:;
    }
}
