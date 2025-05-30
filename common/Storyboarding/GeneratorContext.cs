﻿using StorybrewCommon.Mapset;

namespace StorybrewCommon.Storyboarding
{
    public abstract class GeneratorContext
    {
        public abstract string ProjectPath { get; }
        public abstract string ProjectAssetPath { get; }
        public abstract string MapsetPath { get; }

        public abstract void AddDependency(string path);
        public abstract void AppendLog(string message);

        public abstract Beatmap Beatmap { get; }
        public abstract IEnumerable<Beatmap> Beatmaps { get; }
        public abstract StoryboardLayer GetLayer(string identifier);

        public abstract double AudioDuration { get; }
        public abstract float[] GetFft(double time, string path = null, bool splitChannels = false);
        public abstract float GetFftFrequency(string path = null);

        public abstract bool Multithreaded { get; set; }
        public abstract CancellationToken CancellationToken { get; }
    }
}
