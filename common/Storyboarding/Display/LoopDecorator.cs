using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using System;
using System.IO;

namespace StorybrewCommon.Storyboarding.Display
{
#pragma warning disable CS1591
    public class LoopDecorator<TValue> : ITypedCommand<TValue> where TValue : CommandValue
    {
        readonly ITypedCommand<TValue> command;
        readonly double repeatDuration;
        readonly int repeats;

        public OsbEasing Easing { get { throw new InvalidOperationException(); } }
        public double StartTime { get; }
        public double EndTime => StartTime + RepeatDuration * repeats;
        public double Duration => EndTime - StartTime;
        public TValue StartValue => command.StartValue;
        public TValue EndValue => command.EndValue;
        public bool Active => true;
        public int Cost => throw new InvalidOperationException();

        public double RepeatDuration => repeatDuration < 0 ? command.EndTime : repeatDuration;

        public LoopDecorator(ITypedCommand<TValue> command, double startTime, double repeatDuration, int repeats)
        {
            this.command = command;
            StartTime = startTime;
            this.repeatDuration = repeatDuration;
            this.repeats = repeats;
        }

        public TValue ValueAtTime(double time)
        {
            if (time < StartTime) return command.ValueAtTime(command.EndTime);
            if (EndTime < time) return command.ValueAtTime(command.EndTime);

            var repeatDuration = RepeatDuration;
            var repeatTime = time - StartTime;
            var repeated = false;
            while (repeatTime > repeatDuration)
            {
                repeatTime -= repeatDuration;
                repeated = true;
            }

            if (repeatTime < command.StartTime) if (repeated && repeatTime < command.StartTime) return command.ValueAtTime(command.EndTime);
                else return command.ValueAtTime(command.StartTime);

            if (command.EndTime < repeatTime) return command.ValueAtTime(command.EndTime);
            return command.ValueAtTime(repeatTime);
        }
        public int CompareTo(ICommand other) => CommandComparer.CompareCommands(this, other);

        public void WriteOsb(TextWriter writer, ExportSettings exportSettings, int indentation)
        {
            throw new InvalidOperationException();
        }
        public override string ToString() => $"loop x{repeats} ({StartTime}s - {EndTime}s)";
    }
}