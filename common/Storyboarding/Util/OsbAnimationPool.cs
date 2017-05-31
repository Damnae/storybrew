using System;

namespace StorybrewCommon.Storyboarding.Util
{
    public class OsbAnimationPool : OsbSpritePool
    {
        private int frameCount;
        private double frameDelay;
        private OsbLoopType loopType;

        public OsbAnimationPool(StoryboardLayer layer, string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, Action<OsbSprite, double, double> finalizeSprite = null)
            : base(layer, path, origin, finalizeSprite)
        {
            this.frameCount = frameCount;
            this.frameDelay = frameDelay;
            this.loopType = loopType;
        }

        public OsbAnimationPool(StoryboardLayer layer, string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, bool additive)
            : this(layer, path, frameCount, frameDelay, loopType, origin, additive ? (sprite, startTime, endTime) => sprite.Additive(startTime, endTime) : (Action<OsbSprite, double, double>)null)
        {
        }

        protected override OsbSprite CreateSprite(StoryboardLayer layer, string path, OsbOrigin origin)
            => layer.CreateAnimation(path, frameCount, frameDelay, loopType, origin);
    }
}
