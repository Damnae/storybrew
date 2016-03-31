using System;

namespace StorybrewCommon.Storyboarding
{
    public abstract class StoryboardObject : MarshalByRefObject
    {
        public OsbLayer Layer { get; set; } = OsbLayer.Background;

        public abstract string ToOsbString(ExportSettings exportSettings);
    }
}
