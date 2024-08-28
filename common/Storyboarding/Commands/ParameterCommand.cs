using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class ParameterCommand : Command<CommandParameter>
    {
        public override bool MaintainValue => StartTime == EndTime;
        public override bool ExportEndValue => false;

        public ParameterCommand(OsbEasing easing, double startTime, double endTime, CommandParameter value)
            : base("P", easing, startTime, endTime, value, value)
        {
            if (value == CommandParameter.None)
                throw new InvalidOperationException($"Parameter command cannot be None");
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