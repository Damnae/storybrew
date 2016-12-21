using System;
using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding.Util
{
    public class OsbSpritePools : IDisposable
    {
        private StoryboardLayer layer;
        private Dictionary<string, OsbSpritePool> pools = new Dictionary<string, OsbSpritePool>();

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
            pools.Clear();
        }

        public OsbSprite Get(double startTime, double endTime, string path, OsbOrigin origin = OsbOrigin.Centre, Action<OsbSprite, double, double> finalizeSprite = null, int poolGroup = 0)
            => getPool(path, origin, finalizeSprite, poolGroup).Get(startTime, endTime);

        public OsbSprite Get(double startTime, double endTime, string path, OsbOrigin origin, bool additive, int poolGroup = 0)
            => Get(startTime, endTime, path, origin, additive ? (sprite, spriteStartTime, spriteEndTime) => sprite.Additive(spriteStartTime, spriteEndTime) : (Action<OsbSprite, double, double>)null, poolGroup);

        private OsbSpritePool getPool(string path, OsbOrigin origin, Action<OsbSprite, double, double> finalizeSprite, int poolGroup)
        {
            var key = getKey(path, origin, finalizeSprite, poolGroup);

            OsbSpritePool pool;
            if (!pools.TryGetValue(key, out pool))
                pools.Add(key, pool = new OsbSpritePool(layer, path, origin, finalizeSprite) { MaxPoolDuration = maxPoolDuration, });

            return pool;
        }

        private string getKey(string path, OsbOrigin origin, Action<OsbSprite, double, double> action, int poolGroup)
            => $"{path}#{origin}#{action?.Target}.{action?.Method.Name}#{poolGroup}";

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