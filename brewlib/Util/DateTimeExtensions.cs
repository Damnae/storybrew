using System;
using System.Collections.Generic;

namespace BrewLib.Util
{
    public static class DateTimeExtensions
    {
        private static Dictionary<long, string> thresholds = new Dictionary<long, string>()
        {
            [60] = "{0} seconds ago",
            [60 * 2] = "a minute ago",
            [45 * 60] = "{0} minutes ago",
            [120 * 60] = "an hour ago",
            [24 * 3600] = "{0} hours ago",
            [24 * 3600 * 2] = "yesterday",
            [24 * 3600 * 30] = "{0} days ago",
            [24 * 3600 * 30 * 2] = "a month ago",
            [24 * 3600 * 365] = "{0} months ago",
            [24 * 3600 * 365 * 2] = "a year ago",
            [long.MaxValue] = "{0} years ago",
        };

        public static string ToTimeAgo(this DateTime date)
        {
            var seconds = (DateTime.Now.Ticks - date.Ticks) / 10000000;
            foreach (var threshold in thresholds)
                if (seconds < threshold.Key)
                {
                    var timespan = new TimeSpan((DateTime.Now.Ticks - date.Ticks));
                    return string.Format(threshold.Value,
                        (timespan.Days > 365 ? timespan.Days / 365 :
                        (timespan.Days > 30 ? timespan.Days / 30 :
                        (timespan.Days > 0 ? timespan.Days :
                        (timespan.Hours > 0 ? timespan.Hours :
                        (timespan.Minutes > 0 ? timespan.Minutes :
                        (timespan.Seconds > 0 ? timespan.Seconds : 0)))))).ToString());
                }
            throw new InvalidOperationException();
        }
    }
}
