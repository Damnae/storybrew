using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class MoveCommand : Command<CommandPosition>
    {
        public override string Identifier => "M";

        public MoveCommand(OsbEasing easing, double startTime, double endTime, in CommandPosition startValue, in CommandPosition endValue)
            : base(easing, startTime, endTime, startValue, endValue)
        {
        }

        public override CommandPosition GetTransformedStartValue(StoryboardTransform transform) => transform.ApplyToPosition(StartValue);
        public override CommandPosition GetTransformedEndValue(StoryboardTransform transform) => transform.ApplyToPosition(EndValue);

        public override CommandPosition ValueAtProgress(double progress)
            => StartValue + (EndValue - StartValue) * progress;

        public override CommandPosition Midpoint(in Command<CommandPosition> endCommand, double progress)
            => new CommandPosition(StartValue.X + (endCommand.EndValue.X - StartValue.X) * progress, StartValue.Y + (endCommand.EndValue.Y - StartValue.Y) * progress);
    }
}