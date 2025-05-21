using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Display
{
    public readonly struct CommandResult<TValue>
        where TValue : struct, CommandValue
    {
        private readonly Command<TValue> command;
        private readonly double timeOffset;

        public readonly double StartTime;
        public readonly double EndTime;

        public TValue StartValue => command.StartValue;
        public TValue EndValue => command.EndValue;

        public CommandResult(Command<TValue> command, double timeOffset)
        {
            this.command = command;
            this.timeOffset = timeOffset;

            StartTime = command.StartTime + timeOffset;
            EndTime = command.EndTime + timeOffset;
        }

        public TValue ValueAtTime(double time)
            => command.ValueAtTime(time - timeOffset);

        public override string ToString()
            => command.ToString() + $" with start:{StartTime} end:{EndTime}";
    }
}
