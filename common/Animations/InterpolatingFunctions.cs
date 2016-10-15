using OpenTK;
using StorybrewCommon.Storyboarding.CommandValues;
using System;

namespace StorybrewCommon.Animations
{
    public static class InterpolatingFunctions
    {
        public static Func<float, float, double, float> Float = (from, to, progress) => from + (to - from) * (float)progress;
        public static Func<Vector3, Vector3, double, Vector3> Vector3 = (from, to, progress) => from + (to - from) * (float)progress;
        public static Func<Quaternion, Quaternion, double, Quaternion> QuaternionSlerp = (from, to, progress) => Quaternion.Slerp(from, to, (float)progress);

        public static Func<CommandColor, CommandColor, double, CommandColor> CommandColor = (from, to, progress) => from + (to - from) * (float)progress;
    }
}
