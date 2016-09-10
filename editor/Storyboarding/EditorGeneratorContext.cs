using StorybrewCommon.Mapset;
using StorybrewCommon.Storyboarding;
using StorybrewEditor.Audio;
using StorybrewEditor.Mapset;
using System.Collections.Generic;
using System;
using StorybrewEditor.Util;

namespace StorybrewEditor.Storyboarding
{
    public class EditorGeneratorContext : GeneratorContext
    {
        private Effect effect;
        private MultiFileWatcher watcher;

        private string projectPath;
        public override string ProjectPath => projectPath;

        private string mapsetPath;
        public override string MapsetPath => mapsetPath;

        private EditorBeatmap beatmap;
        public override Beatmap Beatmap => beatmap;

        public List<EditorStoryboardLayer> EditorLayers = new List<EditorStoryboardLayer>();

        public EditorGeneratorContext(Effect effect, string projectPath, string mapsetPath, EditorBeatmap beatmap, MultiFileWatcher watcher)
        {
            this.projectPath = projectPath;
            this.mapsetPath = mapsetPath;
            this.effect = effect;
            this.beatmap = beatmap;
            this.watcher = watcher;
        }

        public override StoryboardLayer GetLayer(string identifier)
        {
            var layer = EditorLayers.Find(l => l.Identifier == identifier);
            if (layer == null) EditorLayers.Add(layer = new EditorStoryboardLayer(identifier, effect));
            return layer;
        }

        public override void AddDependency(string path)
            => watcher.Watch(path);

        #region Audio data

        private FftStream audioStream;
        protected FftStream AudioStream => audioStream ?? (audioStream = new FftStream(effect.Project.AudioPath));

        public override double AudioDuration
            => AudioStream.Duration * 1000;

        public override float[] GetFft(double time)
            => AudioStream.GetFft(time * 0.001);

        #endregion

        public void DisposeResources()
        {
            audioStream?.Dispose();
            audioStream = null;
        }
    }
}
