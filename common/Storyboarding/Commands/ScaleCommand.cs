using StorybrewCommon.Storyboarding.CommandValues;
using System;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class ScaleCommand : Command<CommandDecimal>
    {
        public ScaleCommand(OsbEasing easing, double startTime, double endTime, CommandDecimal startValue, CommandDecimal endValue)
            : base("S", easing, startTime, endTime, startValue, endValue)
        {
        }

        // Scale commands cannot return a negative size
        public override CommandDecimal ValueAtProgress(double progress)
            => Math.Max(0, StartValue + (EndValue - StartValue) * progress);

        public override CommandDecimal Midpoint(Command<CommandDecimal> endCommand, double progress)
            => StartValue + (endCommand.EndValue - StartValue) * progress;

        public override IFragmentableCommand GetFragment(double startTime, double endTime)
        {
            // Cannot fragment a command that has includes a negative size
            if (IsFragmentable && StartValue >= 0 && EndValue >= 0)
            {
                var startValue = ValueAtTime(startTime);
                var endValue = ValueAtTime(endTime);
                return new ScaleCommand(Easing, startTime, endTime, startValue, endValue);
            }
            return this;
        }
    }
}