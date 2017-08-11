using OpenTK;
using OpenTK.Graphics;
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

        public static KeyframedValue<Vector2> Add(this KeyframedValue<Vector2> keyframes, double time, float x, float y)
        {
            keyframes.Add(time, new Vector2(x, y));
            return keyframes;
        }

        public static KeyframedValue<Vector3> Add(this KeyframedValue<Vector3> keyframes, double time, float x, float y, float z)
        {
            keyframes.Add(time, new Vector3(x, y, z));
            return keyframes;
        }

        public static KeyframedValue<Quaternion> Add(this KeyframedValue<Quaternion> keyframes, double time, Vector3 axis, float angle)
        {
            keyframes.Add(time, Quaternion.FromAxisAngle(axis, angle));
            return keyframes;
        }

        public static KeyframedValue<Quaternion> Add(this KeyframedValue<Quaternion> keyframes, double time, float angle)
        {
            keyframes.Add(time, new Vector3(0, 0, 1), angle);
            return keyframes;
        }
}
}
