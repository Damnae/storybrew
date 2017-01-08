using StorybrewCommon.Storyboarding;
using System;

namespace StorybrewCommon.Animations
{
    public static class EasingFunctions
    {
        public static double Reverse(Func<double, double> function, double value) => 1 - function(1 - value);
        public static double ToInOut(Func<double, double> function, double value) => .5 * (value < .5 ? function(2 * value) : (2 - function(2 - 2 * value)));

        public static Func<double, double> Step = x => x >= 1 ? 1 : 0;
        public static Func<double, double> Linear = x => x;

        public static Func<double, double> QuadIn = x => x * x;
        public static Func<double, double> QuadOut = x => Reverse(QuadIn, x);
        public static Func<double, double> QuadInOut = x => ToInOut(QuadIn, x);
        public static Func<double, double> CubicIn = x => x * x * x;
        public static Func<double, double> CubicOut = x => Reverse(CubicIn, x);
        public static Func<double, double> CubicInOut = x => ToInOut(CubicIn, x);
        public static Func<double, double> QuartIn = x => x * x * x * x;
        public static Func<double, double> QuartOut = x => Reverse(QuartIn, x);
        public static Func<double, double> QuartInOut = x => ToInOut(QuartIn, x);
        public static Func<double, double> QuintIn = x => x * x * x * x * x;
        public static Func<double, double> QuintOut = x => Reverse(QuintIn, x);
        public static Func<double, double> QuintInOut = x => ToInOut(QuintIn, x);

        public static Func<double, double> SineIn = x => 1 - Math.Cos(x * Math.PI / 2);
        public static Func<double, double> SineOut = x => Reverse(SineIn, x);
        public static Func<double, double> SineInOut = x => ToInOut(SineIn, x);

        public static Func<double, double> ExpoIn = x => Math.Pow(2, 10 * (x - 1));
        public static Func<double, double> ExpoOut = x => Reverse(ExpoIn, x);
        public static Func<double, double> ExpoInOut = x => ToInOut(ExpoIn, x);

        public static Func<double, double> CircIn = x => 1 - Math.Sqrt(1 - x * x);
        public static Func<double, double> CircOut = x => Reverse(CircIn, x);
        public static Func<double, double> CircInOut = x => ToInOut(CircIn, x);

        public static Func<double, double> BackIn = x => x * x * ((1.70158 + 1) * x - 1.70158);
        public static Func<double, double> BackOut = x => Reverse(BackIn, x);
        public static Func<double, double> BackInOut = x => ToInOut((y) => y * y * ((1.70158 * 1.525 + 1) * y - 1.70158 * 1.525), x);

        public static Func<double, double> BounceIn = x => Reverse(BounceOut, x);
        public static Func<double, double> BounceOut = x => x < 1 / 2.75 ? 7.5625 * x * x : x < 2 / 2.75 ? 7.5625 * (x -= (1.5 / 2.75)) * x + .75 : x < 2.5 / 2.75 ? 7.5625 * (x -= (2.25 / 2.75)) * x + .9375 : 7.5625 * (x -= (2.625 / 2.75)) * x + .984375;
        public static Func<double, double> BounceInOut = x => ToInOut(BounceIn, x);

        public static Func<double, double> ElasticIn = x => Reverse(ElasticOut, x);
        public static Func<double, double> ElasticOut = x => Math.Pow(2, -10 * x) * Math.Sin((x - 0.075) * (2 * Math.PI) / .3) + 1;
        public static Func<double, double> ElasticOutHalf = x => Math.Pow(2, -10 * x) * Math.Sin((0.5 * x - 0.075) * (2 * Math.PI) / .3) + 1;
        public static Func<double, double> ElasticOutQuarter = x => Math.Pow(2, -10 * x) * Math.Sin((0.25 * x - 0.075) * (2 * Math.PI) / .3) + 1;
        public static Func<double, double> ElasticInOut = x => ToInOut(ElasticIn, x);

        public static double Ease(this OsbEasing easing, double value)
            => easing.ToEasingFunction().Invoke(value);

        public static Func<double, double> ToEasingFunction(this OsbEasing easing)
        {
            switch (easing)
            {
                default:
                case OsbEasing.None: return Linear;

                case OsbEasing.In:
                case OsbEasing.InQuad: return QuadIn;
                case OsbEasing.Out:
                case OsbEasing.OutQuad: return QuadOut;
                case OsbEasing.InOutQuad: return QuadInOut;

                case OsbEasing.InCubic: return CubicIn;
                case OsbEasing.OutCubic: return CubicOut;
                case OsbEasing.InOutCubic: return CubicInOut;
                case OsbEasing.InQuart: return QuartIn;
                case OsbEasing.OutQuart: return QuartOut;
                case OsbEasing.InOutQuart: return QuartInOut;
                case OsbEasing.InQuint: return QuintIn;
                case OsbEasing.OutQuint: return QuintOut;
                case OsbEasing.InOutQuint: return QuintInOut;

                case OsbEasing.InSine: return SineIn;
                case OsbEasing.OutSine: return SineOut;
                case OsbEasing.InOutSine: return SineInOut;
                case OsbEasing.InExpo: return ExpoIn;
                case OsbEasing.OutExpo: return ExpoOut;
                case OsbEasing.InOutExpo: return ExpoInOut;
                case OsbEasing.InCirc: return CircIn;
                case OsbEasing.OutCirc: return CircOut;
                case OsbEasing.InOutCirc: return CircInOut;
                case OsbEasing.InElastic: return ElasticIn;
                case OsbEasing.OutElastic: return ElasticOut;
                case OsbEasing.OutElasticHalf: return ElasticOutHalf;
                case OsbEasing.OutElasticQuarter: return ElasticOutQuarter;
                case OsbEasing.InOutElastic: return ElasticInOut;
                case OsbEasing.InBack: return BackIn;
                case OsbEasing.OutBack: return BackOut;
                case OsbEasing.InOutBack: return BackInOut;
                case OsbEasing.InBounce: return BounceIn;
                case OsbEasing.OutBounce: return BounceOut;
                case OsbEasing.InOutBounce: return BounceInOut;
            }
        }
    }
}
