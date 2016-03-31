using ManagedBass.Dynamics;
using System;
using System.Collections.Generic;

namespace StorybrewEditor.Audio
{
    public class AudioManager : IDisposable
    {
        private List<AudioStream> audioStreams = new List<AudioStream>();

        private float volume = 1;
        public float Volume
        {
            get { return volume; }
            set
            {
                if (volume == value) return;
                volume = value;
                foreach (var audio in audioStreams)
                    audio.UpdateVolume();
            }
        }

        public AudioManager(IntPtr windowHandle)
        {
            Bass.Init(-1, 44100, DeviceInitFlags.Default, windowHandle);

            Bass.Configure(Configuration.PlaybackBufferLength, 100);
            Bass.Configure(Configuration.NetBufferLength, 500);
            Bass.Configure(Configuration.UpdatePeriod, 10);
        }

        public AudioStream LoadStream(string path)
        {
            var audio = new AudioStream(this, path);
            audioStreams.Add(audio);
            return audio;
        }

        internal void NotifyDisposed(AudioStream audio)
        {
            audioStreams.Remove(audio);
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
                Bass.Free(Bass.CurrentDevice);
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
