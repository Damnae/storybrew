using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class ColorCommand : Command<CommandColor>
    {
        public override string Identifier => "C";

        public ColorCommand(OsbEasing easing, double startTime, double endTime, CommandColor startValue, CommandColor endValue)
            : base(easing, startTime, endTime, startValue, endValue)
        {
        }

        public override CommandColor ValueAtProgress(double progress)
            => StartValue + (EndValue - StartValue) * progress;

        public override CommandColor Midpoint(in Command<CommandColor> endCommand, double progress)
            => StartValue + (endCommand.EndValue - StartValue) * progress;
    }
}