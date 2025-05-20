using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class RotateCommand : Command<CommandDecimal>
    {
        public override string Identifier => "R";
        
        public RotateCommand(OsbEasing easing, double startTime, double endTime, in CommandDecimal startValue, in CommandDecimal endValue)
            : base(easing, startTime, endTime, startValue, endValue)
        {
        }

        public override CommandDecimal GetTransformedStartValue(StoryboardTransform transform) => transform.ApplyToRotation(StartValue);
        public override CommandDecimal GetTransformedEndValue(StoryboardTransform transform) => transform.ApplyToRotation(EndValue);

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
                return new RotateCommand(Easing, startTime, endTime, startValue, endValue);
            }
            return this;
        }
    }
}