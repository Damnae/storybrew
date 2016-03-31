using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace StorybrewEditor.Util
{
    public class AsyncActionQueue<T> : IDisposable
    {
        private string threadName;
        private bool allowDuplicates;

        private Thread thread;
        private Queue<ActionContainer> queue = new Queue<ActionContainer>();

        public delegate void ActionFailedEventHandler(T target, Exception e);
        public event ActionFailedEventHandler OnActionFailed;

        public AsyncActionQueue(string threadName, bool allowDuplicates = false)
        {
            this.threadName = threadName;
            this.allowDuplicates = allowDuplicates;
        }

        public void Queue(T key, Action<T> action)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(AsyncActionQueue<T>));

            if (thread == null || !thread.IsAlive)
            {
                Thread localThread = null;
                thread = localThread = new Thread(() =>
                {
                    while (true)
                    {
                        ActionContainer toUpdate;
                        lock (queue)
                        {
                            while (queue.Count == 0)
                            {
                                if (thread != localThread)
                                {
                                    Trace.WriteLine($"Stopping {localThread.Name} thread");
                                    return;
                                }
                                Monitor.Wait(queue);
                            }
                            toUpdate = queue.Dequeue();
                        }

                        try
                        {
                            toUpdate.Action.Invoke(toUpdate.Target);
                        }
                        catch (Exception e)
                        {
                            var target = toUpdate.Target;
                            Program.Schedule(() =>
                            {
                                if (OnActionFailed != null) OnActionFailed.Invoke(target, e);
                                else Trace.WriteLine($"Action failed for '{toUpdate.Target}': {e}");
                            });
                        }
                    }
                })
                { Name = threadName, IsBackground = true, };

                Trace.WriteLine($"Starting {localThread.Name} thread");
                thread.Start();
            }

            lock (queue)
            {
                if (!allowDuplicates)
                    foreach (var queued in queue)
                        if (queued.Target.Equals(key))
                            return;

                queue.Enqueue(new ActionContainer() { Target = key, Action = action });
                Monitor.Pulse(queue);
            }
        }

        private void cancelQueuedActions()
        {
            if (thread == null)
                return;

            var localThread = thread;
            lock (queue)
            {
                thread = null;
                queue.Clear();
                Monitor.Pulse(queue);
            }
            if (!localThread.Join(5000))
            {
                Trace.WriteLine($"Aborting {threadName} thread.");
                localThread.Abort();
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    cancelQueuedActions();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        private struct ActionContainer
        {
            public T Target;
            public Action<T> Action;
        }
    }
}
