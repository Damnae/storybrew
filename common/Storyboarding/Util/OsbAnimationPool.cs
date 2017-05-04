using System;

namespace StorybrewCommon.Storyboarding.Util
{
    public class OsbAnimationPool : OsbSpritePool
    {
        private int frameCount;
        private int frameDelay;
        private OsbLoopType loopType;

        public OsbAnimationPool(StoryboardLayer layer, string path, int frameCount, int frameDelay, OsbLoopType loopType, OsbOrigin origin, Action<OsbSprite, double, double> finalizeSprite = null) : base(layer, path, origin, finalizeSprite)
        {
            this.frameCount = frameCount;
            this.frameDelay = frameDelay;
            this.loopType = loopType;
        }

        public OsbAnimationPool(StoryboardLayer layer, string path, int frameCount, int frameDelay, OsbLoopType loopType, OsbOrigin origin, bool additive) : this(layer, path, frameCount, frameDelay, loopType, origin, additive ? (sprite, startTime, endTime) => sprite.Additive(startTime, endTime) : (Action<OsbSprite, double, double>)null)
        {
        }

        public new OsbSprite Get(double startTime, double endTime)
        {
            var result = (PooledSprite)null;
            foreach (var pooledSprite in pooledSprites)
                if (pooledSprite.EndTime < startTime
                    && startTime < pooledSprite.StartTime + MaxPoolDuration
                    && (result == null || pooledSprite.StartTime < result.StartTime))
                {
                    result = pooledSprite;
                }

            if (result != null)
            {
                result.EndTime = endTime;
                return result.Sprite;
            }

            var sprite = layer.CreateAnimation(path, frameCount, frameDelay, loopType, origin);
            pooledSprites.Add(new PooledSprite(sprite, startTime, endTime));
            return sprite;
        }

    }
}
