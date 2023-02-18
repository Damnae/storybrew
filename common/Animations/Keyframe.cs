using System;

namespace StorybrewCommon.Animations
{
    /// <summary> Keyframing support. </summary>
    /// <typeparam name="TValue"> Any type value. </typeparam>
    public struct Keyframe<TValue> : IComparable<Keyframe<TValue>>
    {
        ///<summary> Time of this keyframe. </summary>
        public readonly double Time;

        ///<summary> Type value of this keyframe. </summary>
        public readonly TValue Value;

        ///<summary> <see cref="EasingFunctions"/> type of this keyframe. </summary>
        public readonly Func<double, double> Ease;

        ///<summary> Initializes a new keyframe with default type value. </summary>
        ///<param name="time"> Time of the keyframe. </param>
        public Keyframe(double time) : this(time, default) { }

        ///<summary> Initializes a new keyframe. </summary>
        ///<param name="time"> Time of the keyframe. </param>
        ///<param name="value"> Any type value to be assigned to the keyframe. </param>
        ///<param name="easing"> An <see cref="EasingFunctions"/> type to be assigned. </param>
        public Keyframe(double time, TValue value, Func<double, double> easing = null)
        {
            Time = time;
            Value = value;
            Ease = easing ?? EasingFunctions.Linear;
        }

        ///<summary> Overrides a keyframe with a new time. </summary>
        ///<param name="time"> The time to be overriden with. </param>
        public Keyframe<TValue> WithTime(double time) => new Keyframe<TValue>(time, Value, Ease);

        ///<summary> Overrides a keyframe with a new type value. </summary>
        ///<param name="value"> The type value to be overriden with. </param>
        public Keyframe<TValue> WithValue(TValue value) => new Keyframe<TValue>(Time, value, Ease);

        ///<summary> Compares a keyframe to another keyframe of the same type. </summary>
        ///<param name="other"> The other keyframe to be compared. </param>
        ///<returns> A relative value of the comparison. </returns>
        public int CompareTo(Keyframe<TValue> other) => Math.Sign(Time - other.Time);

        ///<returns> The fully qualified type name of this keyframe. </returns>
        public override string ToString() => $"{Time:0.000}s {typeof(TValue)}:{Value}";
    }
}