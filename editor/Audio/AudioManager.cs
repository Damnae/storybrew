using ManagedBass;
using System;
using System.Collections.Generic;

namespace StorybrewEditor.Audio
{
    public class AudioManager : IDisposable
    {
        private List<AudioStream> audioStreams = new List<AudioStream>();

        private float volume = 1;

        public AudioManager(IntPtr windowHandle)
        {
            Bass.Init(-1, 44100, DeviceInitFlags.Default, windowHandle);
            Bass.PlaybackBufferLength = 100;
            Bass.NetBufferLength = 500;
            Bass.UpdatePeriod = 10;

            updateVolume();
            Program.Settings.Volume.OnValueChanged += (sender, e) => updateVolume();
        }

        public AudioStream LoadStream(string path)
        {
            var audio = new AudioStream(this, path);
            audioStreams.Add(audio);
            return audio;
        }

        public void NotifyDisposed(AudioStream audio)
        {
            audioStreams.Remove(audio);
        }

        private void updateVolume()
        {
            var newVolume = Program.Settings.Volume;
            if (volume == newVolume) return;

            volume = newVolume;
            foreach (var audio in audioStreams)
                audio.UpdateVolume();
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
