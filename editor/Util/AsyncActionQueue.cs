﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace StorybrewEditor.Util
{
    public class AsyncActionQueue<T> : IDisposable
    {
        private readonly ActionQueueContext context = new();
        private readonly List<ActionRunner> actionRunners = [];
        private readonly bool allowDuplicates;

        public delegate void ActionFailedEventHandler(T target, Exception e);
        public event ActionFailedEventHandler OnActionFailed
        {
            add => context.OnActionFailed += value;
            remove => context.OnActionFailed -= value;
        }

        public bool Enabled
        {
            get => context.Enabled;
            set => context.Enabled = value;
        }

        public int TaskCount
        {
            get
            {
                lock (context.Queue)
                    lock (context.Running)
                        return context.Queue.Count + context.Running.Count;
            }
        }

        public AsyncActionQueue(string threadName, bool allowDuplicates = false, int runnerCount = 0)
        {
            this.allowDuplicates = allowDuplicates;

            if (runnerCount == 0)
                runnerCount = Environment.ProcessorCount - 1;
            runnerCount = Math.Max(1, runnerCount);

            for (var i = 0; i < runnerCount; i++)
                actionRunners.Add(new ActionRunner(context, $"{threadName} #{i + 1}"));
        }

        public void Queue(T target, Action<T, CancellationTokenSource> action, bool mustRunAlone = false)
            => Queue(target, null, action, mustRunAlone);

        public void Queue(T target, string uniqueKey, Action<T, CancellationTokenSource> action, bool mustRunAlone = false)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(AsyncActionQueue<T>));

            foreach (var r in actionRunners)
                r.EnsureThreadAlive();

            lock (context.Queue)
            {
                if (!allowDuplicates && context.Queue.Any(q => q.Target.Equals(target)))
                    return;

                context.Queue.Add(new ActionContainer()
                {
                    Target = target,
                    UniqueKey = uniqueKey,
                    Action = action,
                    MustRunAlone = mustRunAlone,
                });
                Monitor.PulseAll(context.Queue);
            }
        }

        public void AbortQueuedActions(bool stopThreads)
        {
            var cancellationTokens = new List<CancellationTokenSource>();
            lock (context.Queue)
            {
                foreach (var actionContainer in context.Queue)
                {
                    actionContainer.CancellationTokenSource.Cancel();
                    cancellationTokens.Add(actionContainer.CancellationTokenSource);
                }
                context.Queue.Clear();
            }

            if (stopThreads)
            {
                var sw = new Stopwatch();
                sw.Start();
                foreach (var r in actionRunners)
                    r.JoinOrAbort(Math.Max(1000, 5000 - (int)sw.ElapsedMilliseconds));
            }

            foreach (var cancellationToken in cancellationTokens)
                cancellationToken.Dispose();
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    context.Enabled = false;
                    AbortQueuedActions(true);
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        private class ActionContainer
        {
            public T Target;
            public string UniqueKey;
            public Action<T, CancellationTokenSource> Action;
            public bool MustRunAlone;

            public readonly CancellationTokenSource CancellationTokenSource = new();
        }

        private class ActionQueueContext
        {
            public readonly List<ActionContainer> Queue = new List<ActionContainer>();
            public readonly HashSet<string> Running = new HashSet<string>();
            public bool RunningLoneTask;

            public event ActionFailedEventHandler OnActionFailed;
            public bool TriggerActionFailed(T target, Exception e)
            {
                if (OnActionFailed == null)
                    return false;

                OnActionFailed.Invoke(target, e);
                return true;
            }

            private bool enabled;
            public bool Enabled
            {
                get => enabled;
                set
                {
                    if (enabled == value)
                        return;

                    enabled = value;

                    if (enabled)
                        lock (Queue)
                            if (Queue.Count > 0)
                                Monitor.PulseAll(Queue);
                }
            }
        }

        private class ActionRunner
        {
            private readonly ActionQueueContext context;
            private readonly string threadName;

            private Thread thread;

            public ActionRunner(ActionQueueContext context, string threadName)
            {
                this.context = context;
                this.threadName = threadName;
            }

            public void EnsureThreadAlive()
            {
                if (thread == null || !thread.IsAlive)
                {
                    Thread localThread = null;
                    thread = localThread = new Thread(() =>
                    {
                        var mustSleep = false;
                        while (true)
                        {
                            if (mustSleep)
                            {
                                // Thread must sleep with no lock taken in case the queue was not empty but no task was able to start.
                                Thread.Sleep(200);
                                mustSleep = false;
                            }

                            ActionContainer task;
                            lock (context.Queue)
                            {
                                while (!context.Enabled || context.Queue.Count == 0)
                                {
                                    if (thread != localThread)
                                    {
                                        Trace.WriteLine($"Exiting thread {localThread.Name}.");
                                        return;
                                    }
                                    Monitor.Wait(context.Queue);
                                }

                                lock (context.Running)
                                {
                                    if (context.RunningLoneTask)
                                    {
                                        mustSleep = true;
                                        continue;
                                    }

                                    task = context.Queue.FirstOrDefault(t => !context.Running.Contains(t.UniqueKey)
                                        && !t.MustRunAlone || t.MustRunAlone && context.Running.Count == 0);

                                    if (task == null)
                                    {
                                        mustSleep = true;
                                        continue;
                                    }

                                    context.Queue.Remove(task);
                                    context.Running.Add(task.UniqueKey);
                                    if (task.MustRunAlone)
                                        context.RunningLoneTask = true;
                                }
                            }

                            try
                            {
                                task.Action.Invoke(task.Target, task.CancellationTokenSource);
                            }
                            catch (Exception e)
                            {
                                var target = task.Target;
                                Program.Schedule(() =>
                                {
                                    if (!context.TriggerActionFailed(target, e))
                                        Trace.WriteLine($"Action failed for '{task.UniqueKey}': {e}");
                                });
                            }

                            lock (context.Running)
                            {
                                context.Running.Remove(task.UniqueKey);
                                if (task.MustRunAlone)
                                    context.RunningLoneTask = false;
                            }
                        }
                    })
                    { Name = threadName, IsBackground = true, };

                    Trace.WriteLine($"Starting thread {thread.Name}.");
                    thread.Start();
                }
            }

            public void JoinOrAbort(int millisecondsTimeout)
            {
                if (thread == null)
                    return;

                var localThread = thread;
                thread = null;

                lock (context.Queue)
                    Monitor.PulseAll(context.Queue);

                if (!localThread.Join(millisecondsTimeout))
                {
                    Trace.WriteLine($"Aborting thread {localThread.Name}.");
                    IntPtr handle = Native.OpenThread(0x0001, false, (uint)localThread.ManagedThreadId);
                    Native.TerminateThread(handle, 1);
                    Native.CloseHandle(handle);
                }
            }
        }
    }
}
