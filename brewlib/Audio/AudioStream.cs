using ManagedBass;
using ManagedBass.Fx;
using System.Diagnostics;
using System.Resources;

namespace BrewLib.Audio
{
    public class AudioStream : AudioChannel
    {
        private string path;
        public string Path => path;

        private int stream;
        private int decodeStream;

        internal AudioStream(AudioManager manager, string path, ResourceManager resourceManager) : base(manager)
        {
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

            Channel = stream;
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected override void Dispose(bool disposing)
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
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
