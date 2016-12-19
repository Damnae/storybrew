using System;
using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding.Util
{
    public class OsbSpritePool : IDisposable
    {
        private StoryboardLayer layer;
        private string path;
        private OsbOrigin origin;
        private Action<OsbSprite, double, double> finalizeSprite;

        private List<PooledSprite> pooledSprites = new List<PooledSprite>();

        public OsbSpritePool(StoryboardLayer layer, string path, OsbOrigin origin, bool additive)
        {
            this.layer = layer;
            this.path = path;
            this.origin = origin;

            finalizeSprite = (sprite, startTime, endTime) => sprite.Additive(startTime, endTime);
        }

        public OsbSpritePool(StoryboardLayer layer, string path, OsbOrigin origin, Action<OsbSprite, double, double> finalizeSprite = null)
        {
            this.layer = layer;
            this.path = path;
            this.origin = origin;
            this.finalizeSprite = finalizeSprite;
        }

        public void Clear()
        {
            if (finalizeSprite != null)
                foreach (var pooledSprite in pooledSprites)
                {
                    var sprite = pooledSprite.Sprite;
                    finalizeSprite(sprite, sprite.CommandsStartTime, pooledSprite.EndTime);
                }

            pooledSprites.Clear();
        }

        public OsbSprite Get(double startTime, double endTime)
        {
            var sprite = Get(startTime);
            Release(sprite, endTime);
            return sprite;
        }

        public OsbSprite Get(double startTime)
        {
            var result = (PooledSprite)null;
            foreach (var pooledSprite in pooledSprites)
                if (pooledSprite.EndTime < startTime
                    && (result == null || pooledSprite.Sprite.CommandsStartTime < result.Sprite.CommandsStartTime))
                {
                    result = pooledSprite;
                }

            if (result != null)
            {
                pooledSprites.Remove(result);
                return result.Sprite;
            }

            return layer.CreateSprite(path, origin);
        }

        public void Release(OsbSprite sprite, double endTime)
            => pooledSprites.Add(new PooledSprite(sprite, endTime));

        private class PooledSprite
        {
            public OsbSprite Sprite;
            public double EndTime;

            public PooledSprite(OsbSprite sprite, double endTime)
            {
                Sprite = sprite;
                EndTime = endTime;
            }
        }

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