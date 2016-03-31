using System;
using System.Collections.Generic;

namespace StorybrewCommon.Animations
{
    public class KeyframedValue<TValue>
    {
        private List<Keyframe<TValue>> keyframes = new List<Keyframe<TValue>>();
        private Func<TValue, TValue, double, TValue> interpolate;

        public KeyframedValue(Func<TValue, TValue, double, TValue> interpolate)
        {
            this.interpolate = interpolate;
        }
        
        public void Add(double time, TValue value)
        {
            Add(time, value, EasingFunctions.Linear);
        }

        public void Add(double time, TValue value, Func<double, double> easing)
        {
            var keyframe = new Keyframe<TValue>(time, value, easing);
            if (keyframes.Count == 0 || time > keyframes[keyframes.Count - 1].Time)
            {
                keyframes.Add(keyframe);
            }
            else
            {
                int index = keyframes.BinarySearch(keyframe);
                if (index < 0) index = ~index;
                keyframes.Insert(index, keyframe);
            }
        }

        public void SimplifyKeyframes()
        {
            var simplifiedKeyframes = new List<Keyframe<TValue>>();
            for (int i = 0, count = keyframes.Count; i < count; i++)
            {
                var startKeyframe = keyframes[i];
                simplifiedKeyframes.Add(startKeyframe);

                for (int j = i + 1; j < count; j++)
                {
                    var endKeyframe = keyframes[j];
                    if (!startKeyframe.Value.Equals(endKeyframe.Value))
                    {
                        if (i < j - 1) simplifiedKeyframes.Add(keyframes[j - 1]);
                        simplifiedKeyframes.Add(endKeyframe);
                        i = j;
                        break;
                    }
                    else if (j == count - 1)
                        i = j;
                }
            }
            simplifiedKeyframes.TrimExcess();
            keyframes = simplifiedKeyframes;
        }

        public TValue ValueAt(double time)
        {
            if (keyframes.Count == 0) return default(TValue);
            if (keyframes.Count == 1) return keyframes[0].Value;

            int index = keyframes.BinarySearch(new Keyframe<TValue>(time));
            if (index < 0) index = ~index;

            if (index == 0)
            {
                return keyframes[0].Value;
            }
            else if (index == keyframes.Count)
            {
                return keyframes[keyframes.Count - 1].Value;
            }
            else
            {
                var from = keyframes[index - 1];
                var to = keyframes[index];
                if (from.Time == to.Time) return to.Value;

                var progress = to.Ease((time - from.Time) / (to.Time - from.Time));
                return interpolate(from.Value, to.Value, progress);
            }
        }
    }
}
