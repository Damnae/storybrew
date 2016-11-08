using System;

namespace BrewLib.Time
{
    public abstract class TimeSource
    {
        public abstract double Current { get; set; }
        public abstract double Previous { get; }

        public double Delta => Current - Previous;

        public event EventHandler Changed;
        protected void NotifyTimeChanged()
            => Changed?.Invoke(this, EventArgs.Empty);
    }
}
