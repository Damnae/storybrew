using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StorybrewCommon.Storyboarding.Display
{
    public class AnimatedValue<TValue>
        where TValue : CommandValue
    {
        public TValue DefaultValue;

        private List<ITypedCommand<TValue>> commands = new List<ITypedCommand<TValue>>();
        public IEnumerable<ITypedCommand<TValue>> Commands => commands;
        public bool HasCommands => commands.Count > 0;

        public double StartTime => commands.Count > 0 ? commands[0].StartTime : 0;
        public double EndTime => commands.Count > 0 ? commands[commands.Count - 1].EndTime : 0;
        public TValue StartValue => commands.Count > 0 ? commands[0].StartValue : DefaultValue;
        public TValue EndValue => commands.Count > 0 ? commands[commands.Count - 1].EndValue : DefaultValue;

        public AnimatedValue()
        {
        }

        public AnimatedValue(TValue defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public void Add(ITypedCommand<TValue> command)
        {
            if (command.EndTime < command.StartTime)
                Debug.Print($"'{command}' ends before it starts");

            var index = 0;
            while (index < commands.Count && commands[index].StartTime < command.StartTime)
                index++;

            if (index > 0 && command.StartTime < commands[index - 1].EndTime
                || index < commands.Count && commands[index].StartTime < command.EndTime)
                Debug.Print($"'{command}' overlaps existing command '{commands[index]}'");

            commands.Insert(index, command);

            var triggerable = command as TriggerDecorator<TValue>;
            if (triggerable != null) triggerable.OnTimeChanged += triggerable_OnTimeChanged;
        }

        public void Remove(ITypedCommand<TValue> command)
        {
            commands.Remove(command);

            var triggerable = command as TriggerDecorator<TValue>;
            if (triggerable != null) triggerable.OnTimeChanged -= triggerable_OnTimeChanged;
        }

        public bool IsActive(double time)
            => commands.Count > 0 && StartTime <= time && time <= EndTime;

        public TValue ValueAtTime(double time)
        {
            if (commands.Count == 0) return DefaultValue;
            if (time >= EndTime) return EndValue;

            ITypedCommand<TValue> previousCommand = null, candidateCommand = null;
            foreach (var command in commands)
            {
                if (!command.Enabled) continue;
                if (time < command.StartTime)
                {
                    if (candidateCommand != null) return candidateCommand.ValueAtTime(time);
                    if (previousCommand != null) return previousCommand.EndValue;
                    return command.StartValue;
                }
                previousCommand = command;
                if (command.EndTime < time) continue;
                candidateCommand = command;
            }
            if (candidateCommand != null) return candidateCommand.ValueAtTime(time);
            if (previousCommand != null) return previousCommand.EndValue;
            return DefaultValue;
        }

        private void triggerable_OnTimeChanged(object sender, EventArgs e)
        {
            var command = (ITypedCommand<TValue>)sender;
            if (commands.Remove(command))
            {
                var index = 0;
                while (index < commands.Count && commands[index].StartTime < command.StartTime)
                    index++;

                commands.Insert(index, command);
            }
            else throw new InvalidOperationException();
        }
    }
}
