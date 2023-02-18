using System;

namespace StorybrewCommon.Storyboarding.Util
{
#pragma warning disable CS1591
    [Obsolete("Use StorybrewCommon.Storyboarding.AnimationPool instead for better support.")]
    public class OsbAnimationPool : OsbSpritePool
    {
        readonly int frameCount;
        readonly double frameDelay;
        readonly OsbLoopType loopType;

        public OsbAnimationPool(StoryboardSegment segment, string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, Action<OsbSprite, double, double> finalizeSprite = null)
            : base(segment, path, origin, finalizeSprite)
        {
            this.frameCount = frameCount;
            this.frameDelay = frameDelay;
            this.loopType = loopType;
        }

        public OsbAnimationPool(StoryboardSegment segment, string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, bool additive)
            : this(segment, path, frameCount, frameDelay, loopType, origin, additive ?
            (pA, sT, eT) => pA.Additive(sT) : (Action<OsbSprite, double, double>)null)
        { }

        protected override OsbSprite CreateSprite(StoryboardSegment segment, string path, OsbOrigin origin)
            => segment.CreateAnimation(path, frameCount, frameDelay, loopType, origin);
    }
}
