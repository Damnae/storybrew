using BrewLib.Data;
using ManagedBass;
using ManagedBass.Fx;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace BrewLib.Audio
{
    public class AudioStream : AudioChannel
    {
        int stream;
        int decodeStream;

        public string Path { get; }

        internal AudioStream(AudioManager manager, string path, ResourceContainer resourceContainer) : base(manager)
        {
            Path = path;
            var flags = BassFlags.Decode | BassFlags.Prescan;

            decodeStream = Bass.CreateStream(path, 0, 0, flags);
            if (decodeStream == 0 && !System.IO.Path.IsPathRooted(path))
            {
                var resourceStream = resourceContainer.GetStream(path, ResourceSource.Embedded);
                if (resourceStream != null)
                {
                    var readBuffer = new byte[32768];
                    var procedures = new FileProcedures
                    {
                        Read = (buffer, length, user) =>
                        {
                            if (length > readBuffer.Length) readBuffer = new byte[length];
                            if (!resourceStream.CanRead) return 0;

                            var readBytes = resourceStream.Read(readBuffer, 0, length);
                            Marshal.Copy(readBuffer, 0, buffer, readBytes);
                            return readBytes;
                        },
                        Length = user => resourceStream.Length,
                        Seek = (offset, user) => resourceStream.Seek(offset, SeekOrigin.Begin) == offset,
                        Close = user => resourceStream.Dispose()
                    };
                    decodeStream = Bass.CreateStream(StreamSystem.NoBuffer, flags, procedures, IntPtr.Zero);
                }
            }
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
