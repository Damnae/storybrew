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

        public OsbSprite Get(double startTime, double endTime, string path, OsbLayer osbLayer, OsbOrigin origin = OsbOrigin.Centre, bool additive = false, int poolGroup = 0)
            => getPool(path, osbLayer, origin, additive, poolGroup).Get(startTime, endTime);

        public void Release(OsbSprite sprite, double endTime)
            => getPool(sprite.TexturePath, sprite.Layer, sprite.Origin, false, 0).Release(sprite, endTime);

        private OsbSpritePool getPool(string path, OsbLayer osbLayer, OsbOrigin origin, bool additive, int poolGroup)
        {
            string key = getKey(path, osbLayer, origin, additive, poolGroup);

            OsbSpritePool pool;
            if (!pools.TryGetValue(key, out pool))
                pools.Add(key, pool = new OsbSpritePool(layer, path, osbLayer, origin, additive));

            return pool;
        }

        private string getKey(string path, OsbLayer layer, OsbOrigin origin, bool additive, int poolGroup)
            => $"{path}#{layer}#{origin}#{(additive ? "1" : "0")}#{poolGroup}";
    }
}