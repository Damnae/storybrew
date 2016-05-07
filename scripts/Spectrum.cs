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
        public override void Generate()
        {
            // Create a spectrum effect from 0 to 20 seconds, using the sprite located at sb/pl.png
            // 256 bars are created and updated every 1/8
            MakeSpectrum(0, 10000, "sb/pl.png", 256, 8);
        }

        private void MakeSpectrum(int tStart, int tEnd, string spritePath, int barCount, double beatDivisor)
        {
            var layer = GetLayer("Spectrum");
            var fftTimeStep = Beatmap.GetTimingPointAt(tStart).BeatDuration / beatDivisor;

            var bars = new OsbSprite[barCount];
            var barWidth = 640.0 / bars.Length;
            var imageWidth = 76;

            for (var i = 0; i < bars.Length; i++)
            {
                bars[i] = layer.CreateSprite(spritePath, OsbLayer.Background, OsbOrigin.CentreLeft);
                bars[i].Move(tStart, i * barWidth, 240);
                bars[i].ScaleVec(tStart, barWidth / imageWidth, 0);
                bars[i].Additive(tStart, tEnd);
            }

            for (var time = (double)tStart; time < tEnd; time += fftTimeStep)
            {
                var fft = GetFft(time + fftTimeStep * 0.2, bars.Length);
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
