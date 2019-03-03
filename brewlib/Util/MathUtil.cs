using System;

namespace BrewLib.Util
{
    public static class MathUtil
    {
        public static bool FloatEquals(float a, float b, float epsilon)
            => Math.Abs(a - b) < epsilon;

        public static bool DoubleEquals(double a, double b, double epsilon)
            => Math.Abs(a - b) < epsilon;
    }
}
