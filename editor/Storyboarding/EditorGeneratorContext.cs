using StorybrewCommon.Mapset;
using StorybrewCommon.Storyboarding;
using StorybrewEditor.Mapset;
using System.Collections.Generic;

namespace StorybrewEditor.Storyboarding
{
    public class EditorGeneratorContext : GeneratorContext
    {
        private Effect effect;
        private EditorBeatmap beatmap;

        public override Beatmap Beatmap => beatmap;

        public List<EditorStoryboardLayer> EditorLayers = new List<EditorStoryboardLayer>();

        public override StoryboardLayer GetLayer(string identifier)
        {
            var layer = EditorLayers.Find(l => l.Identifier == identifier);
            if (layer == null) EditorLayers.Add(layer = new EditorStoryboardLayer(identifier, effect));
            return layer;
        }

        public EditorGeneratorContext(Effect effect, EditorBeatmap beatmap)
        {
            this.effect = effect;
            this.beatmap = beatmap;
        }
    }
}
