using StorybrewCommon.Storyboarding;
using System.Collections.Generic;

namespace StorybrewEditor.Storyboarding
{
    public class EditorGeneratorContext : GeneratorContext
    {
        private Effect effect;

        public List<EditorStoryboardLayer> EditorLayers = new List<EditorStoryboardLayer>();

        public override StoryboardLayer GetLayer(string identifier)
        {
            var layer = EditorLayers.Find(l => l.Identifier == identifier);
            if (layer == null) EditorLayers.Add(layer = new EditorStoryboardLayer(identifier, effect));
            return layer;
        }

        public EditorGeneratorContext(Effect effect)
        {
            this.effect = effect;
        }
    }
}
