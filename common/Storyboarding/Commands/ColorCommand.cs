using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class ColorCommand : Command<CommandColor>
    {
        public ColorCommand(OsbEasing easing, double startTime, double endTime, CommandColor startValue, CommandColor endValue)
            : base("C", easing, startTime, endTime, startValue, endValue)
        {
        }

        public override CommandColor ValueAtProgress(double progress)
            => StartValue + (EndValue - StartValue) * progress;

        public override CommandColor Midpoint(Command<CommandColor> endCommand, double progress)
            => StartValue + (endCommand.EndValue - StartValue) * progress;
    }
}