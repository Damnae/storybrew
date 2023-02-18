using BrewLib.Data;
using ManagedBass;
using System;
using System.Diagnostics;

namespace BrewLib.Audio
{
    public class AudioSample
    {
        const int MaxSimultaneousPlayBacks = 8;

        int sample;

        public readonly AudioManager Manager;
        public string Path { get; }

        internal AudioSample(AudioManager audioManager, string path, ResourceContainer resourceContainer)
        {
            Manager = audioManager;
            Path = path;

            sample = Bass.SampleLoad(path, 0, 0, MaxSimultaneousPlayBacks, BassFlags.SampleOverrideLongestPlaying);
            if (sample != 0) return;

            var bytes = resourceContainer?.GetBytes(path, ResourceSource.Embedded);
            if (bytes != null)
            {
                sample = Bass.SampleLoad(bytes, 0, bytes.Length, MaxSimultaneousPlayBacks, BassFlags.SampleOverrideLongestPlaying);
                if (sample != 0) return;
            }

            Trace.WriteLine($"Failed to load audio sample ({path}): {Bass.LastError}");
        }
        public void Play(float volume = 1, float pitch = 1, float pan = 0)
        {
            if (sample == 0) return;
            var channel = new AudioChannel(Manager, Bass.SampleGetChannel(sample), true)
            {
                Volume = volume,
                Pitch = pitch,
                Pan = pan
            };
            Manager.RegisterChannel(channel);
            channel.Playing = true;
        }

        #region IDisposable Support

        bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) { }
                if (sample != 0)
                {
                    Bass.SampleFree(sample);
                    sample = 0;
                }
                disposedValue = true;
            }
        }
        ~AudioSample() => Dispose(false);
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}