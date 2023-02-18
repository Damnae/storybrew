using BrewLib.Time;

namespace BrewLib.Audio
{
    public class AudioChannelTimeSource : TimeSource
    {
        readonly AudioChannel channel;

        public double Current => channel.Time;
        public bool Playing
        {
            get => channel.Playing;
            set => channel.Playing = value;
        }
        public double TimeFactor
        {
            get => channel.TimeFactor;
            set => channel.TimeFactor = value;
        }
        public AudioChannelTimeSource(AudioChannel channel) => this.channel = channel;

        public bool Seek(double time)
        {
            if (time >= 0 && time < channel.Duration)
            {
                channel.Time = time;
                return true;
            }
            return false;
        }
    }
}