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

        public abstract OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, Vector2 initialPosition);
        public abstract OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin = OsbOrigin.Centre);

        public abstract OsbSample CreateSample(string path, double time, double volume = 100);
    }
}
