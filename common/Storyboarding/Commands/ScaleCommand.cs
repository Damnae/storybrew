using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class ScaleCommand : Command<CommandDecimal>
    {
        public override string Identifier => "S";

        public ScaleCommand(OsbEasing easing, double startTime, double endTime, in CommandDecimal startValue, in CommandDecimal endValue)
            : base(easing, startTime, endTime, startValue, endValue)
        {
        }

        public override bool IsFragmentableAt(double time) => base.IsFragmentableAt(time) && StartValue >= 0 && EndValue >= 0;

        public override CommandDecimal GetTransformedStartValue(StoryboardTransform transform) => transform.ApplyToScale(StartValue);
        public override CommandDecimal GetTransformedEndValue(StoryboardTransform transform) => transform.ApplyToScale(EndValue);

        // Scale commands cannot return a negative size
        public override CommandDecimal ValueAtProgress(double progress)
            => Math.Max(0, StartValue + (EndValue - StartValue) * progress);

        public override CommandDecimal Midpoint(in Command<CommandDecimal> endCommand, double progress)
            => StartValue + (endCommand.EndValue - StartValue) * progress;
    }
}