using ManagedBass;
using OpenTK;
using System;

namespace BrewLib.Audio
{
    public class AudioChannel : IDisposable
    {
        public readonly AudioManager Manager;

        float frequency;
        public float Frequency => frequency;

        int channel;
        protected int Channel
        {
            get => channel;
            set
            {
                if (channel == value) return;
                channel = value;

                if (channel == 0) return;

                Bass.ChannelGetAttribute(channel, ChannelAttribute.Frequency, out frequency);
                Duration = Bass.ChannelBytes2Seconds(channel, Bass.ChannelGetLength(channel));

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
        public double Duration { get; set; }

        bool played;
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

        bool loop;
        public bool Loop
        {
            get => loop;
            set
            {
                if (loop == value) return;
                loop = value;
                if (channel == 0) return;
                Bass.ChannelFlags(channel, loop ? BassFlags.Loop : 0, BassFlags.Loop);
            }
        }

        public bool Completed => played && Bass.ChannelIsActive(channel) == PlaybackState.Stopped;

        float volume = 1;
        public float Volume
        {
            get => volume;
            set
            {
                if (volume == value) return;
                volume = value;
                UpdateVolume();
            }
        }

        double timeFactor = 1;
        public double TimeFactor
        {
            get => timeFactor;
            set
            {
                if (timeFactor == value) return;
                timeFactor = value;
                updateTimeFactor();
            }
        }

        float pitch = 1;
        public float Pitch
        {
            get => pitch;
            set
            {
                if (pitch == value) return;
                pitch = value;
                updatePitch();
            }
        }

        float pan = 0;
        public float Pan
        {
            get => pan;
            set
            {
                value = MathHelper.Clamp(value, -1, 1);
                if (pan == value) return;

                pan = value;
                updatePan();
            }
        }

        public int AvailableData
        {
            get
            {
                if (channel == 0) return 0;
                return Bass.ChannelGetData(channel, IntPtr.Zero, (int)DataFlags.Available);
            }
        }
        public bool Temporary { get; }

        internal AudioChannel(AudioManager audioManager, int channel = 0, bool temporary = false)
        {
            Manager = audioManager;
            Channel = channel;
            Temporary = temporary;
        }
        public float[] GetFft(bool complex = false)
        {
            var length = 2048;
            var flags = DataFlags.FFT4096;

            if (complex)
            {
                flags |= DataFlags.FFTComplex;
                length *= 4;
            }

            var data = new float[length];
            Bass.ChannelGetData(channel, data, (int)flags);
            return data;
        }
        internal void UpdateVolume()
        {
            if (channel == 0) return;
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Volume, SoundUtil.FromLinearVolume(volume * Manager.Volume));
        }
        void updateTimeFactor()
        {
            if (channel == 0) return;
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Tempo, (int)((timeFactor - 1) * 100));
        }
        void updatePitch()
        {
            if (channel == 0 || frequency <= 0) return;
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Frequency, MathHelper.Clamp(frequency * pitch, 100, 100000));
        }
        void updatePan()
        {
            if (channel == 0) return;
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Pan, pan);
        }

        #region IDisposable Support

        bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) { }
                channel = 0;
                disposedValue = true;
                if (disposing) Manager.UnregisterChannel(this);
            }
        }

        ~AudioChannel() => Dispose(false);
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}