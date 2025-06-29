﻿
using System.Diagnostics;

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
            protected set
            {
                LoopCount = (int)Math.Floor((value - StartTime) / CommandsEndTime);
            }
        }

        public LoopCommand(double startTime, int loopCount)
        {
            StartTime = startTime;
            LoopCount = loopCount;
        }

        public override void EndGroup()
        {
            // Make commands loop from their earliest start time (like osu! does)
            var commandsStartTime = CommandsStartTime;
            if (commandsStartTime > 0)
            {
                StartTime += commandsStartTime;
                foreach (var command in Commands)
                    (command as IOffsetable).Offset(-commandsStartTime);
            }
            base.EndGroup();
        }

        public override bool IsFragmentableAt(double time)
        {
            Debug.Assert(CommandsStartTime == 0);
            for (var i = 1; i < LoopCount - 1; i++)
                if (time == StartTime + i * CommandsEndTime)
                    return true;
            return false;
        }

        protected override string GetCommandGroupHeader(ExportSettings exportSettings)
            => $"L,{((int)StartTime).ToString(exportSettings.NumberFormat)},{LoopCount.ToString(exportSettings.NumberFormat)}";
    }
}
