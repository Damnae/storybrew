using System;

namespace BrewLib.Util
{
    public static class EventHelper
    {
        public static void InvokeStrict(Func<MulticastDelegate> getEventDelegate, Action<Delegate> raise)
        {
            var invocationList = getEventDelegate()?.GetInvocationList();
            if (invocationList == null) return;

            var first = true;
            foreach (var handler in invocationList)
            {
                if (first) first = false;
                else
                {
                    var currentList = getEventDelegate()?.GetInvocationList();
                    if (currentList == null) return;

                    if (!Array.Exists(currentList, h => h == handler)) continue;
                }
                raise(handler);
            }
        }
    }
}