using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Mapset;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Util;
using System;
using System.Linq;

namespace StorybrewScripts
{
    public class Particles : StoryboardObjectGenerator
    {
        [Configurable]
        public string Path = "sb/particle.png";

        [Configurable]
        public int StartTime;

        [Configurable]
        public int EndTime;

        [Configurable]
        public double ParticleDuration = 2000;

        [Configurable]
        public double ParticleAmount = 16;

        [Configurable]
        public Vector2 StartPosition = new Vector2(-107, 0);

        [Configurable]
        public Vector2 EndPosition = new Vector2(747, 480);

        [Configurable]
        public bool RandomX = true;

        [Configurable]
        public bool RandomY;

        [Configurable]
        public OsbEasing Easing;

        [Configurable]
        public bool RandomEasing;

        [Configurable]
        public int FadeInDuration = 200;

        [Configurable]
        public int FadeOutDuration = 200;

        [Configurable]
        public Color4 Color = new Color4(1, 1, 1, 0.6f);

        [Configurable]
        public double StartScale = 0.1;

        [Configurable]
        public double EndScale = 1.0;

        [Configurable]
        public bool RandomScale;

        [Configurable]
        public double StartRotation;

        [Configurable]
        public double EndRotation;

        [Configurable]
        public bool RandomRotation;

        [Configurable]
        public OsbOrigin Origin = OsbOrigin.Centre;

        [Configurable]
        public bool Additive = true;

        public override void Generate()
        {
            if (StartTime == EndTime)
            {
                StartTime = (int)Beatmap.HitObjects.First().StartTime;
                EndTime = (int)Beatmap.HitObjects.Last().EndTime;
            }
            EndTime = Math.Min(EndTime, (int)AudioDuration);
            StartTime = Math.Min(StartTime, EndTime);

            var particleDuration = ParticleDuration > 0 ? ParticleDuration :
                Beatmap.GetTimingPointAt(StartTime).BeatDuration * 4;

            // This is an example of using a sprite pool.
            // Sprites using the same layer, path and origin can be reused as if they were multiple sprites.

            var layer = GetLayer("");
            using (var pool = new OsbSpritePool(layer, Path, Origin, (sprite, startTime, endTime) =>
            {
                // This action runs for every sprite created from the pool, after all of them are created (AFTER the for loop below).

                // It is intended to set states common to every sprite:
                // In this example, this handles cases where all sprites will have the same color / opacity / scale / rotation / additive mode.

                // Note that the pool is in a using block, this is necessary to run this action.

                if (Color.R < 1 || Color.G < 1 || Color.B < 1)
                    sprite.Color(startTime, Color);

                if (Color.A < 1)
                    sprite.Fade(startTime, Color.A);

                if (StartScale == EndScale && StartScale != 1)
                    sprite.Scale(startTime, StartScale);

                if (StartRotation == EndRotation && StartRotation != 0)
                    sprite.Rotate(startTime, MathHelper.DegreesToRadians(StartRotation));

                if (Additive)
                    sprite.Additive(startTime, endTime);
            }))
            {
                var timeStep = particleDuration / ParticleAmount;
                for (var startTime = (double)StartTime; startTime <= EndTime - particleDuration; startTime += timeStep)
                {
                    var endTime = startTime + particleDuration;

                    // This is where sprites are created from the pool.
                    // Commands here are specific to each sprite.

                    // Note that you must know for how long you are going to use the sprite:
                    // startTime being the start time of the earliest command, endTime the end time of the last command.

                    // Sprites must also be created in order (startTime keeps increasing in each loop iteration),
                    // or sprites won't be properly reused.
                    var sprite = pool.Get(startTime, endTime);

                    var easing = RandomEasing ? (OsbEasing)Random(1, 3) : Easing;

                    var startX = RandomX ? Random(StartPosition.X, EndPosition.X) : StartPosition.X;
                    var startY = RandomY ? Random(StartPosition.Y, EndPosition.Y) : StartPosition.Y;
                    var endX = RandomX ? startX : EndPosition.X;
                    var endY = RandomY ? startY : EndPosition.Y;
                    sprite.Move(easing, startTime, endTime, startX, startY, endX, endY);

                    if (FadeInDuration > 0 || FadeOutDuration > 0)
                    {
                        var fadeInTime = startTime + FadeInDuration;
                        var fadeOutTime = endTime - FadeOutDuration;
                        if (fadeOutTime < fadeInTime)
                            fadeInTime = fadeOutTime = (fadeInTime + fadeOutTime) / 2;

                        sprite.Fade(easing, startTime, Math.Max(startTime, fadeInTime), 0, Color.A);
                        sprite.Fade(easing, Math.Min(fadeOutTime, endTime), endTime, Color.A, 0);
                    }

                    if (StartScale != EndScale)
                        if (RandomScale)
                            sprite.Scale(easing, startTime, endTime, Random(StartScale, EndScale), Random(StartScale, EndScale));
                        else sprite.Scale(easing, startTime, endTime, StartScale, EndScale);

                    if (StartRotation != EndRotation)
                        if (RandomRotation)
                            sprite.Rotate(easing, startTime, endTime, MathHelper.DegreesToRadians(Random(StartRotation, EndRotation)), MathHelper.DegreesToRadians(Random(StartRotation, EndRotation)));
                        else sprite.Rotate(easing, startTime, endTime, MathHelper.DegreesToRadians(StartRotation), MathHelper.DegreesToRadians(EndRotation));
                }
            }
        }
    }
}
