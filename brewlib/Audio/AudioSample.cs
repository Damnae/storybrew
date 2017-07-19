using ManagedBass;
using System;
using System.Diagnostics;
using System.Resources;

namespace BrewLib.Audio
{
    public class AudioSample
    {
        private const int MaxSimultaneousPlayBacks = 8;

        private string path;
        public string Path => path;

        private int sample;

        public readonly AudioManager Manager;

        internal AudioSample(AudioManager audioManager, string path, ResourceManager resourceManager)
        {
            Manager = audioManager;
            this.path = path;

            sample = Bass.SampleLoad(path, 0, 0, MaxSimultaneousPlayBacks, BassFlags.SampleOverrideLongestPlaying);
            if (sample == 0)
            {
                Trace.WriteLine($"Failed to load audio sample ({path}): {Bass.LastError}");
                return;
            }
        }

        public void Play(float volume = 1)
        {
            if (sample == 0) return;
            var channel = new AudioChannel(Manager, Bass.SampleGetChannel(sample), true)
            {
                Volume = volume,
            };
            Manager.RegisterChannel(channel);
            channel.Playing = true;
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                if (sample != 0)
                {
                    Bass.SampleFree(sample);
                    sample = 0;
                }
                disposedValue = true;
            }
        }

        ~AudioSample()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
