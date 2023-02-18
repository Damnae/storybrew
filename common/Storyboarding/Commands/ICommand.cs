using System;
using System.IO;

namespace StorybrewCommon.Storyboarding.Commands
{
#pragma warning disable CS1591
    public interface ICommand : IComparable<ICommand>
    {
        double StartTime { get; }
        double EndTime { get; }
        bool Active { get; }
        int Cost { get; }

        void WriteOsb(TextWriter writer, ExportSettings exportSettings, int indentation);
    }
}