using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using System;
using System.Linq;

namespace StorybrewScripts
{
    /// <summary>
    /// An example of a radial spectrum effect, using movement instead of scaling.
    /// </summary>
    public class RadialSpectrum : StoryboardObjectGenerator
    {
        [Configurable]
        public int StartTime = 0;

        [Configurable]
        public int EndTime = 10000;

        [Configurable]
        public Vector2 Position = new Vector2(320, 240);

        [Configurable]
        public int BeatDivisor = 8;

        [Configurable]
        public int BarCount = 20;

        [Configurable]
        public string SpritePath = "sb/bar.png";

        [Configurable]
        public OsbOrigin SpriteOrigin = OsbOrigin.Centre;

        [Configurable]
        public Vector2 SpriteScale = Vector2.One;

        [Configurable]
        public int Radius = 50;

        [Configurable]
        public float Scale = 50;

        [Configurable]
        public int LogScale = 600;

        [Configurable]
        public double Tolerance = 2;

        [Configurable]
        public int CommandDecimals = 0;

        [Configurable]
        public OsbEasing FftEasing = OsbEasing.InExpo;

        public override void Generate()
        {
            if (StartTime == EndTime)
            {
                StartTime = (int)Beatmap.HitObjects.First().StartTime;
                EndTime = (int)Beatmap.HitObjects.Last().EndTime;
            }
            EndTime = Math.Min(EndTime, (int)AudioDuration);
            StartTime = Math.Min(StartTime, EndTime);

            var bitmap = GetMapsetBitmap(SpritePath);

            var positionKeyframes = new KeyframedValue<Vector2>[BarCount];
            for (var i = 0; i < BarCount; i++)
                positionKeyframes[i] = new KeyframedValue<Vector2>(null);

            var fftTimeStep = Beatmap.GetTimingPointAt(StartTime).BeatDuration / BeatDivisor;
            var fftOffset = fftTimeStep * 0.2;
            for (var time = (double)StartTime; time < EndTime; time += fftTimeStep)
            {
                var fft = GetFft(time + fftOffset, BarCount, null, FftEasing);
                for (var i = 0; i < BarCount; i++)
                {
                    var height = Radius + (float)Math.Log10(1 + fft[i] * LogScale) * Scale;

                    var angle = i * (Math.PI * 2) / BarCount;
                    var offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * height;

                    positionKeyframes[i].Add(time, Position + offset);
                }
            }

            var layer = GetLayer("Spectrum");
            var barScale = ((Math.PI * 2 * Radius) / BarCount) / bitmap.Width;
            for (var i = 0; i < BarCount; i++)
            {
                var keyframes = positionKeyframes[i];
                keyframes.Simplify2dKeyframes(Tolerance, h => h);

                var angle = i * (Math.PI * 2) / BarCount;
                var defaultPosition = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Radius;

                var bar = layer.CreateSprite(SpritePath, SpriteOrigin);
                bar.CommandSplitThreshold = 300;
                bar.ColorHsb(StartTime, (i * 360.0 / BarCount) + Random(-10.0, 10.0), 0.6 + Random(0.4), 1);
                if (SpriteScale.X == SpriteScale.Y)
                    bar.Scale(StartTime, barScale * SpriteScale.X);
                else bar.ScaleVec(StartTime, barScale * SpriteScale.X, barScale * SpriteScale.Y);
                bar.Rotate(StartTime, angle);
                bar.Additive(StartTime, EndTime);

                var hasMove = false;
                keyframes.ForEachPair(
                    (start, end) =>
                    {
                        hasMove = true;
                        bar.Move(start.Time, end.Time, start.Value, end.Value);
                    },
                    defaultPosition,
                    s => new Vector2((float)Math.Round(s.X, CommandDecimals), (float)Math.Round(s.Y, CommandDecimals))
                );
                if (!hasMove) bar.Move(StartTime, defaultPosition);
            }
        }
    }
}
