using System;

namespace BrewLib.Time
{
    public interface FrameTimeSource : ReadOnlyTimeSource
    {
        double Previous { get; }
        double Elapsed { get; }

        event EventHandler Changed;
    }
    public class FrameClock : FrameTimeSource
    {
        public double Current { get; private set; }
        public double Previous { get; private set; }

        public double Elapsed => Current - Previous;

        public double TimeFactor => 1;
        public bool Playing => true;

        public event EventHandler Changed;

        public void AdvanceFrame(double duration)
        {
            Previous = Current;
            Current += duration;

            if (Previous != Current) Changed?.Invoke(this, EventArgs.Empty);
        }
        public void AdvanceFrameTo(double time)
        {
            Previous = Current;
            Current = time;

            if (Previous != Current) Changed?.Invoke(this, EventArgs.Empty);
        }
        public void Reset() => Current = 0;
    }
}