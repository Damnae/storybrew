using System;
using System.Threading;

namespace StorybrewCommon.Util
{
    public static class Misc
    {
        public static void WithRetries(Action action, int timeout = 2000)
        {
            WithRetries(() =>
            {
                action();
                return true;
            },
            timeout);
        }

        public static T WithRetries<T>(Func<T> action, int timeout = 2000)
        {
            var sleepTime = 0;
            while (true)
            {
                try
                {
                    return action();
                }
                catch
                {
                    if (sleepTime >= timeout) throw;

                    var retryDelay = timeout / 10;
                    sleepTime += retryDelay;
                    Thread.Sleep(retryDelay);
                }
            }
        }
    }
}
