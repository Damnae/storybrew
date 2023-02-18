using System;

namespace BrewLib.Time
{
    public class TimeSourceExtender : TimeSource
    {
        readonly Clock clock = new Clock();
        readonly TimeSource timeSource;

        public double Current => timeSource.Playing ? timeSource.Current : clock.Current;

        public bool Playing
        {
            get => clock.Playing;
            set
            {
                if (clock.Playing == value) return;

                timeSource.Playing = value && timeSource.Seek(clock.Current);
                clock.Playing = value;
            }
        }
        public double TimeFactor
        {
            get => clock.TimeFactor;
            set
            {
                timeSource.TimeFactor = value;
                clock.TimeFactor = value;
            }
        }
        public TimeSourceExtender(TimeSource timeSource) => this.timeSource = timeSource;

        public bool Seek(double time)
        {
            if (!timeSource.Seek(time)) timeSource.Playing = false;
            return clock.Seek(time);
        }
        public void Update()
        {
            timeSource.Playing = clock.Playing && (timeSource.Playing || timeSource.Seek(clock.Current));

            if (timeSource.Playing && Math.Abs(clock.Current - timeSource.Current) > .005f)
                clock.Seek(timeSource.Current);
        }
    }
}