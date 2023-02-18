using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
#pragma warning disable CS1591
    public class MoveYCommand : Command<CommandDecimal>
    {
        public MoveYCommand(OsbEasing easing, double startTime, double endTime, CommandDecimal startValue, CommandDecimal endValue)
            : base("MY", easing, startTime, endTime, startValue, endValue) { }

        public override CommandDecimal ValueAtProgress(double progress) => StartValue + (EndValue - StartValue) * progress;
        public override CommandDecimal Midpoint(Command<CommandDecimal> endCommand, double progress) => StartValue + (endCommand.EndValue - StartValue) * progress;

        public override IFragmentableCommand GetFragment(double startTime, double endTime)
        {
            if (IsFragmentable)
            {
                var startValue = ValueAtTime(startTime);
                var endValue = ValueAtTime(endTime);
                return new MoveYCommand(Easing, startTime, endTime, startValue, endValue);
            }
            return this;
        }
    }
}