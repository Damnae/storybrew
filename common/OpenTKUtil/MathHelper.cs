using System;
using System.Diagnostics.Contracts;
using System.Globalization;

// OpenTK 4.2
namespace StorybrewCommon.OpenTKUtil
{
    /// <summary>
    /// Contains common mathematical functions and constants.
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// Defines the value of Pi as a <see cref="float"/>.
        /// </summary>
        public const float Pi = (float)Math.PI;

        /// <summary>
        /// Defines the value of Pi divided by two as a <see cref="float"/>.
        /// </summary>
        public const float PiOver2 = Pi / 2;

        /// <summary>
        /// Defines the value of Pi divided by three as a <see cref="float"/>.
        /// </summary>
        public const float PiOver3 = Pi / 3;

        /// <summary>
        /// Defines the value of  Pi divided by four as a <see cref="float"/>.
        /// </summary>
        public const float PiOver4 = Pi / 4;

        /// <summary>
        /// Defines the value of Pi divided by six as a <see cref="float"/>.
        /// </summary>
        public const float PiOver6 = Pi / 6;

        /// <summary>
        /// Defines the value of Pi multiplied by two as a <see cref="float"/>.
        /// </summary>
        public const float TwoPi = 2 * Pi;

        /// <summary>
        /// Defines the value of Pi multiplied by 3 and divided by two as a <see cref="float"/>.
        /// </summary>
        public const float ThreePiOver2 = 3 * Pi / 2;

        /// <summary>
        /// Defines the value of E as a <see cref="float"/>.
        /// </summary>
        public const float E = (float)Math.E;

        /// <summary>
        /// Defines the base-10 logarithm of E.
        /// </summary>
        public const float Log10E = .4342945f;

        /// <summary>
        /// Defines the base-2 logarithm of E.
        /// </summary>
        public const float Log2E = 1.442695f;

        ///<inheritdoc cref="Math.Abs(decimal)"/>
        [Pure]
        public static decimal Abs(decimal n) => Math.Abs(n);

        ///<inheritdoc cref="Math.Abs(double)"/>
        [Pure]
        public static double Abs(double n) => Math.Abs(n);

        ///<inheritdoc cref="Math.Abs(short)"/>
        [Pure]
        public static short Abs(short n) => Math.Abs(n);

        ///<inheritdoc cref="Math.Abs(int)"/>
        [Pure]
        public static int Abs(int n) => Math.Abs(n);

        ///<inheritdoc cref="Math.Abs(long)"/>
        [Pure]
        public static long Abs(long n) => Math.Abs(n);

        ///<inheritdoc cref="Math.Abs(sbyte)"/>
        [Pure]
        public static sbyte Abs(sbyte n) => Math.Abs(n);

        ///<inheritdoc cref="Math.Abs(float)"/>
        [Pure]
        public static float Abs(float n) => Math.Abs(n);

        ///<inheritdoc cref="Math.Sin"/>
        [Pure]
        public static double Sin(double radians) => Math.Sin(radians);

        ///<inheritdoc cref="Math.Sinh"/>
        [Pure]
        public static double Sinh(double radians) => Math.Sinh(radians);

        ///<inheritdoc cref="Math.Asin"/>
        [Pure]
        public static double Asin(double radians) => Math.Asin(radians);

        ///<inheritdoc cref="Math.Cos"/>
        [Pure]
        public static double Cos(double radians) => Math.Cos(radians);

        ///<inheritdoc cref="Math.Cosh"/>
        [Pure]
        public static double Cosh(double radians) => Math.Cosh(radians);

        ///<inheritdoc cref="Math.Acos"/>
        [Pure]
        public static double Acos(double radians) => Math.Acos(radians);

        ///<inheritdoc cref="Math.Tan"/>
        [Pure]
        public static double Tan(double radians) => Math.Tan(radians);

        ///<inheritdoc cref="Math.Tanh"/>
        [Pure]
        public static double Tanh(double radians) => Math.Tanh(radians);

        ///<inheritdoc cref="Math.Atan"/>
        [Pure]
        public static double Atan(double radians) => Math.Atan(radians);

        ///<inheritdoc cref="Math.Atan2"/>
        [Pure]
        public static double Atan2(double y, double x) => Math.Atan2(y, x);

        ///<inheritdoc cref="Math.BigMul"/>
        [Pure]
        public static long BigMul(int a, int b) => Math.BigMul(a, b);

        ///<inheritdoc cref="Math.Sqrt"/>
        [Pure]
        public static double Sqrt(double n) => Math.Sqrt(n);

        ///<inheritdoc cref="Math.Pow"/>
        [Pure]
        public static double Pow(double x, double y) => Math.Pow(x, y);

        ///<inheritdoc cref="Math.Ceiling(decimal)"/>
        [Pure]
        public static decimal Ceiling(decimal n) => Math.Ceiling(n);

        ///<inheritdoc cref="Math.Ceiling(double)"/>
        [Pure]
        public static double Ceiling(double n) => Math.Ceiling(n);

        ///<inheritdoc cref="Math.Floor(decimal)"/>
        [Pure]
        public static decimal Floor(decimal n) => Math.Floor(n);

        ///<inheritdoc cref="Math.Floor(double)"/>
        [Pure]
        public static double Floor(double n) => Math.Floor(n);

        ///<inheritdoc cref="Math.DivRem(int, int, out int)"/>
        [Pure]
        public static int DivRem(int a, int b, out int result) => Math.DivRem(a, b, out result);

        ///<inheritdoc cref="Math.DivRem(long, long, out long)"/>
        [Pure]
        public static long DivRem(long a, long b, out long result) => Math.DivRem(a, b, out result);

        ///<inheritdoc cref="Math.Log(double)"/>
        [Pure]
        public static double Log(double n) => Math.Log(n);

        ///<inheritdoc cref="Math.Log(double, double)"/>
        [Pure]
        public static double Log(double n, double newBase) => Math.Log(n, newBase);

        ///<inheritdoc cref="Math.Log10"/>
        [Pure]
        public static double Log10(double n) => Math.Log10(n);

        ///<inheritdoc cref="Math.Exp"/>
        [Pure]
        public static double Exp(double n) => Math.Exp(n);

        ///<inheritdoc cref="Math.IEEERemainder"/>
        [Pure]
        public static double IEEERemainder(double a, double b) => Math.IEEERemainder(a, b);

        ///<inheritdoc cref="Math.Max(byte, byte)"/>
        [Pure]
        public static byte Max(byte a, byte b) => Math.Max(a, b);

        ///<inheritdoc cref="Math.Max(sbyte, sbyte)"/>
        [Pure]
        public static sbyte Max(sbyte a, sbyte b) => Math.Max(a, b);

        ///<inheritdoc cref="Math.Max(short, short)"/>
        [Pure]
        public static short Max(short a, short b) => Math.Max(a, b);

        ///<inheritdoc cref="Math.Max(ushort, ushort)"/>
        [Pure]
        public static ushort Max(ushort a, ushort b) => Math.Max(a, b);

        ///<inheritdoc cref="Math.Max(decimal, decimal)"/>
        [Pure]
        public static decimal Max(decimal a, decimal b) => Math.Max(a, b);

        ///<inheritdoc cref="Math.Max(int, int)"/>
        [Pure]
        public static int Max(int a, int b) => Math.Max(a, b);

        ///<inheritdoc cref="Math.Max(uint, uint)"/>
        [Pure]
        public static uint Max(uint a, uint b) => Math.Max(a, b);

        ///<inheritdoc cref="Math.Max(float, float)"/>
        [Pure]
        public static float Max(float a, float b) => Math.Max(a, b);

        ///<inheritdoc cref="Math.Max(long, long)"/>
        [Pure]
        public static long Max(long a, long b) => Math.Max(a, b);

        ///<inheritdoc cref="Math.Max(ulong, ulong)"/>
        [Pure]
        public static ulong Max(ulong a, ulong b) => Math.Max(a, b);

        ///<inheritdoc cref="Math.Min(byte, byte)"/>
        [Pure]
        public static byte Min(byte a, byte b) => Math.Min(a, b);

        ///<inheritdoc cref="Math.Min(sbyte, sbyte)"/>
        [Pure]
        public static sbyte Min(sbyte a, sbyte b) => Math.Min(a, b);

        ///<inheritdoc cref="Math.Min(short, short)"/>
        [Pure]
        public static short Min(short a, short b) => Math.Min(a, b);

        ///<inheritdoc cref="Math.Min(ushort, ushort)"/>
        [Pure]
        public static ushort Min(ushort a, ushort b) => Math.Min(a, b);

        ///<inheritdoc cref="Math.Min(decimal, decimal)"/>
        [Pure]
        public static decimal Min(decimal a, decimal b) => Math.Min(a, b);

        ///<inheritdoc cref="Math.Min(int, int)"/>
        [Pure]
        public static int Min(int a, int b) => Math.Min(a, b);

        ///<inheritdoc cref="Math.Min(uint, uint)"/>
        [Pure]
        public static uint Min(uint a, uint b) => Math.Min(a, b);

        ///<inheritdoc cref="Math.Min(float, float)"/>
        [Pure]
        public static float Min(float a, float b) => Math.Min(a, b);

        ///<inheritdoc cref="Math.Min(double, double)"/>
        [Pure]
        public static double Min(double a, double b) => Math.Min(a, b);

        ///<inheritdoc cref="Math.Min(long, long)"/>
        [Pure]
        public static long Min(long a, long b) => Math.Min(a, b);

        ///<inheritdoc cref="Math.Min(ulong, ulong)"/>
        [Pure]
        public static ulong Min(ulong a, ulong b) => Math.Min(a, b);

        ///<inheritdoc cref="Math.Round(decimal, int, MidpointRounding)"/>
        [Pure]
        public static decimal Round(decimal d, int digits, MidpointRounding mode) => Math.Round(d, digits, mode);

        ///<inheritdoc cref="Math.Round(double, int, MidpointRounding)"/>
        [Pure]
        public static double Round(double d, int digits, MidpointRounding mode) => Math.Round(d, digits, mode);

        ///<inheritdoc cref="Math.Round(decimal, MidpointRounding)"/>
        [Pure]
        public static decimal Round(decimal d, MidpointRounding mode) => Math.Round(d, mode);

        ///<inheritdoc cref="Math.Round(double, MidpointRounding)"/>
        [Pure]
        public static double Round(double d, MidpointRounding mode) => Math.Round(d, mode);

        ///<inheritdoc cref="Math.Round(decimal, int)"/>
        [Pure]
        public static decimal Round(decimal d, int digits) => Math.Round(d, digits);

        ///<inheritdoc cref="Math.Round(double, int)"/>
        [Pure]
        public static double Round(double d, int digits) => Math.Round(d, digits);

        ///<inheritdoc cref="Math.Round(decimal)"/>
        [Pure]
        public static decimal Round(decimal d) => Math.Round(d);

        ///<inheritdoc cref="Math.Round(double)"/>
        [Pure]
        public static double Round(double d) => Math.Round(d);

        ///<inheritdoc cref="Math.Truncate(decimal)"/>
        [Pure]
        public static decimal Truncate(decimal d) => Math.Truncate(d);

        ///<inheritdoc cref="Math.Truncate(double)"/>
        [Pure]
        public static double Truncate(double d) => Math.Truncate(d);

        ///<inheritdoc cref="Math.Sign(sbyte)"/>
        [Pure]
        public static int Sign(sbyte d) => Math.Sign(d);

        ///<inheritdoc cref="Math.Sign(short)"/>
        [Pure]
        public static int Sign(short d) => Math.Sign(d);

        ///<inheritdoc cref="Math.Sign(int)"/>
        [Pure]
        public static int Sign(int d) => Math.Sign(d);

        ///<inheritdoc cref="Math.Sign(float)"/>
        [Pure]
        public static int Sign(float d) => Math.Sign(d);

        ///<inheritdoc cref="Math.Sign(decimal)"/>
        [Pure]
        public static int Sign(decimal d) => Math.Sign(d);

        ///<inheritdoc cref="Math.Sign(double)"/>
        [Pure]
        public static int Sign(double d) => Math.Sign(d);

        ///<inheritdoc cref="Math.Sign(long)"/>
        [Pure]
        public static int Sign(long d) => Math.Sign(d);

        /// <summary>
        /// Returns the next power of two that is greater than or equal to the specified number.
        /// </summary>
        /// <returns>The next power of two.</returns>
        [Pure]
        public static long NextPowerOfTwo(long n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), "Must be positive.");
            return (long)Math.Pow(2, Math.Ceiling(Math.Log(n, 2)));
        }

        /// <summary>
        /// Returns the next power of two that is greater than or equal to the specified number.
        /// </summary>
        /// <returns>The next power of two.</returns>
        [Pure]
        public static int NextPowerOfTwo(int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), "Must be positive.");
            return (int)Math.Pow(2, Math.Ceiling(Math.Log(n, 2)));
        }

        /// <summary>
        /// Returns the next power of two that is greater than or equal to the specified number.
        /// </summary>
        /// <returns>The next power of two.</returns>
        [Pure]
        public static float NextPowerOfTwo(float n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), "Must be positive.");
            return (float)Math.Pow(2, Math.Ceiling(Math.Log(n, 2)));
        }

        /// <summary>
        /// Returns the next power of two that is greater than or equal to the specified number.
        /// </summary>
        /// <returns>The next power of two.</returns>
        [Pure]
        public static double NextPowerOfTwo(double n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), "Must be positive.");
            return Math.Pow(2, Math.Ceiling(Math.Log(n, 2)));
        }

        /// <summary>
        /// Calculates the factorial of a given natural number.
        /// </summary>
        /// <returns>The factorial of <paramref name="n"/>.</returns>
        [Pure]
        public static long Fact(int n)
        {
            long result = 1;
            for (; n > 1; n--) result *= n;

            return result;
        }

        /// <summary>
        /// Calculates the binomial coefficient <paramref name="n"/> above <paramref name="k"/>.
        /// </summary>
        /// <returns><paramref name="n"/>! / (<paramref name="k"/>! * (<paramref name="n"/> - <paramref name="k"/>)!).</returns>
        [Pure]
        public static long BinomialCoefficient(int n, int k) => Fact(n) / (Fact(k) * Fact(n - k));

        /// <summary>
        /// Convert degrees to radians.
        /// </summary>
        /// <param name="degrees">An angle in degrees.</param>
        /// <returns>The angle expressed in radians.</returns>
        [Pure]
        public static float DegreesToRadians(float degrees) => degrees * (float)Math.PI / 180;

        /// <summary>
        /// Convert radians to degrees.
        /// </summary>
        /// <param name="radians">An angle in radians.</param>
        /// <returns>The angle expressed in degrees.</returns>
        [Pure]
        public static float RadiansToDegrees(float radians) => radians * (float)(180 / Math.PI);

        /// <summary>
        /// Convert degrees to radians.
        /// </summary>
        /// <param name="degrees">An angle in degrees.</param>
        /// <returns>The angle expressed in radians.</returns>
        [Pure]
        public static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;

        /// <summary>
        /// Convert radians to degrees.
        /// </summary>
        /// <param name="radians">An angle in radians.</param>
        /// <returns>The angle expressed in degrees.</returns>
        [Pure]
        public static double RadiansToDegrees(double radians) => radians * 180 / Math.PI;

        /// <summary>
        /// Swaps two float values.
        /// </summary>
        /// <typeparam name="T">The type of the values to swap.</typeparam>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        public static void Swap<T>(ref T a, ref T b) => (b, a) = (a, b);

        /// <summary>
        /// Clamps a number between a minimum and a maximum.
        /// </summary>
        /// <param name="n">The number to clamp.</param>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="max">The maximum allowed value.</param>
        /// <returns>min, if n is lower than min; max, if n is higher than max; n otherwise.</returns>
        [Pure]
        public static int Clamp(int n, int min, int max) => Math.Max(Math.Min(n, max), min);

        /// <summary>
        /// Clamps a number between a minimum and a maximum.
        /// </summary>
        /// <param name="n">The number to clamp.</param>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="max">The maximum allowed value.</param>
        /// <returns>min, if n is lower than min; max, if n is higher than max; n otherwise.</returns>
        [Pure]
        public static float Clamp(float n, float min, float max) => Math.Max(Math.Min(n, max), min);

        /// <summary>
        /// Clamps a number between a minimum and a maximum.
        /// </summary>
        /// <param name="n">The number to clamp.</param>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="max">The maximum allowed value.</param>
        /// <returns>min, if n is lower than min; max, if n is higher than max; n otherwise.</returns>
        [Pure]
        public static double Clamp(double n, double min, double max) => Math.Max(Math.Min(n, max), min);

        /// <summary>
        /// Scales the specified number linearly between a minimum and a maximum.
        /// </summary>
        /// <remarks>If the value range is zero, this function will throw a divide by zero exception.</remarks>
        /// <param name="value">The number to scale.</param>
        /// <param name="valueMin">The minimum expected number (inclusive).</param>
        /// <param name="valueMax">The maximum expected number (inclusive).</param>
        /// <param name="resultMin">The minimum output number (inclusive).</param>
        /// <param name="resultMax">The maximum output number (inclusive).</param>
        /// <returns>The number, scaled linearly between min and max.</returns>
        [Pure]
        public static int MapRange(int value, int valueMin, int valueMax, int resultMin, int resultMax)
        {
            var inRange = valueMax - valueMin;
            var resultRange = resultMax - resultMin;
            return resultMin + (resultRange * ((value - valueMin) / inRange));
        }

        /// <summary>
        /// Scales the specified number linearly between a minimum and a maximum.
        /// </summary>
        /// <remarks>If the value range is zero, this function will throw a divide by zero exception.</remarks>
        /// <param name="value">The number to scale.</param>
        /// <param name="valueMin">The minimum expected number (inclusive).</param>
        /// <param name="valueMax">The maximum expected number (inclusive).</param>
        /// <param name="resultMin">The minimum output number (inclusive).</param>
        /// <param name="resultMax">The maximum output number (inclusive).</param>
        /// <returns>The number, scaled linearly between min and max.</returns>
        [Pure]
        public static float MapRange(float value, float valueMin, float valueMax, float resultMin, float resultMax)
        {
            var inRange = valueMax - valueMin;
            var resultRange = resultMax - resultMin;
            return resultMin + (resultRange * ((value - valueMin) / inRange));
        }

        /// <summary>
        /// Scales the specified number linearly between a minimum and a maximum.
        /// </summary>
        /// <remarks>If the value range is zero, this function will throw a divide by zero exception.</remarks>
        /// <param name="value">The number to scale.</param>
        /// <param name="valueMin">The minimum expected number (inclusive).</param>
        /// <param name="valueMax">The maximum expected number (inclusive).</param>
        /// <param name="resultMin">The minimum output number (inclusive).</param>
        /// <param name="resultMax">The maximum output number (inclusive).</param>
        /// <returns>The number, scaled linearly between min and max.</returns>
        [Pure]
        public static double MapRange(double value, double valueMin, double valueMax, double resultMin, double resultMax)
        {
            var inRange = valueMax - valueMin;
            var resultRange = resultMax - resultMin;
            return resultMin + (resultRange * ((value - valueMin) / inRange));
        }

        /// <summary>
        /// Approximates double-precision floating point equality by an epsilon (maximum error) value.
        /// This method is designed as a "fits-all" solution and attempts to handle as many cases as possible.
        /// </summary>
        /// <param name="a">The first double.</param>
        /// <param name="b">The second double.</param>
        /// <param name="epsilon">The maximum error between the two.</param>
        /// <returns>
        /// <value>true</value> if the values are approximately equal within the error margin; otherwise,
        /// <value>false</value>.
        /// </returns>
        [Pure]
        public static bool ApproximatelyEqualEpsilon(double a, double b, double epsilon)
        {
            const double doubleNormal = (1L << 52) * double.Epsilon;
            var absA = Math.Abs(a);
            var absB = Math.Abs(b);
            var diff = Math.Abs(a - b);

            if (a == b) return true;
            if (a == 0 || b == 0 || diff < doubleNormal) return diff < epsilon * doubleNormal;

            return diff / Math.Min(absA + absB, double.MaxValue) < epsilon;
        }

        /// <summary>
        /// Approximates single-precision floating point equality by an epsilon (maximum error) value.
        /// This method is designed as a "fits-all" solution and attempts to handle as many cases as possible.
        /// </summary>
        /// <param name="a">The first float.</param>
        /// <param name="b">The second float.</param>
        /// <param name="epsilon">The maximum error between the two.</param>
        /// <returns>
        ///  <value>true</value> if the values are approximately equal within the error margin; otherwise,
        ///  <value>false</value>.
        /// </returns>
        [Pure]
        public static bool ApproximatelyEqualEpsilon(float a, float b, float epsilon)
        {
            const float floatNormal = (1 << 23) * float.Epsilon;
            var absA = Math.Abs(a);
            var absB = Math.Abs(b);
            var diff = Math.Abs(a - b);

            if (a == b) return true;
            if (a == 0 || b == 0 || diff < floatNormal) return diff < epsilon * floatNormal;

            var relativeError = diff / Math.Min(absA + absB, float.MaxValue);
            return relativeError < epsilon;
        }

        /// <summary>
        /// Approximates equivalence between two single-precision floating-point numbers on a direct human scale.
        /// It is important to note that this does not approximate equality - instead, it merely checks whether or not
        /// two numbers could be considered equivalent to each other within a certain tolerance. The tolerance is
        /// inclusive.
        /// </summary>
        /// <param name="a">The first value to compare.</param>
        /// <param name="b">The second value to compare.</param>
        /// <param name="tolerance">The tolerance within which the two values would be considered equivalent.</param>
        /// <returns>Whether or not the values can be considered equivalent within the tolerance.</returns>
        [Pure]
        public static bool ApproximatelyEquivalent(float a, float b, float tolerance)
        {
            if (a == b) return true;

            var diff = Math.Abs(a - b);
            return diff <= tolerance;
        }

        /// <summary>
        /// Approximates equivalence between two double-precision floating-point numbers on a direct human scale.
        /// It is important to note that this does not approximate equality - instead, it merely checks whether or not
        /// two numbers could be considered equivalent to each other within a certain tolerance. The tolerance is
        /// inclusive.
        /// </summary>
        /// <param name="a">The first value to compare.</param>
        /// <param name="b">The second value to compare.</param>
        /// <param name="tolerance">The tolerance within which the two values would be considered equivalent.</param>
        /// <returns>Whether or not the values can be considered equivalent within the tolerance.</returns>
        [Pure]
        public static bool ApproximatelyEquivalent(double a, double b, double tolerance)
        {
            if (a == b) return true;

            var diff = Math.Abs(a - b);
            return diff <= tolerance;
        }

        /// <summary>
        /// Normalizes an angle to the range (-180, 180].
        /// </summary>
        /// <param name="angle">The angle in degrees to normalize.</param>
        /// <returns>The normalized angle in the range (-180, 180].</returns>
        public static float NormalizeAngle(float angle)
        {
            // returns angle in the range [0, 360)
            angle = ClampAngle(angle);
            if (angle > 180f) angle -= 360f;

            return angle;
        }

        /// <summary>
        /// Normalizes an angle to the range (-180, 180].
        /// </summary>
        /// <param name="angle">The angle in degrees to normalize.</param>
        /// <returns>The normalized angle in the range (-180, 180].</returns>
        public static double NormalizeAngle(double angle)
        {
            // returns angle in the range [0, 360)
            angle = ClampAngle(angle);
            if (angle > 180f) angle -= 360f;

            return angle;
        }

        /// <summary>
        /// Normalizes an angle to the range (-π, π].
        /// </summary>
        /// <param name="angle">The angle in radians to normalize.</param>
        /// <returns>The normalized angle in the range (-π, π].</returns>
        public static float NormalizeRadians(float angle)
        {
            // returns angle in the range [0, 2π).
            angle = ClampRadians(angle);

            if (angle > Pi) angle -= 2 * Pi;

            return angle;
        }

        /// <summary>
        /// Normalizes an angle to the range (-π, π].
        /// </summary>
        /// <param name="angle">The angle in radians to normalize.</param>
        /// <returns>The normalized angle in the range (-π, π].</returns>
        public static double NormalizeRadians(double angle)
        {
            // returns angle in the range [0, 2π).
            angle = ClampRadians(angle);

            if (angle > Pi) angle -= 2 * Pi;

            return angle;
        }

        /// <summary>
        /// Clamps an angle to the range [0, 360).
        /// </summary>
        /// <param name="angle">The angle to clamp in degrees.</param>
        /// <returns>The clamped angle in the range [0, 360).</returns>
        public static float ClampAngle(float angle)
        {
            // mod angle so it's in the range (-360, 360)
            angle %= 360f;

            if (angle < 0) angle += 360f;

            return angle;
        }

        /// <summary>
        /// Clamps an angle to the range [0, 360).
        /// </summary>
        /// <param name="angle">The angle to clamp in degrees.</param>
        /// <returns>The clamped angle in the range [0, 360).</returns>
        public static double ClampAngle(double angle)
        {
            // mod angle so it's in the range (-360, 360)
            angle %= 360d;

            if (angle < 0) angle += 360d;

            return angle;
        }

        /// <summary>
        /// Clamps an angle to the range [0, 2π).
        /// </summary>
        /// <param name="angle">The angle to clamp in radians.</param>
        /// <returns>The clamped angle in the range [0, 2π).</returns>
        public static float ClampRadians(float angle)
        {
            // mod angle so it's in the range (-2π,2π)
            angle %= TwoPi;
            if (angle < 0) angle += TwoPi;

            return angle;
        }

        /// <summary>
        /// Clamps an angle to the range [0, 2π).
        /// </summary>
        /// <param name="angle">The angle to clamp in radians.</param>
        /// <returns>The clamped angle in the range [0, 2π).</returns>
        public static double ClampRadians(double angle)
        {
            angle %= 2d * Math.PI;
            if (angle < 0) angle += 2 * Math.PI;

            return angle;
        }
        internal static string ListSeparator => CultureInfo.CurrentCulture.TextInfo.ListSeparator;
    }
}