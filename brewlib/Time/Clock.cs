namespace BrewLib.Time
{
    public class Clock : TimeSource
    {
        private double time;
        private double previousTime;

        public override double Current
        {
            get { return time; }
            set
            {
                previousTime = time;
                time = value;

                if (previousTime != time)
                    NotifyTimeChanged();
            }
        }
        public override double Previous => previousTime;

        public void Reset() => Current = 0;
    }
}
