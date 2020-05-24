using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class ParameterCommand : Command<CommandParameter>
    {
        public ParameterCommand(OsbEasing easing, double startTime, double endTime, CommandParameter startValue)
            : base("P", easing, startTime, endTime, startValue, startValue)
        {
        }

        public override CommandParameter ValueAtProgress(double progress)
            => StartValue;

        public override CommandParameter Midpoint(Command<CommandParameter> endCommand, double progress)
            => StartValue;

        public override IFragmentableCommand GetFragment(double startTime, double endTime)
        {
            var startValue = ValueAtTime(startTime);
            return new ParameterCommand(Easing, startTime, endTime, startValue);
        }
    }
}