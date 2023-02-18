using BrewLib.Data;
using BrewLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrewLib.Audio
{
    public class AudioSampleContainer : IDisposable
    {
        readonly AudioManager audioManager;
        readonly ResourceContainer resourceContainer;

        Dictionary<string, AudioSample> samples = new Dictionary<string, AudioSample>();
        public IEnumerable<string> ResourceNames => samples.Where(e => e.Value != null).Select(e => e.Key);

        public AudioSampleContainer(AudioManager audioManager, ResourceContainer resourceContainer = null)
        {
            this.audioManager = audioManager;
            this.resourceContainer = resourceContainer;
        }
        public AudioSample Get(string filename)
        {
            filename = PathHelper.WithStandardSeparators(filename);
            if (!samples.TryGetValue(filename, out AudioSample sample))
            {
                sample = audioManager.LoadSample(filename, resourceContainer);
                samples.Add(filename, sample);
            }
            return sample;
        }

        #region IDisposable Support

        bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var entry in samples) entry.Value?.Dispose();
                    samples.Clear();
                }
                samples = null;
                disposedValue = true;
            }
        }
        public void Dispose() => Dispose(true);

        #endregion
    }
}