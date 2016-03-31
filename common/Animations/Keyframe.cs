using System;

namespace StorybrewCommon.Animations
{
    public struct Keyframe<TValue> : IComparable<Keyframe<TValue>>
    {
        public double Time;
        public TValue Value;
        public Func<double, double> Ease;

        public Keyframe(double time)
            : this(time, default(TValue))
        {
        }

        public Keyframe(double time, TValue value)
            : this(time, value, EasingFunctions.Step)
        {
        }

        public Keyframe(double time, TValue value, Func<double, double> easing)
        {
            Time = time;
            Value = value;
            Ease = easing;
        }

        public int CompareTo(Keyframe<TValue> other)
        {
            return Math.Sign(Time - other.Time);
        }

        public override string ToString()
        {
            return string.Format("{0:0.000}s {1}:{2}", Time, typeof(TValue), Value);
        }
    }
}
