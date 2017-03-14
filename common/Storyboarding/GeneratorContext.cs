using System;
using StorybrewCommon.Mapset;
using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding
{
    public abstract class GeneratorContext : MarshalByRefObject
    {
        public abstract string ProjectPath { get; }
        public abstract string MapsetPath { get; }

        public abstract void AddDependency(string path);
        public abstract void AppendLog(string message);

        public abstract Beatmap Beatmap { get; }
        public abstract IEnumerable<Beatmap> Beatmaps { get; }
        public abstract StoryboardLayer GetLayer(string identifier);

        public abstract double AudioDuration { get; }

        public abstract float[] GetFft(double time, string path = null);
    }
}
