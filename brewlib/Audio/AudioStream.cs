using ManagedBass;
using ManagedBass.Fx;
using System;
using System.Diagnostics;

namespace BrewLib.Audio
{
    public class AudioStream : IDisposable
    {
        private string path;
        private int stream;
        private int decodeStream;

        public readonly AudioManager Manager;

        public double Time
        {
            get
            {
                if (stream == 0) return 0;
                var position = Bass.ChannelGetPosition(stream, PositionFlags.Bytes);
                return Bass.ChannelBytes2Seconds(stream, position);
            }
            set
            {
                if (stream == 0) return;
                var position = Bass.ChannelSeconds2Bytes(stream, value);
                Bass.ChannelSetPosition(stream, position);
            }
        }

        private double duration;
        public double Duration => duration;

        public bool Playing
        {
            get
            {
                if (stream == 0) return false;
                return Bass.ChannelIsActive(stream) == PlaybackState.Playing;
            }
            set
            {
                if (stream == 0) return;
                if (value) Bass.ChannelPlay(stream, false);
                else Bass.ChannelPause(stream);
            }
        }

        private float volume = 1;
        public float Volume
        {
            get
            {
                return volume;
            }
            set
            {
                if (volume == value) return;
                volume = value;
                UpdateVolume();
            }
        }

        private double timeFactor = 1;
        public double TimeFactor
        {
            get
            {
                return timeFactor;
            }
            set
            {
                if (timeFactor == value) return;
                timeFactor = value;
                updateTimeFactor();
            }
        }

        public AudioStream(AudioManager manager, string path)
        {
            Manager = manager;
            this.path = path;

            decodeStream = Bass.CreateStream(path, 0, 0, BassFlags.Decode | BassFlags.Prescan);
            if (decodeStream == 0)
            {
                Trace.WriteLine($"Failed to load audio stream ({path}): {Bass.LastError}");
                return;
            }
            stream = BassFx.TempoCreate(decodeStream, BassFlags.Default);

            Bass.ChannelSetAttribute(stream, ChannelAttribute.TempoUseQuickAlgorithm, 1);
            Bass.ChannelSetAttribute(stream, ChannelAttribute.TempoOverlapMilliseconds, 4);
            Bass.ChannelSetAttribute(stream, ChannelAttribute.TempoSequenceMilliseconds, 30);
            duration = Bass.ChannelBytes2Seconds(stream, Bass.ChannelGetLength(stream));

            UpdateVolume();
            updateTimeFactor();
        }

        public void UpdateVolume()
        {
            if (stream == 0) return;
            Bass.ChannelSetAttribute(stream, ChannelAttribute.Volume, volume * Manager.Volume);
        }

        private void updateTimeFactor()
        {
            if (stream == 0) return;
            Bass.ChannelSetAttribute(stream, ChannelAttribute.Tempo, (int)((timeFactor - 1) * 100));
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
                if (stream != 0)
                {
                    Bass.StreamFree(stream);
                    stream = 0;
                }
                if (decodeStream != 0)
                {
                    Bass.StreamFree(decodeStream);
                    decodeStream = 0;
                }
                disposedValue = true;
                if (disposing) Manager.NotifyDisposed(this);
            }
        }

        ~AudioStream()
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
