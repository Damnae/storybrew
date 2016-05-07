
using System.IO;

namespace StorybrewCommon.Storyboarding.Commands
{
    public interface ICommand
    {
        double StartTime { get; }
        double EndTime { get; }
        bool Enabled { get; }

        void WriteOsb(TextWriter writer, ExportSettings exportSettings, int indentation);
    }
}