using System;
using System.IO;

namespace StorybrewCommon.Storyboarding
{
    public abstract class StoryboardObject : MarshalByRefObject
    {
        public abstract double StartTime { get; }
        public abstract double EndTime { get; }
        public abstract int CommandCost { get; }

        public abstract void WriteOsb(TextWriter writer, ExportSettings exportSettings, OsbLayer layer);
    }
}
