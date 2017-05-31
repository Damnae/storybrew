using System;
using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding.Util
{
    public class OsbSpritePools : IDisposable
    {
        private StoryboardLayer layer;
        private Dictionary<string, OsbSpritePool> pools = new Dictionary<string, OsbSpritePool>();
        private Dictionary<string, OsbAnimationPool> animationPools = new Dictionary<string, OsbAnimationPool>();

        private int maxPoolDuration = 60000;
        public int MaxPoolDuration
        {
            get { return maxPoolDuration; }
            set
            {
                if (maxPoolDuration == value) return;
                maxPoolDuration = value;
                foreach (var pool in pools.Values)
                    pool.MaxPoolDuration = maxPoolDuration;
            }
        }

        public OsbSpritePools(StoryboardLayer layer)
        {
            this.layer = layer;
        }

        public void Clear()
        {
            foreach (var pool in pools)
                pool.Value.Clear();
            foreach (var pool in animationPools)
                pool.Value.Clear();
            pools.Clear();
            animationPools.Clear();
        }

        public OsbSprite Get(double startTime, double endTime, string path, OsbOrigin origin = OsbOrigin.Centre, Action<OsbSprite, double, double> finalizeSprite = null, int poolGroup = 0)
            => getPool(path, origin, finalizeSprite, poolGroup).Get(startTime, endTime);

        public OsbSprite Get(double startTime, double endTime, string path, OsbOrigin origin, bool additive, int poolGroup = 0)
            => Get(startTime, endTime, path, origin, additive ? (sprite, spriteStartTime, spriteEndTime) => sprite.Additive(spriteStartTime, spriteEndTime) : (Action<OsbSprite, double, double>)null, poolGroup);

        public OsbAnimation Get(double startTime, double endTime, string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin = OsbOrigin.Centre, Action<OsbSprite, double, double> finalizeSprite = null, int poolGroup = 0)
            => (OsbAnimation)getPool(path, frameCount, frameDelay, loopType, origin, finalizeSprite, poolGroup).Get(startTime, endTime);

        public OsbAnimation Get(double startTime, double endTime, string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, bool additive, int poolGroup = 0)
            => Get(startTime, endTime, path, frameCount, frameDelay, loopType, origin, additive ? (sprite, spriteStartTime, spriteEndTime) => sprite.Additive(spriteStartTime, spriteEndTime) : (Action<OsbSprite, double, double>)null, poolGroup);

        private OsbSpritePool getPool(string path, OsbOrigin origin, Action<OsbSprite, double, double> finalizeSprite, int poolGroup)
        {
            var key = getKey(path, origin, finalizeSprite, poolGroup);

            OsbSpritePool pool;
            if (!pools.TryGetValue(key, out pool))
                pools.Add(key, pool = new OsbSpritePool(layer, path, origin, finalizeSprite) { MaxPoolDuration = maxPoolDuration, });

            return pool;
        }

        private OsbAnimationPool getPool(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, Action<OsbSprite, double, double> finalizeSprite, int poolGroup)
        {
            var key = getKey(path, frameCount, frameDelay, loopType, origin, finalizeSprite, poolGroup);

            OsbAnimationPool pool;
            if (!animationPools.TryGetValue(key, out pool))
                animationPools.Add(key, pool = new OsbAnimationPool(layer, path, frameCount, frameDelay, loopType, origin, finalizeSprite) { MaxPoolDuration = maxPoolDuration, });

            return pool;
        }

        private string getKey(string path, OsbOrigin origin, Action<OsbSprite, double, double> action, int poolGroup)
            => $"{path}#{origin}#{action?.Target}.{action?.Method.Name}#{poolGroup}";

        private string getKey(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, Action<OsbSprite, double, double> action, int poolGroup)
            => $"{path}#{frameCount}#{frameDelay}#{loopType}#{origin}#{action?.Target}.{action?.Method.Name}#{poolGroup}";

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Clear();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}