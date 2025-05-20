using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class FadeCommand : Command<CommandDecimal>
    {
        public override string Identifier => "F";

        public FadeCommand(OsbEasing easing, double startTime, double endTime, in CommandDecimal startValue, in CommandDecimal endValue)
            : base(easing, startTime, endTime, startValue, endValue)
        {
        }

        public override CommandDecimal ValueAtProgress(double progress)
            => StartValue + (EndValue - StartValue) * progress;

        public override CommandDecimal Midpoint(in Command<CommandDecimal> endCommand, double progress)
            => StartValue + (endCommand.EndValue - StartValue) * progress;

        public override IFragmentableCommand GetFragment(double startTime, double endTime)
        {
            if (IsFragmentable)
            {
                var startValue = ValueAtTime(startTime);
                var endValue = ValueAtTime(endTime);
                return new FadeCommand(Easing, startTime, endTime, startValue, endValue);
            }
            return this;
        }
    }
}