using OpenTK;
using System;

namespace StorybrewCommon.Storyboarding
{
    public abstract class StoryboardLayer : MarshalByRefObject
    {
        public string Identifier { get; }

        public StoryboardLayer(string identifier)
        {
            Identifier = identifier;
        }

        public abstract OsbSprite CreateSprite(string path, OsbOrigin origin, Vector2 initialPosition);
        public abstract OsbSprite CreateSprite(string path, OsbOrigin origin = OsbOrigin.Centre);

        public abstract OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, Vector2 initialPosition);
        public abstract OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin = OsbOrigin.Centre);

        public abstract OsbSample CreateSample(string path, double time, double volume = 100);
    }
}
