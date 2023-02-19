﻿using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using System;
using System.Linq;

namespace StorybrewScripts
{
    ///<summary> An example of a spectrum effect. </summary>
    class Spectrum : StoryboardObjectGenerator
    {
        [Group("Timing")]
        [Configurable] public int StartTime = 0;
        [Configurable] public int EndTime = 10000;
        [Configurable] public int BeatDivisor = 16;

        [Group("Sprite")]
        [Configurable] public string SpritePath = "sb/bar.png";
        [Configurable] public OsbOrigin SpriteOrigin = OsbOrigin.BottomLeft;
        [Configurable] public Vector2 SpriteScale = new Vector2(1, 100);

        [Group("Bars")]
        [Configurable] public Vector2 Position = new Vector2(0, 400);
        [Configurable] public float Width = 640;
        [Configurable] public int BarCount = 96;
        [Configurable] public int LogScale = 600;
        [Configurable] public OsbEasing FftEasing = OsbEasing.InExpo;
        [Configurable] public float MinimalHeight = 0.05f;

        [Group("Optimization")]
        [Configurable] public double Tolerance = 0.2;
        [Configurable] public int CommandDecimals = 1;
        [Configurable] public int FrequencyCutOff = 16000;

        protected override void Generate()
        {
            if (StartTime == EndTime && Beatmap.HitObjects.FirstOrDefault() != null)
            {
                StartTime = (int)Beatmap.HitObjects.First().StartTime;
                EndTime = (int)Beatmap.HitObjects.Last().EndTime;
            }
            EndTime = Math.Min(EndTime, (int)AudioDuration);
            StartTime = Math.Min(StartTime, EndTime);

            var bitmap = GetMapsetBitmap(SpritePath);

            var heightKeyframes = new KeyframedValue<float>[BarCount];
            for (var i = 0; i < BarCount; i++) heightKeyframes[i] = new KeyframedValue<float>();

            var timeStep = Beatmap.GetTimingPointAt(StartTime).BeatDuration / BeatDivisor;
            var offset = timeStep / 5;

            for (double time = StartTime; time < EndTime; time += timeStep)
            {
                var fft = GetFft(time + offset, BarCount, null, FftEasing, FrequencyCutOff);
                for (var i = 0; i < BarCount; i++)
                {
                    var height = (float)Math.Log10(1 + fft[i] * LogScale) * SpriteScale.Y / bitmap.Height;
                    if (height < MinimalHeight) height = MinimalHeight;

                    heightKeyframes[i].Add(time, height);
                }
            }

            var layer = GetLayer("Spectrum");
            var barWidth = Width / BarCount;
            for (var i = 0; i < BarCount; i++)
            {
                var keyframes = heightKeyframes[i];
                keyframes.Simplify1dKeyframes(Tolerance, h => h);

                var bar = layer.CreateSprite(SpritePath, SpriteOrigin, new Vector2(Position.X + i * barWidth, Position.Y));
                bar.CommandSplitThreshold = 300;
                bar.ColorHsb(StartTime, (i * 360f / BarCount) + Random(-10f, 10), .6f + Random(.4f), 1);
                bar.Additive(StartTime, EndTime);

                var scaleX = SpriteScale.X * barWidth / bitmap.Width;
                scaleX = (float)Math.Floor(scaleX * 10) / 10f;

                var hasScale = false;
                keyframes.ForEachPair((start, end) =>
                {
                    hasScale = true;
                    bar.ScaleVec(start.Time, end.Time, scaleX, start.Value, scaleX, end.Value);
                }, MinimalHeight, s => (float)Math.Round(s, CommandDecimals));
                if (!hasScale) bar.ScaleVec(StartTime, scaleX, MinimalHeight);
            }
        }
    }
}