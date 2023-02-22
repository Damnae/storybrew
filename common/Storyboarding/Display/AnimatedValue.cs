using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StorybrewCommon.Storyboarding.Display
{
#pragma warning disable CS1591
    public class AnimatedValue<TValue> where TValue : CommandValue
    {
        public TValue DefaultValue;

        readonly List<ITypedCommand<TValue>> commands = new List<ITypedCommand<TValue>>();
        public IEnumerable<ITypedCommand<TValue>> Commands => commands;

        public bool HasCommands => commands.Count > 0;
        public bool HasOverlap { get; private set; }

        public double StartTime => commands.Count > 0 ? commands[0].StartTime : 0;
        public double EndTime => commands.Count > 0 ? commands[commands.Count - 1].EndTime : 0;
        public double Duration => EndTime - StartTime;
        public TValue StartValue => commands.Count > 0 ? commands[0].StartValue : DefaultValue;
        public TValue EndValue => commands.Count > 0 ? commands[commands.Count - 1].EndValue : DefaultValue;

        public AnimatedValue() { }
        public AnimatedValue(TValue defaultValue) => DefaultValue = defaultValue;

        public void Add(ITypedCommand<TValue> command)
        {
            var triggerable = command as TriggerDecorator<TValue>;
            if (triggerable == null)
            {
                if (command.EndTime < command.StartTime) Debug.Print($"'{command}' ends before it starts");

                findCommandIndex(command.StartTime, out int index);
                while (index < commands.Count)
                {
                    if (commands[index].CompareTo(command) < 0) index++;
                    else break;
                }

                HasOverlap |=
                    (index > 0 && (int)Math.Round(command.StartTime) < (int)Math.Round(commands[index - 1].EndTime)) ||
                    (index < commands.Count && (int)Math.Round(commands[index].StartTime) < (int)Math.Round(command.EndTime));

                commands.Insert(index, command);
            }
            else triggerable.OnStateChanged += triggerable_OnStateChanged;
        }
        public void Remove(ITypedCommand<TValue> command)
        {
            var triggerable = command as TriggerDecorator<TValue>;
            if (triggerable == null) commands.Remove(command);
            else triggerable.OnStateChanged -= triggerable_OnStateChanged;
        }

        public bool IsActive(double time) => commands.Count > 0 && StartTime <= time && time <= EndTime;
        public TValue ValueAtTime(double time)
        {
            if (commands.Count == 0) return DefaultValue;

            if (!findCommandIndex(time, out int index) && index > 0) index--;
            if (HasOverlap) for (var i = 0; i < index; i++) if (time < commands[i].EndTime)
            {
                index = i;
                break;
            }

            var command = commands[index];
            return command.ValueAtTime(time);
        }
        bool findCommandIndex(double time, out int index)
        {
            var left = 0;
            var right = commands.Count - 1;
            while (left <= right)
            {
                index = left + ((right - left) >> 1);
                var commandTime = commands[index].StartTime;
                if (commandTime == time) return true;
                else if (commandTime < time) left = index + 1;
                else right = index - 1;
            }
            index = left;
            return false;
        }
        void triggerable_OnStateChanged(object sender, EventArgs e)
        {
            var command = (ITypedCommand<TValue>)sender;

            commands.Remove(command);
            if (command.Active)
            {
                findCommandIndex(command.StartTime, out int index);
                commands.Insert(index, command);
            }
        }
    }
}