using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class ParameterCommand : Command<CommandParameter>
    {
        public override string Identifier => "P";
        public override bool MaintainValue => StartTime == EndTime;
        public override bool ExportEndValue => false;

        public ParameterCommand(OsbEasing easing, double startTime, double endTime, in CommandParameter value)
            : base(easing, startTime, endTime, value, value)
        {
            if (value == CommandParameter.None)
                throw new InvalidOperationException($"Parameter command cannot be None");
        }

        public override CommandParameter ValueAtProgress(double progress)
            => StartValue;

        public override CommandParameter Midpoint(in Command<CommandParameter> endCommand, double progress)
            => StartValue;
    }
}