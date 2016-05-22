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

        public abstract OsbSprite CreateSprite(string path, OsbOrigin origin, Vector2 initialPosition);
        public abstract OsbSprite CreateSprite(string path, OsbOrigin origin = OsbOrigin.Centre);
        [Obsolete("OsbLayer comes from the storyboard layer and is ignored by this method")]
        public abstract OsbSprite CreateSprite(string path, OsbLayer layer = OsbLayer.Background, OsbOrigin origin = OsbOrigin.Centre);

        public abstract OsbAnimation CreateAnimation(string path, int frameCount, int frameDelay, OsbLoopType loopType, OsbOrigin origin, Vector2 initialPosition);
        public abstract OsbAnimation CreateAnimation(string path, int frameCount, int frameDelay, OsbLoopType loopType, OsbOrigin origin = OsbOrigin.Centre);
    }
}
