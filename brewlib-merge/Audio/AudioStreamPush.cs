using ManagedBass;
using ManagedBass.Fx;
using System.Diagnostics;

namespace BrewLib.Audio
{
    public class AudioStreamPush : AudioChannel
    {
        int stream, decodeStream;

        readonly int bytesPerSample;

        internal AudioStreamPush(AudioManager manager, int frequency, int channels) : base(manager)
        {
            bytesPerSample = sizeof(short);

            var flags = BassFlags.Decode;
            if (channels == 1) flags |= BassFlags.Mono;

            decodeStream = Bass.CreateStream(frequency, channels, flags, StreamProcedureType.Push);
            if (decodeStream == 0)
            {
                Trace.WriteLine($"Failed to create push audio stream: {Bass.LastError}");
                return;
            }

            stream = BassFx.TempoCreate(decodeStream, BassFlags.Default);
            Bass.ChannelSetAttribute(stream, ChannelAttribute.TempoUseQuickAlgorithm, 1);
            Bass.ChannelSetAttribute(stream, ChannelAttribute.TempoOverlapMilliseconds, 4);
            Bass.ChannelSetAttribute(stream, ChannelAttribute.TempoSequenceMilliseconds, 30);

            Channel = stream;
        }

        public void PushData(short[] data, int sampleCount) => Bass.StreamPutData(decodeStream, data, sampleCount * bytesPerSample);

        #region IDisposable Support

        bool disposedValue = false;
        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) { }
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
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}