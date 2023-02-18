using ManagedBass;
using System;

namespace BrewLib.Audio
{
    public class FftStream : IDisposable
    {
        readonly string path;
        int stream;
        ChannelInfo info;

        readonly float frequency;
        public float Frequency => frequency;

        public double Duration { get; }

        public FftStream(string path)
        {
            this.path = path;
            stream = Bass.CreateStream(path, 0, 0, BassFlags.Decode | BassFlags.Prescan);
            Duration = Bass.ChannelBytes2Seconds(stream, Bass.ChannelGetLength(stream));
            info = Bass.ChannelGetInfo(stream);

            Bass.ChannelGetAttribute(stream, ChannelAttribute.Frequency, out frequency);
        }

        public float[] GetFft(double time, bool splitChannels = false)
        {
            var position = Bass.ChannelSeconds2Bytes(stream, time);
            Bass.ChannelSetPosition(stream, position);

            var size = 1024;
            var flags = DataFlags.FFT2048;

            if (splitChannels)
            {
                size *= info.Channels;
                flags |= DataFlags.FFTIndividual;
            }

            var data = new float[size];
            Bass.ChannelGetData(stream, data, unchecked((int)flags));
            return data;
        }

        #region IDisposable Support

        bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) { }
                Bass.StreamFree(stream);
                stream = 0;
                disposedValue = true;
            }
        }

        ~FftStream() => Dispose(false);
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
