using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using System.Diagnostics;

namespace StorybrewCommon.Storyboarding.Display
{
    public class CommandChannel<TValue>
        where TValue : struct, CommandValue
    {
        private readonly List<ITypedCommand<TValue>> commands = [];
        public IReadOnlyList<ITypedCommand<TValue>> Commands => commands;
        public bool HasOverlap { get; private set; }

        /// <summary>
        /// The command that takes effect before this channel starts
        /// </summary>
        public ITypedCommand<TValue> StartCommand => commands.Count > 0 ? commands[0] : null;
        
        /// <summary>
        /// The command that takes effect after this channel ends
        /// </summary>
        public ITypedCommand<TValue> EndCommand { get; private set; }

        public void Add(ITypedCommand<TValue> command)
        {
            if (command.EndTime < command.StartTime)
                Debug.Print($"'{command}' ends before it starts");

            findCommandIndex(command.StartTime, out int index);
            while (index < commands.Count)
            {
                if (commands[index].CompareTo(command) < 0)
                    index++;
                else break;
            }

            HasOverlap |=
                (index > 0 && (int)Math.Round(command.StartTime) < (int)Math.Round(commands[index - 1].EndTime)) ||
                (index < commands.Count && (int)Math.Round(commands[index].StartTime) < (int)Math.Round(command.EndTime));

            commands.Insert(index, command);

            // Because of possible overlap, the end command is not always the last command in the list
            if (EndCommand == null || EndCommand.EndTime <= command.EndTime)
                EndCommand = command;
        }

        public ITypedCommand<TValue> CommandAtTime(double time)
        {
            if (commands.Count == 0)
                return null;

            if (!findCommandIndex(time, out int index) && index > 0)
                index--;

            // In case of overlapping commands, the command that started first has priority
            if (HasOverlap)
                for (var i = 0; i < index; i++)
                    if (time < commands[i].EndTime)
                    {
                        index = i;
                        break;
                    }

            return commands[index];
        }

        public virtual bool ResultAtTime(double time, out CommandResult<TValue> result)
        {
            var command = CommandAtTime(time);
            if (command == null)
            {
                result = default;
                return false;
            }

            result = command.AsResult();
            return true;
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
    }
}
