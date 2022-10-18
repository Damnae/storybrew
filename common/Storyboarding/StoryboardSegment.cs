using OpenTK;

namespace StorybrewCommon.Storyboarding
{
    public abstract class StoryboardSegment : StoryboardObject
    {
        public abstract bool ReverseDepth { get; set; }

        public abstract StoryboardSegment CreateSegment();

        public abstract OsbSprite CreateSprite(string path, OsbOrigin origin, Vector2 initialPosition);
        public abstract OsbSprite CreateSprite(string path, OsbOrigin origin = OsbOrigin.Centre);

        public abstract OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, Vector2 initialPosition);
        public abstract OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin = OsbOrigin.Centre);

        public abstract OsbSample CreateSample(string path, double time, double volume = 100);

        public abstract void Discard(StoryboardObject storyboardObject);
    }
}
