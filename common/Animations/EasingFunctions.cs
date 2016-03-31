﻿using StorybrewCommon.Storyboarding;
using System;

namespace StorybrewCommon.Animations
{
    public static class EasingFunctions
    {
        public static double ToOut(Func<double, double> function, double value) => 1 - function(1 - value);
        public static double ToInOut(Func<double, double> function, double value) => .5 * (value < .5 ? function(2 * value) : (2 - function(2 - 2 * value)));

        public static Func<double, double> Step = x => x >= 1 ? 1 : 0;
        public static Func<double, double> Linear = x => x;

        public static Func<double, double> QuadIn = x => x * x;
        public static Func<double, double> QuadOut = x => ToOut(QuadIn, x);
        public static Func<double, double> QuadInOut = x => ToInOut(QuadIn, x);
        public static Func<double, double> CubicIn = x => x * x * x;
        public static Func<double, double> CubicOut = x => ToOut(CubicIn, x);
        public static Func<double, double> CubicInOut = x => ToInOut(CubicIn, x);
        public static Func<double, double> QuartIn = x => x * x * x * x;
        public static Func<double, double> QuartOut = x => ToOut(QuartIn, x);
        public static Func<double, double> QuartInOut = x => ToInOut(QuartIn, x);
        public static Func<double, double> QuintIn = x => x * x * x * x * x;
        public static Func<double, double> QuintOut = x => ToOut(QuintIn, x);
        public static Func<double, double> QuintInOut = x => ToInOut(QuintIn, x);

        public static Func<double, double> SineIn = x => 1 - Math.Cos(x * Math.PI / 2);
        public static Func<double, double> SineOut = x => ToOut(SineIn, x);
        public static Func<double, double> SineInOut = x => ToInOut(SineIn, x);

        public static Func<double, double> ExpoIn = x => Math.Pow(2, 10 * (x - 1));
        public static Func<double, double> ExpoOut = x => ToOut(ExpoIn, x);
        public static Func<double, double> ExpoInOut = x => ToInOut(ExpoIn, x);

        public static Func<double, double> CircIn = x => 1 - Math.Sqrt(1 - x * x);
        public static Func<double, double> CircOut = x => ToOut(CircIn, x);
        public static Func<double, double> CircInOut = x => ToInOut(CircIn, x);

        public static Func<double, double> BackIn = x => x * x * ((1.70158 + 1) * x - 1.70158);
        public static Func<double, double> BackOut = x => ToOut(BackIn, x);
        public static Func<double, double> BackInOut = x => ToInOut((y) => y * y * ((1.70158 * 1.525 + 1) * y - 1.70158 * 1.525), x);

        public static Func<double, double> BounceIn = x => 1 - (x < 0.25 / 2.75 ? 7.5625 * ((x - 0.125) / 2.75) * (x - 0.125) + 0.984375 : x < 0.75 / 2.75 ? 7.5625 * ((x - 0.5) / 2.75) * (x - 0.5) + 0.9375 : x < 1.75 / 2.75 ? 7.5625 * ((x - 1.25) / 2.75) * (x - 1.25) + 0.75 : 7.5625 * (x - 1) * (x - 1));
        public static Func<double, double> BounceOut = x => ToOut(BounceIn, x);
        public static Func<double, double> BounceInOut = x => ToInOut(BounceIn, x);

        public static Func<double, double> ElasticIn = x => -(Math.Pow(2, 10 * (x - 1)) * Math.Sin((x - (0.3 / (2 * Math.PI) * Math.Asin(1))) * 2 * Math.PI / 0.3));
        public static Func<double, double> ElasticOut = x => ToOut(ElasticIn, x);
        public static Func<double, double> ElasticInOut = x => ToInOut((y) => -(Math.Pow(2, 10 * (y - 1)) * Math.Sin((y - (0.45 / (2 * Math.PI) * Math.Asin(1))) * 2 * Math.PI / 0.45)), x);

        public static double Ease(this OsbEasing easing, double value)
        {
            switch (easing)
            {
                default:
                case OsbEasing.None: return value;

                case OsbEasing.In:
                case OsbEasing.InQuad: return QuadIn(value);
                case OsbEasing.Out:
                case OsbEasing.OutQuad: return QuadOut(value);
                case OsbEasing.InOutQuad: return QuadInOut(value);

                case OsbEasing.InCubic: return CubicIn(value);
                case OsbEasing.OutCubic: return CubicOut(value);
                case OsbEasing.InOutCubic: return CubicInOut(value);
                case OsbEasing.InQuart: return QuartIn(value);
                case OsbEasing.OutQuart: return QuartOut(value);
                case OsbEasing.InOutQuart: return QuartInOut(value);
                case OsbEasing.InQuint: return QuintIn(value);
                case OsbEasing.OutQuint: return QuintOut(value);
                case OsbEasing.InOutQuint: return QuintInOut(value);

                case OsbEasing.InSine: return SineIn(value);
                case OsbEasing.OutSine: return SineOut(value);
                case OsbEasing.InOutSine: return SineInOut(value);
                case OsbEasing.InExpo: return ExpoIn(value);
                case OsbEasing.OutExpo: return ExpoOut(value);
                case OsbEasing.InOutExpo: return ExpoInOut(value);
                case OsbEasing.InCirc: return CircIn(value);
                case OsbEasing.OutCirc: return CircOut(value);
                case OsbEasing.InOutCirc: return CircInOut(value);
                case OsbEasing.InElastic: return ElasticIn(value);
                case OsbEasing.OutElastic:
                case OsbEasing.OutElasticHalf:
                case OsbEasing.OutElasticQuarter: return ElasticOut(value);
                case OsbEasing.InOutElastic: return ElasticInOut(value);
                case OsbEasing.InBack: return BackIn(value);
                case OsbEasing.OutBack: return BackOut(value);
                case OsbEasing.InOutBack: return BackInOut(value);
                case OsbEasing.InBounce: return BounceIn(value);
                case OsbEasing.OutBounce: return BounceOut(value);
                case OsbEasing.InOutBounce: return BounceInOut(value);
            }
        }
    }
}
