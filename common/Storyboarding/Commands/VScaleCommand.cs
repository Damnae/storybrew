using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class VScaleCommand : Command<CommandScale>
    {
        public override string Identifier => "V"; 

        public VScaleCommand(OsbEasing easing, double startTime, double endTime, in CommandScale startValue, in CommandScale endValue)
            : base(easing, startTime, endTime, startValue, endValue)
        {
        }

        public override CommandScale GetTransformedStartValue(StoryboardTransform transform) => transform.ApplyToScale(StartValue);
        public override CommandScale GetTransformedEndValue(StoryboardTransform transform) => transform.ApplyToScale(EndValue);

        public override CommandScale ValueAtProgress(double progress)
            => StartValue + (EndValue - StartValue) * progress;

        public override CommandScale Midpoint(in Command<CommandScale> endCommand, double progress)
            => new CommandScale(StartValue.X + (endCommand.EndValue.X - StartValue.X) * progress, StartValue.Y + (endCommand.EndValue.Y - StartValue.Y) * progress);

        public override IFragmentableCommand GetFragment(double startTime, double endTime)
        {
            if (IsFragmentable)
            {
                var startValue = ValueAtTime(startTime);
                var endValue = ValueAtTime(endTime);
                return new VScaleCommand(Easing, startTime, endTime, startValue, endValue);
            }
            return this;
        }
    }
}