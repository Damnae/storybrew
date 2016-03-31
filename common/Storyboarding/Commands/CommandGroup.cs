using System;
using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding.Commands
{
    public abstract class CommandGroup : MarshalByRefObject, ICommand
    {
        public double StartTime { get; set; }
        public virtual double EndTime { get; set; }
        public bool Enabled => true;

        private List<ICommand> commands = new List<ICommand>();
        public IEnumerable<ICommand> Commands => commands;

        public double CommandsStartTime
        {
            get
            {
                var commandsStartTime = double.MaxValue;
                foreach (ICommand command in Commands)
                    commandsStartTime = Math.Min(commandsStartTime, command.EndTime);

                return commandsStartTime;
            }
        }

        public double CommandsEndTime
        {
            get
            {
                var commandsEndTime = double.MinValue;
                foreach (ICommand command in Commands)
                    commandsEndTime = Math.Max(commandsEndTime, command.EndTime);

                return commandsEndTime;
            }
        }

        public double CommandsDuration
        {
            get
            {
                var commandsStartTime = double.MaxValue;
                var commandsEndTime = double.MinValue;
                foreach (ICommand command in Commands)
                    commandsStartTime = Math.Min(commandsStartTime, command.StartTime);
                foreach (ICommand command in Commands)
                    commandsEndTime = Math.Max(commandsEndTime, command.EndTime);
                return commandsEndTime - commandsStartTime;
            }
        }

        public void Add(ICommand command)
            => commands.Add(command);

        public string ToOsbString(ExportSettings exportSettings)
        {
            if (commands.Count <= 0)
                return string.Empty;

            var lines = new string[commands.Count + 1];
            lines[0] = GetCommandGroupHeader(exportSettings);
            for (int i = 0; i < commands.Count; i++)
                lines[i + 1] = " " + commands[i].ToOsbString(exportSettings);

            return string.Join("\n ", lines);
        }

        protected abstract string GetCommandGroupHeader(ExportSettings exportSettings);

        public override string ToString()
            => $"{GetCommandGroupHeader(ExportSettings.Default)} ({commands.Count} commands)";
    }
}
