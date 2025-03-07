using System.Runtime.CompilerServices;

namespace DiLib.Threading;

public class ThreadClusterThread
{
    [InlineArray(Length)]
    internal struct TwoWaitHandles
    {
        internal const int Length = 2;
        internal WaitHandle waitHandle0;
    }

    [ThreadStatic]
    static int threadIndex;

    public static int ThreadIndex => threadIndex;

    TwoWaitHandles StartWaitHandles;
    CountdownEvent ActionFinishedCountdownEvent { get; }

    Action action;
    bool ExitThread { get; set; }

    internal Thread Thread { get; }

    internal ref Action Action => ref action;

    internal ThreadClusterThread(ThreadPriority threadPriority, WaitHandle startWaitHandle0, WaitHandle startWaitHandle1, CountdownEvent actionFinishedCountdownEvent, int threadIndex)
    {
        StartWaitHandles[0] = startWaitHandle0;
        StartWaitHandles[1] = startWaitHandle1;
        ActionFinishedCountdownEvent = actionFinishedCountdownEvent;

        action = Nop;
        ExitThread = false;

        Thread = new Thread(ThreadStart) { Name = "Thread Collection Thread", IsBackground = true, Priority = threadPriority };
        Thread.Start(threadIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static void Nop() { }

    internal void ExitThreadAction() { ExitThread = true; }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    void ThreadStart(object? threadIndex)
    {
        if (threadIndex is not int)
        {
            throw new ArgumentException($"Type of argument '{nameof(threadIndex)}' must be int!", nameof(threadIndex));
        }

        ThreadClusterThread.threadIndex = (int)threadIndex;

        var startWaitHandleIndex = 0;

        while (!ExitThread)
        {
            StartWaitHandles[startWaitHandleIndex].WaitOne();
            action();
            ActionFinishedCountdownEvent.Signal();
            startWaitHandleIndex = 1 - startWaitHandleIndex;
        }
    }
}
