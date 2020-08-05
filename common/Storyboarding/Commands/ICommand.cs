using System;
using System.IO;

namespace StorybrewCommon.Storyboarding.Commands
{
    public interface ICommand : IComparable<ICommand>
    {
        double StartTime { get; }
        double EndTime { get; }
        bool Active { get; }
        int Cost { get; }

        void WriteOsb(TextWriter writer, ExportSettings exportSettings, int indentation);
    }
}