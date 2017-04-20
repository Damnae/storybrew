using System;
using System.Collections.Generic;
using System.IO;

namespace StorybrewCommon.Storyboarding.Commands
{
    public abstract class CommandGroup : MarshalByRefObject, ICommand
    {
        private bool ended;

        public double StartTime { get; set; }
        public virtual double EndTime { get; set; }
        public virtual bool Active => true;

        private List<ICommand> commands = new List<ICommand>();
        public IEnumerable<ICommand> Commands => commands;

        public double CommandsStartTime
        {
            get
            {
                var commandsStartTime = double.MaxValue;
                foreach (ICommand command in Commands)
                    commandsStartTime = Math.Min(commandsStartTime, command.StartTime);

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
                {
                    commandsStartTime = Math.Min(commandsStartTime, command.StartTime);
                    commandsEndTime = Math.Max(commandsEndTime, command.EndTime);
                }
                return commandsEndTime - commandsStartTime;
            }
        }

        public void Add(ICommand command)
        {
            if (ended) throw new InvalidOperationException("Cannot add commands to a group after it ended");
            commands.Add(command);
        }

        public virtual void EndGroup()
        {
            ended = true;
        }

        public void WriteOsb(TextWriter writer, ExportSettings exportSettings, int indentation)
        {
            if (commands.Count <= 0)
                return;

            var lines = new string[commands.Count + 1];
            writer.WriteLine(new string(' ', indentation) + GetCommandGroupHeader(exportSettings));
            foreach (var command in commands)
                command.WriteOsb(writer, exportSettings, indentation + 1);
        }

        protected abstract string GetCommandGroupHeader(ExportSettings exportSettings);

        public override string ToString()
            => $"{GetCommandGroupHeader(ExportSettings.Default)} ({commands.Count} commands)";
    }
}
