using OpenTK;
using System;

namespace StorybrewCommon.Animations
{
    public static class KeyframedValueExtensions
    {
        public static void ForEachFlag(this KeyframedValue<bool> keyframes, Action<double, double> action)
        {
            var active = false;
            var startTime = 0.0;
            var lastKeyframeTime = 0.0;
            foreach (var keyframe in keyframes)
                if (keyframe.Value != active)
                {
                    if (keyframe.Value)
                    {
                        startTime = keyframe.Time;
                        active = true;
                    }
                    else
                    {
                        action(startTime, keyframe.Time);
                        active = false;
                    }
                }
                else lastKeyframeTime = keyframe.Time;

            if (active)
                action(startTime, lastKeyframeTime);
        }

        public static KeyframedValue<Vector2> Add(this KeyframedValue<Vector2> keyframes, double time, float x, float y, Func<double, double> easing = null)
        {
            keyframes.Add(time, new Vector2(x, y), easing);
            return keyframes;
        }

        public static KeyframedValue<Vector2> Add(this KeyframedValue<Vector2> keyframes, double time, float scale, Func<double, double> easing = null)
        {
            keyframes.Add(time, new Vector2(scale), easing);
            return keyframes;
        }

        public static KeyframedValue<Vector3> Add(this KeyframedValue<Vector3> keyframes, double time, float x, float y, float z, Func<double, double> easing = null)
        {
            keyframes.Add(time, new Vector3(x, y, z), easing);
            return keyframes;
        }

        public static KeyframedValue<Vector3> Add(this KeyframedValue<Vector3> keyframes, double time, float scale, Func<double, double> easing = null)
        {
            keyframes.Add(time, new Vector3(scale), easing);
            return keyframes;
        }

        public static KeyframedValue<Quaternion> Add(this KeyframedValue<Quaternion> keyframes, double time, Vector3 axis, float angle, Func<double, double> easing = null)
        {
            keyframes.Add(time, Quaternion.FromAxisAngle(axis, angle), easing);
            return keyframes;
        }

        public static KeyframedValue<Quaternion> Add(this KeyframedValue<Quaternion> keyframes, double time, float angle, Func<double, double> easing = null)
        {
            keyframes.Add(time, new Vector3(0, 0, 1), angle, easing);
            return keyframes;
        }
}
}
