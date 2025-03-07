using System.Runtime.InteropServices;

namespace DiLib.Threading;

public class ThreadSynchronizationSpinWait : ThreadSynchronization
{
    protected const int Factor = 1;

#pragma warning disable CA1051 // Do not declare visible instance fields
    protected int[] syncCounters = [];
#pragma warning restore CA1051
    GCHandle syncCountersGCHandle;

    public override void Initialize()
    {
        syncCounters.AsSpan().Fill(int.MaxValue);
    }

    protected override void Dispose(bool disposing)
    {
        if (syncCountersGCHandle.IsAllocated)
        {
            syncCountersGCHandle.Free();
            syncCountersGCHandle = new();
        }

        syncCounters = [];
    }

    protected override void NumThreadsChanged()
    {
        if (syncCountersGCHandle.IsAllocated)
        {
            syncCountersGCHandle.Free();
            syncCountersGCHandle = new();
        }

        syncCounters = new int[Factor * NumThreads];
        syncCountersGCHandle = GCHandle.Alloc(syncCounters, GCHandleType.Pinned);
        var syncCountersAddr = syncCountersGCHandle.AddrOfPinnedObject();
        var syncCountersSize = sizeof(int) * Factor * NumThreads;

        for (int i = 0; i < NumThreads; i++)
        {
            SetSynchronizationData(i, syncCountersAddr, syncCountersSize);
        }

        Initialize();
    }
}
