using BrewLib.Data;
using ManagedBass;
using System;
using System.Collections.Generic;

namespace BrewLib.Audio
{
    public class AudioManager : IDisposable
    {
        readonly List<AudioChannel> audioChannels = new List<AudioChannel>();

        float volume = 1;
        public float Volume
        {
            get => volume;
            set
            {
                if (volume == value) return;

                volume = value;
                foreach (var audio in audioChannels) audio.UpdateVolume();
            }
        }
        public AudioManager(IntPtr windowHandle)
        {
            Bass.Init(-1, 44100, DeviceInitFlags.Default, windowHandle);
            Bass.PlaybackBufferLength = 100;
            Bass.NetBufferLength = 500;
            Bass.UpdatePeriod = 10;
        }

        public void Update()
        {
            for (var i = 0; i < audioChannels.Count; i++)
            {
                var channel = audioChannels[i];
                if (channel.Temporary && channel.Completed)
                {
                    channel.Dispose();
                    i--;
                }
            }
        }
        public AudioStream LoadStream(string path, ResourceContainer resourceContainer = null)
        {
            var audio = new AudioStream(this, path, resourceContainer);
            RegisterChannel(audio);
            return audio;
        }
        public AudioStreamPush CreateStream(int frequency, int channels)
        {
            var audio = new AudioStreamPush(this, frequency, channels);
            RegisterChannel(audio);
            return audio;
        }
        public AudioStreamPull CreateStream(int frequency, int channels, AudioStreamPull.CallbackDelegate callback)
        {
            var audio = new AudioStreamPull(this, frequency, channels, callback);
            RegisterChannel(audio);
            return audio;
        }

        public AudioSample LoadSample(string path, ResourceContainer resourceContainer = null)
            => new AudioSample(this, path, resourceContainer);

        internal void RegisterChannel(AudioChannel channel) => audioChannels.Add(channel);
        internal void UnregisterChannel(AudioChannel channel) => audioChannels.Remove(channel);

        #region IDisposable Support

        bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Bass.Free();
                disposedValue = true;
            }
        }

        ~AudioManager() => Dispose(false);
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}