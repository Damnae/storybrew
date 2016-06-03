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
        private List<ActionContainer> queue = new List<ActionContainer>();
        private HashSet<string> running = new HashSet<string>();

        public delegate void ActionFailedEventHandler(T target, Exception e);
        public event ActionFailedEventHandler OnActionFailed;

        private bool enabled;
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (enabled == value)
                    return;

                enabled = value;

                lock (queue)
                    if (queue.Count > 0)
                        Monitor.Pulse(queue);
            }
        }

        public AsyncActionQueue(string threadName, bool allowDuplicates = false)
        {
            this.threadName = threadName;
            this.allowDuplicates = allowDuplicates;
        }

        public void Queue(T target, Action<T> action)
            => Queue(target, null, action);

        public void Queue(T target, string uniqueKey, Action<T> action)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(AsyncActionQueue<T>));

            if (thread == null || !thread.IsAlive)
            {
                Thread localThread = null;
                thread = localThread = new Thread(() =>
                {
                    while (true)
                    {
                        lock (queue)
                        {
                            while (!enabled || queue.Count == 0)
                            {
                                if (thread != localThread)
                                {
                                    Trace.WriteLine($"Stopping {localThread.Name} thread");
                                    return;
                                }
                                Monitor.Wait(queue);
                            }

                            var startedTasks = new List<ActionContainer>();
                            lock (running)
                                foreach (var task in queue)
                                {
                                    if (running.Contains(task.UniqueKey))
                                        continue;

                                    running.Add(task.UniqueKey);
                                    startedTasks.Add(task);

                                    var taskToRun = task;
                                    ThreadPool.QueueUserWorkItem((state) =>
                                    {
                                        try
                                        {
                                            taskToRun.Action.Invoke(taskToRun.Target);
                                        }
                                        catch (Exception e)
                                        {
                                            var toUpdateTarget = taskToRun.Target;
                                            Program.Schedule(() =>
                                            {
                                                if (OnActionFailed != null) OnActionFailed.Invoke(toUpdateTarget, e);
                                                else Trace.WriteLine($"Action failed for '{taskToRun.Target}': {e}");
                                            });
                                        }
                                        lock (running)
                                            running.Remove(taskToRun.UniqueKey);
                                        lock (queue)
                                            if (queue.Count > 0)
                                                Monitor.Pulse(queue);
                                    });
                                }
                            foreach (var startedTask in startedTasks)
                                queue.Remove(startedTask);
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
                        if (queued.Target.Equals(target))
                            return;

                queue.Add(new ActionContainer() { Target = target, UniqueKey = uniqueKey, Action = action });
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
            public string UniqueKey;
            public Action<T> Action;
        }
    }
}
