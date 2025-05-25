using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class MoveYCommand : Command<CommandDecimal>
    {
        public override string Identifier => "MY";

        public MoveYCommand(OsbEasing easing, double startTime, double endTime, in CommandDecimal startValue, in CommandDecimal endValue)
            : base(easing, startTime, endTime, startValue, endValue)
        {
        }

        public override CommandDecimal GetTransformedStartValue(StoryboardTransform transform) => transform.ApplyToPositionY(StartValue);
        public override CommandDecimal GetTransformedEndValue(StoryboardTransform transform) => transform.ApplyToPositionY(EndValue);

        public override CommandDecimal ValueAtProgress(double progress)
            => StartValue + (EndValue - StartValue) * progress;

        public override CommandDecimal Midpoint(in Command<CommandDecimal> endCommand, double progress)
            => StartValue + (endCommand.EndValue - StartValue) * progress;
    }
}