using System;
using StorybrewCommon.Mapset;

namespace StorybrewCommon.Storyboarding
{
    public abstract class GeneratorContext : MarshalByRefObject
    {
        public abstract Beatmap Beatmap { get; }
        public abstract StoryboardLayer GetLayer(string identifier);

        public abstract double AudioDuration { get; }
        public abstract float[] GetFft(double time);
    }
}
