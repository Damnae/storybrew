using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using System;

namespace StorybrewCommon.Storyboarding.Display
{
    public class LoopDecorator<TValue> : ITypedCommand<TValue>
        where TValue : CommandValue
    {
        private ITypedCommand<TValue> command;
        private double startTime;
        private double repeatDuration;
        private int repeats;

        public OsbEasing Easing { get { throw new InvalidOperationException(); } }
        public double StartTime => startTime;
        public double EndTime => startTime + RepeatDuration * repeats;
        public TValue StartValue => command.StartValue;
        public TValue EndValue => command.EndValue;
        public bool Enabled => true;

        public double RepeatDuration => repeatDuration < 0 ? command.EndTime : repeatDuration;

        public LoopDecorator(ITypedCommand<TValue> command, double startTime, double repeatDuration, int repeats)
        {
            this.command = command;
            this.startTime = startTime;
            this.repeatDuration = repeatDuration;
            this.repeats = repeats;
        }

        public TValue ValueAtTime(double time)
        {
            if (time < StartTime) return command.StartValue;
            if (EndTime < time) return command.EndValue;

            var repeatDuration = RepeatDuration;
            var repeatTime = time - StartTime;
            while (repeatTime > repeatDuration)
                repeatTime -= repeatDuration;

            if (repeatTime < command.StartTime) return command.StartValue;
            if (command.EndTime < repeatTime) return command.EndValue;
            return command.ValueAtTime(repeatTime);
        }

        public string ToOsbString(ExportSettings exportSettings)
        {
            throw new InvalidOperationException();
        }

        public override string ToString() => $"loop x{repeats} ({StartTime}s - {EndTime}s)";
    }
}
