﻿namespace ManagedBass.Dynamics
{
    /// <summary>
    /// Stream File Position modes to be used with <see cref="Bass.StreamGetFilePosition" />
    /// </summary>
    public enum FileStreamPosition
    {
        /// <summary>
        /// Position that is to be decoded for playback next. 
        /// This will be a bit ahead of the position actually being heard due to buffering.
        /// </summary>
        Current = 0,

        /// <summary>
        /// Download progress of an internet file stream or "buffered" User file stream.
        /// </summary>
        Download = 1,

        /// <summary>
        /// End of the file, in other words the file Length. 
        /// When streaming in blocks, the file Length is unknown, so the download Buffer Length is returned instead.
        /// </summary>
        End = 2,

        /// <summary>
        /// Start of stream data in the file.
        /// </summary>
        Start = 3,

        /// <summary>
        /// Internet file stream or "buffered" User file stream is still connected? 0 = no, 1 = yes.
        /// </summary>
        Connected = 4,

        /// <summary>
        /// The amount of data in the Buffer of an internet file stream or "buffered" User file stream.
        /// Unless streaming in blocks, this is the same as <see cref="Download"/>.
        /// </summary>
        Buffer = 5,

        /// <summary>
        /// Returns the socket hanlde used for streaming.
        /// </summary>
        Socket = 6,

        /// <summary>
        /// The amount of data in the asynchronous file reading Buffer. 
        /// This requires that the <see cref="BassFlags.AsyncFile"/> flag was used at the stream's creation.
        /// </summary>
        AsyncBuffer = 7,

        /// <summary>
        /// WMA add-on: internet buffering progress (0-100%)
        /// </summary>
        WmaBuffer = 1000,
    }
}