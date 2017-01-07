using ManagedBass;
using System;

namespace BrewLib.Audio
{
    public class AudioChannel : IDisposable
    {
        public readonly AudioManager Manager;

        private int channel;
        protected int Channel
        {
            get { return channel; }
            set
            {
                if (channel == value) return;
                channel = value;

                if (channel == 0) return;
                duration = Bass.ChannelBytes2Seconds(channel, Bass.ChannelGetLength(channel));
                UpdateVolume();
                updateTimeFactor();
            }
        }

        public double Time
        {
            get
            {
                if (channel == 0) return 0;
                var position = Bass.ChannelGetPosition(channel, PositionFlags.Bytes);
                return Bass.ChannelBytes2Seconds(channel, position);
            }
            set
            {
                if (channel == 0) return;
                var position = Bass.ChannelSeconds2Bytes(channel, value);
                Bass.ChannelSetPosition(channel, position);
            }
        }

        private double duration;
        public double Duration => duration;

        private bool played;
        public bool Playing
        {
            get
            {
                if (channel == 0) return false;
                var playbackState = Bass.ChannelIsActive(channel);
                return playbackState == PlaybackState.Playing || playbackState == PlaybackState.Stalled;
            }
            set
            {
                if (channel == 0) return;
                if (value)
                {
                    Bass.ChannelPlay(channel, false);
                    played = true;
                }
                else Bass.ChannelPause(channel);
            }
        }

        public bool Completed => played && Bass.ChannelIsActive(channel) == PlaybackState.Stopped;

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

        private bool temporary;
        public bool Temporary => temporary;

        internal AudioChannel(AudioManager audioManager, int channel = 0, bool temporary = false)
        {
            Manager = audioManager;
            Channel = channel;
            this.temporary = temporary;
        }

        public void UpdateVolume()
        {
            if (channel == 0) return;
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Volume, volume * Manager.Volume);
        }

        private void updateTimeFactor()
        {
            if (channel == 0) return;
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Tempo, (int)((timeFactor - 1) * 100));
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
                channel = 0;
                disposedValue = true;
                if (disposing) Manager.UnregisterChannel(this);
            }
        }

        ~AudioChannel()
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
