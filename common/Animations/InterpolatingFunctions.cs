﻿using OpenTK;
using StorybrewCommon.Storyboarding.CommandValues;
using System;

namespace StorybrewCommon.Animations
{
    /// <summary> A static class providing interpolating functions. </summary>
    public static class InterpolatingFunctions
    {
#pragma warning disable CS1591
        public static Func<float, float, double, float> Float = (from, to, progress) => from + (to - from) * (float)progress;
        public static Func<float, float, double, float> FloatAngle = (from, to, progress) => from + (float)(getShortestAngleDelta(from, to) * progress);
        public static Func<double, double, double, double> Double = (from, to, progress) => from + (to - from) * progress;
        public static Func<double, double, double, double> DoubleAngle = (from, to, progress) => from + getShortestAngleDelta(from, to) * progress;
        public static Func<Vector2, Vector2, double, Vector2> Vector2 = (from, to, progress) => from + (to - from) * (float)progress;
        public static Func<Vector3, Vector3, double, Vector3> Vector3 = (from, to, progress) => from + (to - from) * (float)progress;
        public static Func<Quaternion, Quaternion, double, Quaternion> QuaternionSlerp = (from, to, progress) => Quaternion.Slerp(from, to, (float)progress);

        public static Func<bool, bool, double, bool> BoolFrom = (from, to, progress) => from;
        public static Func<bool, bool, double, bool> BoolTo = (from, to, progress) => to;
        public static Func<bool, bool, double, bool> BoolAny = (from, to, progress) => from || to;
        public static Func<bool, bool, double, bool> BoolBoth = (from, to, progress) => from && to;

        public static Func<CommandColor, CommandColor, double, CommandColor> CommandColor = (from, to, progress) => from + (to - from) * (float)progress;

        static double getShortestAngleDelta(double from, double to) => BrewLib.Util.MathUtil.ShortestAngleDelta(from, to);
    }
}