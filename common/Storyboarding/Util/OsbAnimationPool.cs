namespace StorybrewCommon.Storyboarding.Util
{
    public class OsbAnimationPool : OsbSpritePool
    {
        private readonly int frameCount;
        private readonly double frameDelay;
        private readonly OsbLoopType loopType;

        public OsbAnimationPool(StoryboardSegment segment, string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, Action<OsbSprite, double, double> finalizeSprite = null)
            : base(segment, path, origin, finalizeSprite)
        {
            this.frameCount = frameCount;
            this.frameDelay = frameDelay;
            this.loopType = loopType;
        }

        public OsbAnimationPool(StoryboardSegment segment, string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, bool additive)
            : this(segment, path, frameCount, frameDelay, loopType, origin, additive ? (sprite, startTime, endTime) => sprite.Additive(startTime, endTime) : (Action<OsbSprite, double, double>)null)
        {
        }

        protected override OsbSprite CreateSprite(StoryboardSegment segment, string path, OsbOrigin origin)
            => segment.CreateAnimation(path, frameCount, frameDelay, loopType, origin);
    }
}
