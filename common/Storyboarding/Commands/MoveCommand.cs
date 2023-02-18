using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
#pragma warning disable CS1591
    public class MoveCommand : Command<CommandPosition>
    {
        public MoveCommand(OsbEasing easing, double startTime, double endTime, CommandPosition startValue, CommandPosition endValue)
            : base("M", easing, startTime, endTime, startValue, endValue) { }

        public override CommandPosition ValueAtProgress(double progress) => StartValue + (EndValue - StartValue) * progress;
        public override CommandPosition Midpoint(Command<CommandPosition> endCommand, double progress) => new CommandPosition(StartValue.X + (endCommand.EndValue.X - StartValue.X) * progress, StartValue.Y + (endCommand.EndValue.Y - StartValue.Y) * progress);

        public override IFragmentableCommand GetFragment(double startTime, double endTime)
        {
            if (IsFragmentable)
            {
                var startValue = ValueAtTime(startTime);
                var endValue = ValueAtTime(endTime);
                return new MoveCommand(Easing, startTime, endTime, startValue, endValue);
            }
            return this;
        }
    }
}