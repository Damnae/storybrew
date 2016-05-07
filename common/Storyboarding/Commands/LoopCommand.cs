using System;
using System.Globalization;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class LoopCommand : CommandGroup
    {
        public int LoopCount { get; set; }
        public override double EndTime
        {
            get
            {
                return StartTime + (CommandsEndTime * LoopCount);
            }
            set
            {
                LoopCount = (int)Math.Floor((value - StartTime) / CommandsEndTime);
            }
        }

        public LoopCommand(double startTime, int loopCount)
        {
            StartTime = startTime;
            LoopCount = loopCount;
        }

        protected override string GetCommandGroupHeader(ExportSettings exportSettings)
            => $"L,{((int)StartTime).ToString(exportSettings.NumberFormat)},{LoopCount.ToString(exportSettings.NumberFormat)}";
    }
}
