using System;
using System.IO;

namespace BrewLib.Util
{
    public class ByteCounterStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        private long length;
        public override long Length => length;
        public override long Position
        {
            get { return length; }
            set { throw new NotSupportedException(); }
        }

        public ByteCounterStream()
        {
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset + count > buffer.Length) throw new ArgumentException();
            if (buffer == null) throw new ArgumentNullException();
            if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException();

            length += count;
        }
    }
}
