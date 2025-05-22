using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Display
{
    public class CommandChannel<TValue>
        where TValue : struct, CommandValue
    {
        private readonly List<ITypedCommand<TValue>> commands = [];
        public IReadOnlyList<ITypedCommand<TValue>> Commands => commands;
        public bool HasOverlap { get; private set; }

        /// <summary>
        /// The command that takes effect before this channel starts;
        /// Because of loops and triggers, StartResult is more useful
        /// </summary>
        public ITypedCommand<TValue> StartCommand => commands.Count > 0 ? commands[0] : null;

        /// <summary>
        /// The command that takes effect after this channel ends;
        /// Because of loops and triggers, EndResult is more useful
        /// </summary>
        public ITypedCommand<TValue> EndCommand => commands.Count > 0 ? commands[^1] : null;

        public virtual CommandResult<TValue> StartResult => StartCommand.AsResult();
        public virtual CommandResult<TValue> EndResult => EndCommand.AsResult();

        public void Add(ITypedCommand<TValue> command)
        {
            if (command.EndTime < command.StartTime)
                throw new InvalidDataException($"'{command}' ends before it starts");

            findCommandIndex(command.StartTime, out int index);
            while (index < commands.Count)
            {
                if (commands[index].CompareTo(command) > 0)
                    break;

                index++;
            }

            HasOverlap |=
                (index > 0 && (int)Math.Round(command.StartTime) < (int)Math.Round(commands[index - 1].EndTime)) ||
                (index < commands.Count && (int)Math.Round(commands[index].StartTime) < (int)Math.Round(command.EndTime));

            commands.Insert(index, command);
        }

        public ITypedCommand<TValue> CommandAtTime(double time)
        {
            if (commands.Count == 0)
                return null;

            if (!findCommandIndex(time, out int index) && index > 0)
                index--;

            if (HasOverlap)
            {
                // The earliest command that started before this one and hasn't ended yet has priority
                for (var i = 0; i < index; i++)
                    if (commands[i].StartTime <= commands[index].StartTime && time <= commands[i].EndTime)
                    {
                        index = i;
                        break;
                    }
            }
            else if (index > 0 && (int)Math.Round(time) == (int)Math.Round(commands[index - 1].EndTime))
            {
                // Even with no overlap, the previous command might still be active
                // due to its end time being the same as this commands' start time
                index--;
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
