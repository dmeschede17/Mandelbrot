using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DiLib.Threading;

public class ThreadSynchronizationCountdownEvent : ThreadSynchronization
{
    CountdownEvent? CountdownEvent { get; set; }
    ManualResetEvent[] ManualResetEvents { get; }

    int manualResetEventIndex;

    public ThreadSynchronizationCountdownEvent()
    {
        ManualResetEvents = new ManualResetEvent[] { new ManualResetEvent(false), new ManualResetEvent(false) };
    }

    public override void Initialize()
    {
        ManualResetEvents[0].Reset();

        manualResetEventIndex = 0;
    }

    protected override void NumThreadsChanged()
    {
        CountdownEvent?.Dispose();

        CountdownEvent = new CountdownEvent(NumThreads - 1);

        ManualResetEvents[0].Reset();
        ManualResetEvents[1].Reset();

        manualResetEventIndex = 0;

        SetSynchronizationAction(0, SynchronizeMasterThread);

        for (int i = 1; i < NumThreads; ++i)
        {
            SetSynchronizationAction(i, SynchronizeSlaveThread);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    void SynchronizeMasterThread(int threadIndex)
    {
        Debug.Assert(threadIndex == 0);

        if (CountdownEvent == null)
        {
            return;
        }

        CountdownEvent.Wait();

        CountdownEvent.Reset();

        var currentManualResetEventIndex = manualResetEventIndex;
        var newManualResetEventIndex = 1 - currentManualResetEventIndex;

        Volatile.Write(ref manualResetEventIndex, newManualResetEventIndex);

        ManualResetEvents[newManualResetEventIndex].Reset();
        ManualResetEvents[currentManualResetEventIndex].Set();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void SynchronizeSlaveThread(int threadIndex)
    {
        if (CountdownEvent == null)
        {
            return;
        }

        var currentManualResetEventIndex = Volatile.Read(ref manualResetEventIndex);

        CountdownEvent.Signal();

        ManualResetEvents[currentManualResetEventIndex].WaitOne();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CountdownEvent?.Dispose();

            for (int i = 0; i < ManualResetEvents.Length; i++)
            {
                ManualResetEvents[i].Dispose();
            }
        }
    }
}
