using System;

namespace StorybrewCommon.Animations
{
    public struct Keyframe<TValue> : IComparable<Keyframe<TValue>>
    {
        public readonly double Time;
        public readonly TValue Value;
        public readonly Func<double, double> Ease;

        public Keyframe(double time)
            : this(time, default(TValue))
        {
        }

        public Keyframe(double time, TValue value, Func<double, double> easing = null)
        {
            Time = time;
            Value = value;
            Ease = easing ?? EasingFunctions.Linear;
        }

        public Keyframe<TValue> WithTime(double time)
            => new Keyframe<TValue>(time, Value, Ease);

        public Keyframe<TValue> WithValue(TValue value)
            => new Keyframe<TValue>(Time, value, Ease);

        public int CompareTo(Keyframe<TValue> other)
        {
            return Math.Sign(Time - other.Time);
        }

        public override string ToString() => $"{Time:0.000}s {typeof(TValue)}:{Value}";
    }
}
