﻿using OpenTK;
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

        public TValue StartValue => keyframes.Count == 0 ? defaultValue : keyframes[0].Value;
        public TValue EndValue => keyframes.Count == 0 ? defaultValue : keyframes[keyframes.Count - 1].Value;
        public int Count => keyframes.Count;

        public KeyframedValue(Func<TValue, TValue, double, TValue> interpolate, TValue defaultValue = default(TValue))
        {
            this.interpolate = interpolate;
            this.defaultValue = defaultValue;
        }

        public void Add(double time, TValue value)
        {
            Add(time, value, EasingFunctions.Linear);
        }

        public void Add(double time, TValue value, Func<double, double> easing)
        {
            var keyframe = new Keyframe<TValue>(time, value, easing);
            if (keyframes.Count == 0 || keyframes[keyframes.Count - 1].Time < time)
                keyframes.Add(keyframe);
            else
                keyframes.Insert(indexFor(keyframe), keyframe);
        }

        public TValue ValueAt(double time)
        {
            if (keyframes.Count == 0) return defaultValue;
            if (keyframes.Count == 1) return keyframes[0].Value;

            var index = indexAt(time);
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

        public void ForEachPair(Action<Keyframe<TValue>, Keyframe<TValue>> pair)
        {
            var hasPair = false;
            var previousKeyframe = (Keyframe<TValue>?)null;
            foreach (var keyframe in keyframes)
            {
                if (previousKeyframe.HasValue && !previousKeyframe.Value.Value.Equals(keyframe.Value))
                {
                    pair(previousKeyframe.Value, keyframe);
                    hasPair = true;
                }
                previousKeyframe = keyframe;
            }

            if (!hasPair && previousKeyframe.HasValue)
                pair(previousKeyframe.Value, previousKeyframe.Value);
        }

        public IEnumerator<Keyframe<TValue>> GetEnumerator() => keyframes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private int indexFor(Keyframe<TValue> keyframe)
        {
            var index = keyframes.BinarySearch(keyframe);
            if (index < 0) index = ~index;
            while (index < keyframes.Count && keyframes[index].Time <= keyframe.Time) index++;
            return index;
        }
        private int indexAt(double time) => indexFor(new Keyframe<TValue>(time));

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
