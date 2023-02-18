using System;
using System.Collections.Generic;

namespace StorybrewEditor.Util
{
    ///<summary> Schedules an action on the main thread until it succeeds. Actions come with a key that prevents queuing the same one multiple times. </summary>
    public class ThrottledActionScheduler
    {
        readonly HashSet<string> scheduled = new HashSet<string>();

        public int Delay = 100;

        public void Schedule(string key, Action<string> action) => Schedule(key, (k) =>
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
            }
            Program.Schedule(() =>
            {
                lock (scheduled) scheduled.Remove(key);
                if (!action(key)) Schedule(key, action);
            }, Delay);
        }
    }
}