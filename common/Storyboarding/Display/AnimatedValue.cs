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
        private bool strict;

        private List<ITypedCommand<TValue>> commands = new List<ITypedCommand<TValue>>();
        public IEnumerable<ITypedCommand<TValue>> Commands => commands;
        public bool HasCommands => commands.Count > 0;

        private bool hasOverlap;

        public double StartTime => commands.Count > 0 ? commands[0].StartTime : 0;
        public double EndTime => commands.Count > 0 ? commands[commands.Count - 1].EndTime : 0;
        public double Duration => EndTime - StartTime;
        public TValue StartValue => commands.Count > 0 ? commands[0].StartValue : DefaultValue;
        public TValue EndValue => commands.Count > 0 ? commands[commands.Count - 1].EndValue : DefaultValue;

        public AnimatedValue()
        {
        }

        public AnimatedValue(TValue defaultValue, bool strict = false)
        {
            DefaultValue = defaultValue;
            this.strict = strict;
        }

        public void Add(ITypedCommand<TValue> command)
        {
            var triggerable = command as TriggerDecorator<TValue>;
            if (triggerable == null)
            {
                if (command.EndTime < command.StartTime)
                    Debug.Print($"'{command}' ends before it starts");

                int index;
                findCommandIndex(command.StartTime, out index);
                while (index < commands.Count)
                {
                    if (commands[index].StartTime <= command.StartTime)
                        index++;
                    else break;
                }

                if (index > 0 && command.StartTime < commands[index - 1].EndTime)
                {
                    hasOverlap = true;
                    //Debug.Print($"'{command}' overlaps existing previous command '{commands[index - 1]}'");
                }
                else if (index < commands.Count && commands[index].StartTime < command.EndTime)
                {
                    hasOverlap = true;
                    //Debug.Print($"'{command}' overlaps existing next command '{commands[index]}'");
                }

                commands.Insert(index, command);
            }
            else triggerable.OnStateChanged += triggerable_OnStateChanged;
        }

        public void Remove(ITypedCommand<TValue> command)
        {
            var triggerable = command as TriggerDecorator<TValue>;
            if (triggerable == null)
                commands.Remove(command);
            else triggerable.OnStateChanged -= triggerable_OnStateChanged;
        }

        public bool IsActive(double time)
            => commands.Count > 0 && StartTime <= time && time <= EndTime;

        public TValue ValueAtTime(double time)
        {
            if (commands.Count == 0) return DefaultValue;
            if (!strict && time >= EndTime) return EndValue;

            int index;
            if (!findCommandIndex(time, out index) && index > 0)
                index--;

            if (hasOverlap)
                for (var i = 0; i < index; i++)
                    if (time < commands[i].EndTime)
                    {
                        index = i;
                        break;
                    }

            var command = commands[index];
            if (strict && (time < command.StartTime || time > command.EndTime && command.Duration != 0))
                return DefaultValue;

            return command.ValueAtTime(time);
        }

        private bool findCommandIndex(double time, out int index)
        {
            var left = 0;
            var right = commands.Count - 1;
            while (left <= right)
            {
                index = left + ((right - left) >> 1);
                var commandTime = commands[index].StartTime;
                if (commandTime == time)
                    return true;
                else if (commandTime < time)
                    left = index + 1;
                else right = index - 1;
            }
            index = left;
            return false;
        }

        private void triggerable_OnStateChanged(object sender, EventArgs e)
        {
            var command = (ITypedCommand<TValue>)sender;

            commands.Remove(command);
            if (command.Active)
            {
                int index;
                findCommandIndex(command.StartTime, out index);
                commands.Insert(index, command);
            }
        }
    }
}
