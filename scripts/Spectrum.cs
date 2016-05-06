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
            MakeFft(0, 10000, "sb/pl.png");
        }

        private void MakeFft(int tStart, int tEnd, string spritePath)
        {
            var layer = GetLayer("fft");
            var fftTimeStep = Beatmap.GetTimingPointAt(tStart).BeatDuration / 8;

            var baseBars = new OsbSprite[256];
            var barWidth = 640.0 / baseBars.Length;
            var imageWidth = 76;

            for (var i = 0; i < baseBars.Length; i++)
            {
                baseBars[i] = layer.CreateSprite(spritePath, OsbLayer.Background, OsbOrigin.CentreLeft, new Vector2());
                baseBars[i].Move(tStart, i * barWidth, 240);
                baseBars[i].Additive(tStart, tEnd);
            }

            for (var time = (double)tStart; time < tEnd; time += fftTimeStep)
            {
                var fft = GetFft(time + fftTimeStep * 0.2, baseBars.Length);
                for (var i = 0; i < baseBars.Length; i++)
                {
                    var height = fft[i] * 2000 / imageWidth;
                    if (height < 0.01) height = 0;

                    var previousHeight = baseBars[i].ScaleAt(time).Y;
                    if (height == previousHeight)
                        continue;

                    if (height < previousHeight)
                        baseBars[i].ScaleVec(time, time + fftTimeStep,
                            barWidth / imageWidth, previousHeight,
                            barWidth / imageWidth, height);
                    else
                        baseBars[i].ScaleVec(time,
                            barWidth / imageWidth, height);
                }
            }
        }
    }
}
