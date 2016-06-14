using System;
using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding.Util
{
    public class OsbSpritePools
    {
        private Dictionary<string, OsbSpritePool> pools = new Dictionary<string, OsbSpritePool>();
        private StoryboardLayer layer;

        public OsbSpritePools(StoryboardLayer layer)
        {
            this.layer = layer;
        }

        public void Clear()
        {
            foreach (var pool in pools)
                pool.Value.Clear();
            pools.Clear();
        }

        [Obsolete("OsbLayer comes from the storyboard layer and is ignored by this method")]
        public OsbSprite Get(double startTime, double endTime, string path, OsbLayer layer, OsbOrigin origin = OsbOrigin.Centre, bool additive = false, int poolGroup = 0)
            => Get(startTime, endTime, path, origin, additive, poolGroup);

        public OsbSprite Get(double startTime, double endTime, string path, OsbOrigin origin = OsbOrigin.Centre, bool additive = false, int poolGroup = 0)
            => getPool(path, origin, additive, poolGroup).Get(startTime, endTime);

        public void Release(OsbSprite sprite, double endTime)
            => getPool(sprite.TexturePath, sprite.Origin, false, 0).Release(sprite, endTime);

        private OsbSpritePool getPool(string path, OsbOrigin origin, bool additive, int poolGroup)
        {
            string key = getKey(path, origin, additive, poolGroup);

            OsbSpritePool pool;
            if (!pools.TryGetValue(key, out pool))
                pools.Add(key, pool = new OsbSpritePool(layer, path, origin, additive));

            return pool;
        }

        private string getKey(string path, OsbOrigin origin, bool additive, int poolGroup)
            => $"{path}#{origin}#{(additive ? "1" : "0")}#{poolGroup}";
    }
}