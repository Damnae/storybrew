using OpenTK;
using System;
using System.Collections.Generic;
using System.Collections;

namespace StorybrewCommon.Animations
{
    public class KeyframedValue<TValue> : MarshalByRefObject, IEnumerable<Keyframe<TValue>>
    {
        private List<Keyframe<TValue>> keyframes = new List<Keyframe<TValue>>();
        private Func<TValue, TValue, double, TValue> interpolate;
        private TValue defaultValue;

        public double StartTime => keyframes.Count == 0 ? 0 : keyframes[0].Time;
        public double EndTime => keyframes.Count == 0 ? 0 : keyframes[keyframes.Count - 1].Time;
        public TValue StartValue => keyframes.Count == 0 ? defaultValue : keyframes[0].Value;
        public TValue EndValue => keyframes.Count == 0 ? defaultValue : keyframes[keyframes.Count - 1].Value;
        public int Count => keyframes.Count;

        public KeyframedValue(Func<TValue, TValue, double, TValue> interpolate, TValue defaultValue = default(TValue))
        {
            this.interpolate = interpolate;
            this.defaultValue = defaultValue;
        }

        public KeyframedValue<TValue> Add(Keyframe<TValue> keyframe, bool before = false)
        {
            if (keyframes.Count == 0 || keyframes[keyframes.Count - 1].Time < keyframe.Time)
                keyframes.Add(keyframe);
            else keyframes.Insert(indexFor(keyframe, before), keyframe);
            return this;
        }

        public KeyframedValue<TValue> Add(params Keyframe<TValue>[] values)
            => AddRange(values);

        public KeyframedValue<TValue> Add(double time, TValue value, bool before = false)
            => Add(time, value, EasingFunctions.Linear, before);

        public KeyframedValue<TValue> Add(double time, TValue value, Func<double, double> easing, bool before = false)
            => Add(new Keyframe<TValue>(time, value, easing), before);

        public KeyframedValue<TValue> AddRange(IEnumerable<Keyframe<TValue>> collection)
        {
            foreach (var keyframe in collection)
                Add(keyframe);
            return this;
        }

        public void TransferKeyframes(KeyframedValue<TValue> to, bool pad = true, bool clear = true)
        {
            if (pad && to.Count > 0) to.Add(StartTime, to.EndValue);
            to.AddRange(this);
            if (clear) Clear();
        }

        public TValue ValueAt(double time)
        {
            if (keyframes.Count == 0) return defaultValue;
            if (keyframes.Count == 1) return keyframes[0].Value;

            var index = indexAt(time, false);
            if (index == 0)
                return keyframes[0].Value;
            else if (index == keyframes.Count)
                return keyframes[keyframes.Count - 1].Value;
            else
            {
                var from = keyframes[index - 1];
                var to = keyframes[index];
                if (from.Time == to.Time) return to.Value;

                var progress = to.Ease((time - from.Time) / (to.Time - from.Time));
                return interpolate(from.Value, to.Value, progress);
            }
        }

        public void ForEachPair(Action<Keyframe<TValue>, Keyframe<TValue>> pair, TValue defaultValue = default(TValue), Func<TValue, TValue> edit = null)
        {
            var hasPair = false;
            var previous = (Keyframe<TValue>?)null;
            var stepStart = (Keyframe<TValue>?)null;
            foreach (var keyframe in keyframes)
            {
                var endKeyframe = editKeyframe(keyframe, edit);
                if (previous.HasValue)
                {
                    var startKeyframe = previous.Value;

                    var isFlat = startKeyframe.Value.Equals(endKeyframe.Value);
                    var isStep = !isFlat && startKeyframe.Time == endKeyframe.Time;

                    if (isStep)
                    {
                        if (!stepStart.HasValue)
                            stepStart = startKeyframe;
                    }
                    else if (stepStart.HasValue)
                    {
                        pair(stepStart.Value, startKeyframe);
                        stepStart = null;
                        hasPair = true;
                    }

                    if (!isStep && !isFlat)
                    {
                        pair(startKeyframe, endKeyframe);
                        hasPair = true;
                    }
                }
                previous = endKeyframe;
            }

            if (stepStart.HasValue)
            {
                pair(stepStart.Value, previous.Value);
                stepStart = null;
                hasPair = true;
            }

            if (!hasPair && keyframes.Count > 0)
            {
                var first = editKeyframe(keyframes[0], edit);
                if (!first.Value.Equals(defaultValue))
                    pair(first, first);
            }
        }

        private static Keyframe<TValue> editKeyframe(Keyframe<TValue> keyframe, Func<TValue, TValue> edit = null)
            => edit != null ? new Keyframe<TValue>(keyframe.Time, edit(keyframe.Value), keyframe.Ease) : keyframe;

        public void Clear() => keyframes.Clear();

        public IEnumerator<Keyframe<TValue>> GetEnumerator() => keyframes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private int indexFor(Keyframe<TValue> keyframe, bool before)
        {
            var index = keyframes.BinarySearch(keyframe);
            if (index < 0) index = ~index;

            if (before)
                while (index > 0 && keyframes[index].Time >= keyframe.Time) index--;
            else while (index < keyframes.Count && keyframes[index].Time <= keyframe.Time) index++;
            return index;
        }
        private int indexAt(double time, bool before) => indexFor(new Keyframe<TValue>(time), before);

        #region Manipulation

        public void Linearize(double timestep)
        {
            var linearKeyframes = new List<Keyframe<TValue>>();

            var previousKeyframe = (Keyframe<TValue>?)null;
            foreach (var keyframe in keyframes)
            {
                if (previousKeyframe.HasValue)
                {
                    var startKeyFrame = previousKeyframe.Value;

                    var duration = keyframe.Time - startKeyFrame.Time;
                    var steps = (int)(duration / timestep);
                    var actualTimestep = duration / steps;

                    for (var i = 0; i < steps; i++)
                    {
                        var time = startKeyFrame.Time + i * actualTimestep;
                        linearKeyframes.Add(new Keyframe<TValue>(time, ValueAt(time)));
                    }
                }
                previousKeyframe = keyframe;
            }
            var endTime = keyframes[keyframes.Count - 1].Time;
            linearKeyframes.Add(new Keyframe<TValue>(endTime, ValueAt(endTime)));

            linearKeyframes.TrimExcess();
            keyframes = linearKeyframes;
        }

        public void SimplifyEqualKeyframes()
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

        public void Simplify1dKeyframes(double tolerance, Func<TValue, float> getComponent)
            => SimplifyKeyframes(tolerance, (startKeyframe, middleKeyframe, endKeyframe) =>
            {
                var start = new Vector2((float)startKeyframe.Time, getComponent(startKeyframe.Value));
                var middle = new Vector2((float)middleKeyframe.Time, getComponent(middleKeyframe.Value));
                var end = new Vector2((float)endKeyframe.Time, getComponent(endKeyframe.Value));

                var area = Math.Abs(.5 * (start.X * end.Y + end.X * middle.Y + middle.X * start.Y - end.X * start.Y - middle.X * end.Y - start.X * middle.Y));
                var bottom = Math.Sqrt(Math.Pow(start.X - end.X, 2) + Math.Pow(start.Y - end.Y, 2));
                return area / bottom * 2;
            });

        public void Simplify2dKeyframes(double tolerance, Func<TValue, Vector2> getComponent)
            => SimplifyKeyframes(tolerance, (startKeyframe, middleKeyframe, endKeyframe) =>
            {
                var startComponent = getComponent(startKeyframe.Value);
                var middleComponent = getComponent(middleKeyframe.Value);
                var endComponent = getComponent(endKeyframe.Value);

                var start = new Vector3((float)startKeyframe.Time, startComponent.X, startComponent.Y);
                var middle = new Vector3((float)middleKeyframe.Time, middleComponent.X, middleComponent.Y);
                var end = new Vector3((float)endKeyframe.Time, endComponent.X, endComponent.Y);

                var startToMiddle = middle - start;
                var startToEnd = end - start;
                return (startToMiddle - (Vector3.Dot(startToMiddle, startToEnd) / Vector3.Dot(startToEnd, startToEnd)) * startToEnd).Length;
            });

        public void Simplify3dKeyframes(double tolerance, Func<TValue, Vector3> getComponent)
            => SimplifyKeyframes(tolerance, (startKeyframe, middleKeyframe, endKeyframe) =>
            {
                var startComponent = getComponent(startKeyframe.Value);
                var middleComponent = getComponent(middleKeyframe.Value);
                var endComponent = getComponent(endKeyframe.Value);

                var start = new Vector4((float)startKeyframe.Time, startComponent.X, startComponent.Y, startComponent.Z);
                var middle = new Vector4((float)middleKeyframe.Time, middleComponent.X, middleComponent.Y, middleComponent.Z);
                var end = new Vector4((float)endKeyframe.Time, endComponent.X, endComponent.Y, endComponent.Z);

                var startToMiddle = middle - start;
                var startToEnd = end - start;
                return (startToMiddle - (Vector4.Dot(startToMiddle, startToEnd) / Vector4.Dot(startToEnd, startToEnd)) * startToEnd).Length;
            });

        public void SimplifyKeyframes(double tolerance, Func<Keyframe<TValue>, Keyframe<TValue>, Keyframe<TValue>, double> getDistance)
        {
            if (keyframes.Count < 3)
                return;

            var firstPoint = 0;
            var lastPoint = keyframes.Count - 1;
            var keyframesToKeep = new List<int>() { firstPoint, lastPoint };
            getSimplifiedKeyframeIndexes(ref keyframesToKeep, firstPoint, lastPoint, tolerance, getDistance);

            if (keyframesToKeep.Count == keyframes.Count)
                return;

            keyframesToKeep.Sort();
            var simplifiedKeyframes = new List<Keyframe<TValue>>(keyframesToKeep.Count);
            foreach (var index in keyframesToKeep)
            {
                var keyframe = keyframes[index];
                simplifiedKeyframes.Add(new Keyframe<TValue>(keyframe.Time, keyframe.Value));
            }
            keyframes = simplifiedKeyframes;
        }

        // Douglas Peucker
        private void getSimplifiedKeyframeIndexes(ref List<int> keyframesToKeep, int firstPoint, int lastPoint, double tolerance, Func<Keyframe<TValue>, Keyframe<TValue>, Keyframe<TValue>, double> getDistance)
        {
            var start = keyframes[firstPoint];
            var end = keyframes[lastPoint];

            var maxDistance = 0.0;
            var indexFarthest = 0;
            for (var index = firstPoint; index < lastPoint; index++)
            {
                var middle = keyframes[index];
                var distance = getDistance(start, middle, end);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    indexFarthest = index;
                }
            }
            if (maxDistance > tolerance && indexFarthest != 0)
            {
                keyframesToKeep.Add(indexFarthest);
                getSimplifiedKeyframeIndexes(ref keyframesToKeep, firstPoint, indexFarthest, tolerance, getDistance);
                getSimplifiedKeyframeIndexes(ref keyframesToKeep, indexFarthest, lastPoint, tolerance, getDistance);
            }
        }

        #endregion
    }
}
