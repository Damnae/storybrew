using OpenTK;
using System;

namespace StorybrewCommon.Storyboarding
{
    public abstract class StoryboardLayer : MarshalByRefObject
    {
        private string identifier;
        public string Identifier => identifier;

        public StoryboardLayer(string identifier)
        {
            this.identifier = identifier;
        }

        public abstract OsbSprite CreateSprite(string path, OsbLayer layer, OsbOrigin origin, Vector2 initialPosition);
        public abstract OsbSprite CreateSprite(string path, OsbLayer layer = OsbLayer.Background, OsbOrigin origin = OsbOrigin.Centre);
    }
}
