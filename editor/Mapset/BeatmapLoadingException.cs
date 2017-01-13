using System;
using System.Runtime.Serialization;

namespace StorybrewEditor.Mapset
{
    [Serializable]
    public class BeatmapLoadingException : Exception
    {
        public BeatmapLoadingException()
        {
        }

        public BeatmapLoadingException(string message) : base(message)
        {
        }

        public BeatmapLoadingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BeatmapLoadingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
