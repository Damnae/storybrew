using System;
using System.Collections.Generic;
using System.Threading;

namespace StorybrewEditor.Util
{
    /// <summary>
    /// Schedules an action on the main thread until it succeeds.
    /// Actions come with a key that prevents queuing the same one multiple times.
    /// </summary>
    public class ThrottledActionScheduler
    {
        private readonly HashSet<string> scheduled = new HashSet<string>();

        public int Delay = 100;

        public void Schedule(string key, Action<string> action)
            => Schedule(key, (k) =>
            {
                action(k);
                return true;
            });

        public void Schedule(string key, Func<string, bool> action)
        {
            lock (scheduled)
            {
                if (scheduled.Contains(key)) return;
                scheduled.Add(key);

                queue(key, action);
            }
        }

        private void queue(string key, Func<string, bool> action)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                Thread.Sleep(Delay);
                Program.Schedule(() =>
                {
                    lock (scheduled)
                        if (action(key))
                            scheduled.Remove(key);
                        else queue(key, action);
                });
            });
        }
    }
}
