using OpenTK;

namespace StorybrewCommon.Storyboarding
{
    public abstract class StoryboardSegment : StoryboardObject
    {
        public abstract string Identifier { get; }

        public abstract Vector2 Origin { get; set; }
        public abstract Vector2 Position { get; set; }
        public abstract double Rotation { get; set; }
        public double RotationDegrees
        {
            get => MathHelper.RadiansToDegrees(Rotation);
            set => Rotation = MathHelper.DegreesToRadians(value);
        }
        public abstract double Scale { get; set; }

        public abstract bool ReverseDepth { get; set; }

        public abstract IEnumerable<StoryboardSegment> NamedSegments { get; }
        public abstract StoryboardSegment CreateSegment(string identifier = null);
        public abstract StoryboardSegment GetSegment(string identifier);

        public abstract OsbSprite CreateSprite(string path, OsbOrigin origin, Vector2 initialPosition);
        public abstract OsbSprite CreateSprite(string path, OsbOrigin origin = OsbOrigin.Centre);

        public abstract OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, Vector2 initialPosition);
        public abstract OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin = OsbOrigin.Centre);

        public abstract OsbSample CreateSample(string path, double time, double volume = 100);

        public abstract void Discard(StoryboardObject storyboardObject);
    }
}
