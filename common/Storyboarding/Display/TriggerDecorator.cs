using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using System;

namespace StorybrewCommon.Storyboarding.Display
{
    public class TriggerDecorator<TValue> : ITypedCommand<TValue>
        where TValue : CommandValue
    {
        private ITypedCommand<TValue> command;
        private double startTime;
        private bool triggered;

        public OsbEasing Easing { get { throw new InvalidOperationException(); } }
        public double StartTime => startTime + command.StartTime;
        public double EndTime => startTime + command.EndTime;
        public TValue StartValue => command.StartValue;
        public TValue EndValue => command.EndValue;
        public bool Enabled => triggered;

        public event EventHandler OnTimeChanged;

        public TriggerDecorator(ITypedCommand<TValue> command)
        {
            this.command = command;
        }

        public void Trigger(double time)
        {
            triggered = true;
            if (startTime != time)
            {
                startTime = time;
                OnTimeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void UnTrigger()
        {
            triggered = false;
        }

        public TValue ValueAtTime(double time)
        {
            if (!triggered) throw new InvalidOperationException("Not triggered");

            var commandTime = time - startTime;
            if (commandTime < command.StartTime) return command.StartValue;
            if (command.EndTime < commandTime) return command.EndValue;
            return command.ValueAtTime(commandTime);
        }

        public string ToOsbString(ExportSettings exportSettings)
        {
            throw new InvalidOperationException();
        }

        public override string ToString() => $"triggerable ({StartTime}s - {EndTime}s active:{triggered})";
    }
}
