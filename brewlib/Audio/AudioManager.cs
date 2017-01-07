using ManagedBass;
using System;
using System.Collections.Generic;
using System.Resources;

namespace BrewLib.Audio
{
    public class AudioManager : IDisposable
    {
        private List<AudioChannel> audioChannels = new List<AudioChannel>();

        private float volume = 1;
        public float Volume
        {
            get { return volume; }
            set
            {
                if (volume == value) return;

                volume = value;
                foreach (var audio in audioChannels)
                    audio.UpdateVolume();
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

        public AudioStream LoadStream(string path, ResourceManager resourceManager = null)
        {
            var audio = new AudioStream(this, path, resourceManager);
            RegisterChannel(audio);
            return audio;
        }

        public AudioSample LoadSample(string path, ResourceManager resourceManager = null)
            => new AudioSample(this, path, resourceManager);

        internal void RegisterChannel(AudioChannel channel)
           => audioChannels.Add(channel);

        internal void UnregisterChannel(AudioChannel channel)
            => audioChannels.Remove(channel);

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                Bass.Free();
                disposedValue = true;
            }
        }

        ~AudioManager()
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
