using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding.Util
{
    public class OsbSpritePool
    {
        private StoryboardLayer layer;
        private string path;
        private OsbOrigin origin;
        private bool additive;

        private List<PooledSprite> pooledSprites = new List<PooledSprite>();

        public OsbSpritePool(StoryboardLayer layer, string path, OsbOrigin origin, bool additive)
        {
            this.layer = layer;
            this.path = path;
            this.origin = origin;
            this.additive = additive;
        }

        public void Clear()
        {
            if (additive)
                foreach (PooledSprite pooledSprite in pooledSprites)
                {
                    var sprite = pooledSprite.Sprite;
                    sprite.Additive(sprite.CommandsStartTime, (int)pooledSprite.EndTime);
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
                if (pooledSprite.EndTime < startTime && (result == null || pooledSprite.Sprite.CommandsStartTime < result.Sprite.CommandsStartTime))
                    result = pooledSprite;

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
    }
}