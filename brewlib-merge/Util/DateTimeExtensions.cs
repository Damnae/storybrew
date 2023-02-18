using System;
using System.Collections.Generic;

namespace BrewLib.Util
{
    public static class DateTimeExtensions
    {
        static readonly Dictionary<long, string> thresholds = new Dictionary<long, string>()
        {
            [60] = "{0} seconds ago",
            [120] = "a minute ago",
            [2700] = "{0} minutes ago",
            [7200] = "an hour ago",
            [86400] = "{0} hours ago",
            [172800] = "yesterday",
            [2592000] = "{0} days ago",
            [5184000] = "a month ago",
            [31536000] = "{0} months ago",
            [63072000] = "a year ago",
            [long.MaxValue] = "{0} years ago",
        };
        public static string ToTimeAgo(this DateTime date)
        {
            var seconds = (DateTime.Now.Ticks - date.Ticks) / 10000000;
            foreach (var threshold in thresholds) if (seconds < threshold.Key)
                {
                    var timespan = new TimeSpan(DateTime.Now.Ticks - date.Ticks);
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