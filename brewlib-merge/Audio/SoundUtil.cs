using System;

namespace BrewLib.Audio
{
    public static class SoundUtil
    {
        public const int C = 40;
        public const int D = C + 2;
        public const int E = D + 2;
        public const int F = E + 1;
        public const int G = F + 2;
        public const int A = G + 2;
        public const int B = A + 2;

        public static double FromLinearVolume(float volume) => Math.Pow(volume, 4);
        public static double GetNoteFrequency(float note, float a = 440) => Math.Pow(2, (note - 49) / 12) * a;
        public static float GetNoteRailsback(float note, float factor = .4f)
        {
            var p = (note - 44) / 44f;
            return p >= 0 ? note + p * p * factor : note + p * p * -factor;
        }

        public static double SquareWave(double t, double period = .5) => t % 2 < period ? 1 : -1;
        public static double SawWave(double t) => (t % 2) - 1;
        public static double SineWave(double t) => Math.Sin(t * Math.PI * 2);
        public static double TriangleWave(double t) => Math.Abs(((t * 4 - 1) % 4) - 2) - 1;
    }
}