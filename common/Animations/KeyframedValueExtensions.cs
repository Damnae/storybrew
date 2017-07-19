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

    }
}
