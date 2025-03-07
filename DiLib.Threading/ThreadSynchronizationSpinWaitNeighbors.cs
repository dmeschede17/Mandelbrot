using System.Diagnostics;

namespace DiLib.Threading;

public class ThreadSynchronizationSpinWaitNeighbors : ThreadSynchronizationSpinWait
{
    protected override void NumThreadsChanged()
    {
        base.NumThreadsChanged();

        if (NumThreads == 0)
        {
        }
        else if (NumThreads == 1)
        {
            SetSynchronizationAction(0, NoThreadSynchronization);
        }
        else
        {
            SetSynchronizationAction(0, SynchronizeThread0);

            var numThreadsMinus1 = NumThreads - 1;
            for (int i = 1; i < numThreadsMinus1; i++)
            {
                SetSynchronizationAction(i, SynchronizeThread);
            }

            SetSynchronizationAction(numThreadsMinus1, SynchronizeThreadPrevious);
        }
    }

    public void SynchronizeThread0(int threadIndex)
    {
        Debug.Assert(threadIndex == 0);

        var syncValue = --syncCounters[0];

        while (Volatile.Read(ref syncCounters[Factor]) > syncValue) ;
    }

    public void SynchronizeThreadPrevious(int threadIndex)
    {
        Debug.Assert(threadIndex > 0);

        var syncValue = --syncCounters[Factor * threadIndex];

        var threadIndexMinusOne = threadIndex - 1;

        while (Volatile.Read(ref syncCounters[Factor * threadIndexMinusOne]) > syncValue) ;
    }

    public void SynchronizeThread(int threadIndex)
    {
        Debug.Assert(threadIndex > 0);
        Debug.Assert(threadIndex < NumThreads - 1);

        var syncValue = --syncCounters[Factor * threadIndex];

        var threadIndexMinusOne = threadIndex - 1;

        while (Volatile.Read(ref syncCounters[Factor * threadIndexMinusOne]) > syncValue) ;

        var threadIndexPlusOne = threadIndex + 1;

        while (Volatile.Read(ref syncCounters[Factor * threadIndexPlusOne]) > syncValue) ;
    }
}
