using System;
using System.Collections.Generic;
using System.Resources;

namespace BrewLib.Audio
{
    public class AudioContainer : IDisposable
    {
        private AudioManager audioManager;
        private ResourceManager resourceManager;

        private Dictionary<string, AudioSample> samples = new Dictionary<string, AudioSample>();

        public AudioContainer(AudioManager audioManager, ResourceManager resourceManager = null)
        {
            this.audioManager = audioManager;
            this.resourceManager = resourceManager;
        }

        public AudioSample Get(string filename)
        {
            AudioSample sample;
            if (!samples.TryGetValue(filename, out sample))
            {
                sample = audioManager.LoadSample(filename, resourceManager);
                samples.Add(filename, sample);
            }
            return sample;
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var entry in samples)
                        entry.Value?.Dispose();
                    samples.Clear();
                }
                samples = null;
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
