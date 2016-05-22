using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Util;
using System;

namespace StorybrewScripts
{
    /// <summary>
    /// An example of a spectrum effect.
    /// </summary>
    public class Spectrum : StoryboardObjectGenerator
    {
        [Configurable]
        public int StartTime = 0;

        [Configurable]
        public int EndTime = 10000;

        [Configurable]
        public int BeatDivisor = 4;

        [Configurable]
        public int BarCount = 256;

        [Configurable]
        public string SpritePath = "sb/pl.png";

        [Configurable]
        public int SpriteWidth = 76;

        public override void Generate()
        {
            var layer = GetLayer("Spectrum");
            var fftTimeStep = Beatmap.GetTimingPointAt(StartTime).BeatDuration / BeatDivisor;

            var bars = new OsbSprite[BarCount];
            var barWidth = 640.0 / bars.Length;
            var imageWidth = SpriteWidth;

            for (var i = 0; i < bars.Length; i++)
            {
                bars[i] = layer.CreateSprite(SpritePath, OsbOrigin.CentreLeft);
                bars[i].Move(StartTime, i * barWidth, 240);
                bars[i].ScaleVec(StartTime, barWidth / imageWidth, 0);
                bars[i].Additive(StartTime, EndTime);
            }

            var fftOffset = fftTimeStep * 0.2;
            for (var time = (double)StartTime; time < EndTime; time += fftTimeStep)
            {
                var fft = GetFft(time + fftOffset, bars.Length);
                for (var i = 0; i < bars.Length; i++)
                {
                    var height = fft[i] * 2000 / imageWidth;
                    if (height < 0.01) height = 0;

                    var previousHeight = bars[i].ScaleAt(time).Y;
                    if (height == previousHeight)
                        continue;

                    if (height < previousHeight)
                        bars[i].ScaleVec(time, time + fftTimeStep,
                            barWidth / imageWidth, previousHeight,
                            barWidth / imageWidth, height);
                    else if (height != previousHeight)
                        bars[i].ScaleVec(time,
                            barWidth / imageWidth, height);
                }
            }
        }
    }
}
