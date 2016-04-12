using System;
using StorybrewCommon.Mapset;

namespace StorybrewCommon.Storyboarding
{
    public abstract class GeneratorContext : MarshalByRefObject
    {
        public virtual Beatmap Beatmap { get; }

        public abstract StoryboardLayer GetLayer(string identifier);
    }
}
