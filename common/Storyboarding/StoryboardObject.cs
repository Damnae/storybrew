using System;
using System.IO;

namespace StorybrewCommon.Storyboarding
{
    public abstract class StoryboardObject : MarshalByRefObject
    {
        public OsbLayer Layer { get; set; } = OsbLayer.Background;

        public abstract void WriteOsb(TextWriter writer, ExportSettings exportSettings);
    }
}
