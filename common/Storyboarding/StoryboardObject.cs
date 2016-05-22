using System;
using System.IO;

namespace StorybrewCommon.Storyboarding
{
    public abstract class StoryboardObject : MarshalByRefObject
    {
        public abstract void WriteOsb(TextWriter writer, ExportSettings exportSettings, OsbLayer layer);
    }
}
