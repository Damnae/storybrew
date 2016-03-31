using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class MoveYCommand : Command<CommandDecimal>
    {
        public MoveYCommand(OsbEasing easing, double startTime, double endTime, CommandDecimal startValue, CommandDecimal endValue)
            : base("MY", easing, startTime, endTime, startValue, endValue)
        {
        }

        public override CommandDecimal ValueAtProgress(double progress)
            => StartValue + (EndValue - StartValue) * progress;

        public override CommandDecimal Midpoint(Command<CommandDecimal> endCommand, double progress)
            => StartValue + (endCommand.EndValue - StartValue) * progress;
    }
}