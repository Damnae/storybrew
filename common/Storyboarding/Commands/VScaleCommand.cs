using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class VScaleCommand : Command<CommandScale>
    {
        public VScaleCommand(OsbEasing easing, double startTime, double endTime, CommandScale startValue, CommandScale endValue)
            : base("V", easing, startTime, endTime, startValue, endValue)
        {
        }

        public override CommandScale ValueAtProgress(double progress)
            => StartValue + (EndValue - StartValue) * progress;

        public override CommandScale Midpoint(Command<CommandScale> endCommand, double progress)
            => new CommandScale(StartValue.X + (endCommand.EndValue.X - StartValue.X) * progress, StartValue.Y + (endCommand.EndValue.Y - StartValue.Y) * progress);
    }
}