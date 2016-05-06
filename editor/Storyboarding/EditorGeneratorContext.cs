using StorybrewCommon.Mapset;
using StorybrewCommon.Storyboarding;
using StorybrewEditor.Mapset;
using System.Collections.Generic;
using System;
using StorybrewEditor.Audio;

namespace StorybrewEditor.Storyboarding
{
    public class EditorGeneratorContext : GeneratorContext
    {
        private Effect effect;
        private FftStream stream;

        private EditorBeatmap beatmap;
        public override Beatmap Beatmap => beatmap;

        public List<EditorStoryboardLayer> EditorLayers = new List<EditorStoryboardLayer>();

        public EditorGeneratorContext(Effect effect, EditorBeatmap beatmap)
        {
            this.effect = effect;
            this.beatmap = beatmap;
        }

        public override StoryboardLayer GetLayer(string identifier)
        {
            var layer = EditorLayers.Find(l => l.Identifier == identifier);
            if (layer == null) EditorLayers.Add(layer = new EditorStoryboardLayer(identifier, effect));
            return layer;
        }

        public override float[] GetFft(double time)
        {
            if (stream == null)
                stream = new FftStream(effect.Project.AudioPath);

            return stream.GetFft(time * 0.001);
        }

        public void DisposeResources()
        {
            stream?.Dispose();
            stream = null;
        }
    }
}
