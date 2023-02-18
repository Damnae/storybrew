using System;
using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding.Commands
{
#pragma warning disable CS1591
    public class CommandComparer : Comparer<ICommand>
    {
        public override int Compare(ICommand x, ICommand y) => CompareCommands(x, y);

        public static int CompareCommands(ICommand x, ICommand y)
        {
            var result = ((int)Math.Round(x.StartTime)).CompareTo((int)Math.Round(y.StartTime));
            if (result != 0) return result;
            return ((int)Math.Round(x.EndTime)).CompareTo((int)Math.Round(y.EndTime));
        }
    }
}