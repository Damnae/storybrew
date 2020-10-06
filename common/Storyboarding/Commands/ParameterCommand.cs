using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class ParameterCommand : Command<CommandParameter>
    {
        public override bool ExportEndValue => false;

        public ParameterCommand(OsbEasing easing, double startTime, double endTime, CommandParameter startValue)
            : base("P", easing, startTime, endTime, startValue, ((int)endTime - (int)startTime) == 0 ? startValue : CommandParameter.None)
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