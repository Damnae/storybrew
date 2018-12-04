using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Mapset;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Util;
using System;

namespace StorybrewScripts
{
    /// <summary>
    /// An example script containing en entire storyboard.
    /// It is best to split yours into multiple effects, or it could take a long time to update in the editor.
    /// 
    /// To be used with https://osu.ppy.sh/s/183628
    /// </summary>
    public class Jigoku : StoryboardObjectGenerator
    {
        #region Timing

        public static int Offset = 0;
        public static int BeatDuration = 11715 - 11363;

        public static int TimeSbStart = Offset + 0;
        public static int TimeIntro = Offset + 774;
        public static int TimePart1 = Offset + 11363;
        public static int TimePart2 = Offset + 22657;
        public static int TimePart3 = Offset + 33951;
        public static int TimePart4 = Offset + 45245;
        public static int TimePart5 = Offset + 56539;
        public static int TimePart6 = Offset + 67833;
        public static int TimePart7 = Offset + 70657;
        public static int TimePart8 = Offset + 91127;
        public static int TimePart9 = Offset + 96068;
        public static int TimePart10 = Offset + 107362;
        public static int TimePart11 = Offset + 118657;
        public static int TimeEnd = Offset + 124127;
        public static int TimeSbEnd = Offset + 126774;

        #endregion

        public double BgScaling = 480.0 / 768;
        public double DegToRad = Math.PI / 180;

        private StoryboardLayer bgLayer;
        private StoryboardLayer mainLayer;
        private OsbSpritePools spritePools;

        public override void Generate()
        {
            bgLayer = GetLayer("Background");
            mainLayer = GetLayer("Main");

            using (spritePools = new OsbSpritePools(mainLayer))
            {
                Intro(TimeIntro, TimePart1);

                // Background
                var bg = bgLayer.CreateSprite("bg.jpg", OsbOrigin.Centre);
                bg.Scale(TimeSbStart, BgScaling);
                bg.Fade(TimePart1 - BeatDuration * 2, TimePart1, 0, 1);
                bg.ColorHsb(TimeSbStart, 0, 0, 0.3);
                bg.ColorHsb(TimePart2 - BeatDuration * 3 / 2, TimePart2 - BeatDuration / 2, 0, 0, 0.3, 0, 0, 0.6);
                bg.ColorHsb(OsbEasing.In, TimePart6 - BeatDuration * 4, TimePart6, 0, 0, 0.3, 0, 0, 0.3);
                bg.Fade(OsbEasing.In, TimePart3 - BeatDuration * 4, TimePart3, 1, 0);
                bg.ColorHsb(OsbEasing.In, TimePart4 - BeatDuration * 2, TimePart4, 0, 0, 0.6, 0, 0, 0.3);
                bg.Fade(OsbEasing.Out, TimePart4 - BeatDuration * 2, TimePart4, 0, 1);
                bg.Fade(OsbEasing.In, TimePart5 - BeatDuration * 4, TimePart5, 1, 0);
                bg.Fade(OsbEasing.Out, TimePart6, TimePart7, 0, 1);
                bg.Fade(OsbEasing.In, TimePart10 - BeatDuration * 4, TimePart10, 1, 0);

                Part1(TimePart1, TimePart2);
                Part2(TimePart2, TimePart3);
                Part3(TimePart3, TimePart4);
                Part4(TimePart4, TimePart5);
                Part5(TimePart5, TimePart6);
                Part6(TimePart6, TimePart7);
                Part7(TimePart7, TimePart8);
                Part8(TimePart8, TimePart9);
                Part9(TimePart9, TimePart10);
                Part10(TimePart10, TimePart11);
                Part11(TimePart11, TimeEnd);
                Outro(TimeEnd, TimeSbEnd);
            }
        }

        private void Intro(int tStart, int tEnd)
        {
            var bg = bgLayer.CreateSprite("sb/bgg.png", OsbOrigin.BottomCentre);
            bg.Scale(TimeSbStart, BgScaling);
            bg.Move(TimeSbStart, 320, 480);
            bg.Fade(OsbEasing.In, TimeSbStart, tStart, 0, 1);
            bg.Fade(tStart, tEnd, 1, 1);

            var times = new int[] { 1833, 2363, 4260, 4833, 6774 };
            var xs = new int[] { 500, 300, 400, 200, 100 };
            for (var i = 0; i < times.Length; ++i)
            {
                var t = times[i];
                var x = xs[i];
                MakeNote(t + Offset, x, 370, -Math.PI / 2, 0, 400, 2000);
            }

            for (var i = 0; i < 10; ++i)
            {
                var angle = -Math.PI * 2 / 6 + -Math.PI * i / 16;
                MakeNote(8671 + Offset + i * BeatDuration / 32, 100 + i * 50, 370, angle, 0, 400, 2000);
            }
        }
        private void Part1(int tStart, int tEnd)
        {
            // Piano
            var x = -100;
            var y = 330;
            var distance = 120;
            for (var i = 0; i < 3; ++i)
            {
                var angle = -Math.PI / 16 - Math.PI * i / 16;
                MakeNote(11363 + Offset + BeatDuration * i / 4, x, y, angle, distance);
            }
            distance += 50;
            for (var i = 0; i < 4; ++i)
            {
                var angle = -Math.PI / 2 + Math.PI * (i + 1) / 16;
                MakeNote(11627 + Offset + BeatDuration * i / 4, x, y, angle, distance);
            }
            distance += 50;
            for (var i = 0; i < 4; ++i)
            {
                var angle = -Math.PI / 16 - Math.PI * (i + 1) / 17;
                MakeNote(11980 + Offset + BeatDuration * i / 4, x, y, angle, distance);
            }
            distance += 50;
            for (var i = 0; i < 4; ++i)
            {
                var angle = -Math.PI / 4 + Math.PI * (i + 1) / 17;
                MakeNote(12377 + Offset + BeatDuration * i / 4, x, y, angle, distance);
            }
            distance += 50;
            for (var i = 0; i < 4; ++i)
            {
                var angle = -Math.PI / 16 - Math.PI * (i + 1) / 17;
                MakeNote(12730 + Offset + BeatDuration * i / 4, x, y, angle, distance);
            }
            distance += 50;
            for (var i = 0; i < 5; ++i)
            {
                var angle = -Math.PI / 2 + Math.PI * (i + 5) / 19;
                MakeNote(13083 + Offset + BeatDuration * i / 4, x, y, angle, distance);
            }

            // Drums
            x += 370;
            y -= 25;
            distance = 0;
            for (var i = 0; i < 4; ++i)
            {
                var angle = Random(-Math.PI);
                MakeNote(13480 + Offset + BeatDuration * i * 3 / 4, x, y, angle, distance, 400, 2000);
                MakeNote(13480 + Offset + BeatDuration * i * 3 / 4, x, y, angle + Math.PI, distance, 400, 2000);
            }

            // Piano (far)
            distance = 60;
            for (var i = 0; i < 24; ++i)
            {
                var angle = -Math.PI + Math.PI * i / 6;
                MakeNote(14186 + Offset + BeatDuration * i / 4, angle, distance);
            }

            // Drums
            distance = 0;
            for (var i = 0; i < 4; ++i)
            {
                var angle = Random(-Math.PI);
                MakeNote(16304 + Offset + BeatDuration * i * 3 / 4, 140, 340, angle, distance, 400, 2000);
                MakeNote(16304 + Offset + BeatDuration * i * 3 / 4, 140, 340, angle + Math.PI, distance, 400, 2000);
            }

            // Piano 
            distance = 120;
            for (var i = 0; i < 8; ++i)
            {
                var angle = -Math.PI + Math.PI * i / 8;
                MakeNote(17010 + Offset + BeatDuration * i / 4, angle, distance);
            }

            // Drums
            distance = 0;
            for (var i = 0; i < 3; ++i)
            {
                var angle = Random(-Math.PI);
                MakeNote(17715 + Offset + BeatDuration * i * 3 / 4, 140, 340, angle, distance, 400, 2000);
                MakeNote(17715 + Offset + BeatDuration * i * 3 / 4, 140, 340, angle + Math.PI, distance, 400, 2000);
            }
            for (var i = 0; i < 3; ++i)
            {
                var angle = Random(-Math.PI);
                MakeNote(18421 + Offset + BeatDuration * i * 3 / 4, 255, 240, angle, distance, 400, 2000);
                MakeNote(18421 + Offset + BeatDuration * i * 3 / 4, 255, 240, angle + Math.PI, distance, 400, 2000);
            }
            for (var i = 0; i < 4; ++i)
            {
                var angle = Random(-Math.PI);
                MakeNote(19127 + Offset + BeatDuration * i * 3 / 4, 120, 120, angle, distance, 400, 2000);
                MakeNote(19127 + Offset + BeatDuration * i * 3 / 4, 120, 120, angle + Math.PI, distance, 400, 2000);
            }

            // Piano
            x = 80;
            y = 220;
            distance = 60;
            for (var i = 0; i < 9; ++i)
            {
                var angle = Math.PI / 2 - Math.PI * (i - 9) / 8;
                MakeNote(19833 + Offset + BeatDuration * i / 3, x, y, angle, distance);
            }
            x = 130;
            y = 330;
            for (var i = 0; i < 6; ++i)
            {
                var angle = -Math.PI / 2 + Math.PI * i / 6;
                MakeNote(20892 + Offset + BeatDuration * i / 6, x, y, angle, distance);
            }
            for (var i = 0; i < 110; ++i)
            {
                var angle = Math.PI * i / 8;
                MakeNote(21245 + Offset + BeatDuration * i / 32, x, y, angle, distance * (1.2 - i * 0.01), 200, 260);
            }
        }

        private void Part2(int tStart, int tEnd)
        {
        }

        private void Part3(int tStart, int tEnd)
        {
            for (var i = 0; i < 4; ++i)
            {
                var t0 = 42421 + Offset + BeatDuration * i * 3 / 2;
                var t1 = t0 + BeatDuration * 3 / 2;

                var bg = mainLayer.CreateSprite("sb/jt" + i + ".png", OsbOrigin.Centre);
                bg.Scale(OsbEasing.Out, t0, t1, i % 2 == 0 ? 0.7 : 0.65, i % 2 == 0 ? 0.65 : 0.7);
                bg.Fade(OsbEasing.In, t0, t1, 1, 0);
            }
        }

        private void Part4(int tStart, int tEnd)
        {
            // Piano
            var x = -0.0;
            var y = 80.0;
            var distance = 180;
            for (var i = 0; i < 3; ++i)
            {
                var angle = Math.PI / 2 - Math.PI * i / 16;
                MakeNote(45245 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }
            x += 150;
            y += 350;
            for (var i = 0; i < 4; ++i)
            {
                var angle = -Math.PI / 2 - Math.PI * (i - 2) / 16;
                MakeNote(45510 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }
            x += 270;
            y -= 450;
            for (var i = 0; i < 4; ++i)
            {
                var angle = Math.PI / 2 - Math.PI * (i - 2) / 16;
                MakeNote(45862 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }
            x += 170;
            y += 550;
            for (var i = 0; i < 4; ++i)
            {
                var angle = -Math.PI / 2 - Math.PI * (i - 2) / 16;
                MakeNote(46215 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }
            x -= 150;
            y -= 450;
            for (var i = 0; i < 4; ++i)
            {
                var angle = Math.PI / 2 - Math.PI * (i - 2) / 16;
                MakeNote(46568 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }
            x -= 270;
            y += 450;
            for (var i = 0; i < 5; ++i)
            {
                var angle = -Math.PI / 2 - Math.PI * (i - 2) / 16;
                MakeNote(46921 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }

            // Orchestra
            x -= 100;
            y -= 140;
            distance = 0;
            for (var i = 0; i < 3; ++i)
            {
                var angle = Random(-Math.PI);
                MakeNote(47362 + Offset + BeatDuration * i * 3 / 4, x, y, angle, distance, 400, 2000);
                MakeNote(47362 + Offset + BeatDuration * i * 3 / 4, x, y, angle + Math.PI, distance, 400, 2000);
            }

            // Piano
            x = 600;
            y = 250;
            distance = -180;
            for (var i = 0; i < 3; ++i)
            {
                var angle = Math.PI / 2 - Math.PI * i / 13;
                MakeNote(48068 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }
            x -= 150;
            for (var i = 0; i < 4; ++i)
            {
                var angle = -Math.PI / 2 - Math.PI * (i - 2) / 13;
                MakeNote(48333 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }
            x -= 170;
            for (var i = 0; i < 4; ++i)
            {
                var angle = Math.PI / 2 - Math.PI * (i - 3) / 13;
                MakeNote(48686 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }
            x -= 170;
            for (var i = 0; i < 4; ++i)
            {
                var angle = Math.PI / 2 - Math.PI * (i + 4) / 13;
                MakeNote(49039 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }
            x += 150;
            for (var i = 0; i < 4; ++i)
            {
                var angle = Math.PI / 2 - Math.PI * (i - 5) / 13;
                MakeNote(49392 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }
            x += 270;
            for (var i = 0; i < 5; ++i)
            {
                var angle = -Math.PI / 2 - Math.PI * (i - 6) / 13;
                MakeNote(49745 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }

            // Drums
            x = 640 - 64;
            y -= 20;
            for (var i = 0; i < 5; ++i)
            {
                MakeNote(50186 + Offset + BeatDuration * i / 2, x - i * 128, y, -Math.PI / 2, distance, 500, 2000);
            }
            MakeNote(50892 + Offset, 64 + 16, y - distance, -Math.PI / 2 - Math.PI / 16, 0, 200, 600);
            MakeNote(50892 + Offset, 64 - 16, y - distance, -Math.PI / 2 + Math.PI / 16, 0, 200, 600);

            // Piano
            x = 64.0;
            y = 240.0;
            distance = 180;
            for (var i = 0; i < 3; ++i)
            {
                var angle = Math.PI / 2 + Math.PI * (i + 1) / 16;
                MakeNote(50892 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }
            x -= 250;
            y += 0;
            for (var i = 0; i < 4; ++i)
            {
                var angle = -Math.PI / 2 + Math.PI * (i + 5) / 16;
                MakeNote(51157 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }
            x += 470;
            y -= 200;
            for (var i = 0; i < 4; ++i)
            {
                var angle = Math.PI / 2 - Math.PI * (i - 4) / 16;
                MakeNote(51510 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }
            x += 170;
            y += 550;
            for (var i = 0; i < 4; ++i)
            {
                var angle = -Math.PI / 2 - Math.PI * (i - 2) / 16;
                MakeNote(51862 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }
            x -= 150;
            y -= 450;
            for (var i = 0; i < 4; ++i)
            {
                var angle = Math.PI / 2 - Math.PI * (i - 2) / 16;
                MakeNote(52215 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }
            x += 270;
            y += 250;
            for (var i = 0; i < 5; ++i)
            {
                var angle = -Math.PI / 2 - Math.PI * (i - 2) / 16;
                MakeNote(52568 + Offset + BeatDuration * i / 4, x, y, angle, distance, 200, 600);
            }

            // Orchestra
            x -= 100;
            y -= 140;
            distance = 0;
            for (var i = 0; i < 3; ++i)
            {
                var angle = Random(-Math.PI);
                MakeNote(53010 + Offset + BeatDuration * i * 3 / 4, x, y, angle, distance, 400, 2000);
                MakeNote(53010 + Offset + BeatDuration * i * 3 / 4, x, y, angle + Math.PI, distance, 400, 2000);
            }
            x -= 100;
            y -= 140;
            for (var i = 0; i < 3; ++i)
            {
                var angle = Random(-Math.PI);
                MakeNote(53715 + Offset + BeatDuration * i * 3 / 4, x, y, angle, distance, 400, 2000);
                MakeNote(53715 + Offset + BeatDuration * i * 3 / 4, x, y, angle + Math.PI, distance, 400, 2000);
            }
            x -= 100;
            y += 280;
            for (var i = 0; i < 3; ++i)
            {
                var angle = Random(-Math.PI);
                MakeNote(54421 + Offset + BeatDuration * i * 3 / 4, x, y, angle, distance, 400, 2000);
                MakeNote(54421 + Offset + BeatDuration * i * 3 / 4, x, y, angle + Math.PI, distance, 400, 2000);
            }
            x -= 200;
            y -= 140;
            for (var i = 0; i < 3; ++i)
            {
                var angle = Random(-Math.PI);
                MakeNote(55127 + Offset + BeatDuration * i * 3 / 4, x, y, angle, distance, 400, 2000);
                MakeNote(55127 + Offset + BeatDuration * i * 3 / 4, x, y, angle + Math.PI, distance, 400, 2000);
            }
            x -= 100;
            y -= 140;
            for (var i = 0; i < 3; ++i)
            {
                var angle = Random(-Math.PI);
                MakeNote(55833 + Offset + BeatDuration * i * 3 / 4, x, y, angle, distance, 400, 2000);
                MakeNote(55833 + Offset + BeatDuration * i * 3 / 4, x, y, angle + Math.PI, distance, 400, 2000);
            }
            x += 200;
            y += 60;
            {
                var angle = Random(-Math.PI);
                MakeNote(56010 + Offset, x, y, angle, distance, 400, 2000);
                MakeNote(56010 + Offset, x, y, angle + Math.PI, distance, 400, 1000);
            }
            x -= 100;
            y += 180;
            {
                var angle = Random(-Math.PI);
                MakeNote(56186 + Offset, x, y, angle, distance, 400, 2000);
                MakeNote(56186 + Offset, x, y, angle + Math.PI, distance, 400, 1000);
            }
            x += 300;
            y -= 140;
            {
                var angle = Random(-Math.PI);
                MakeNote(56362 + Offset, x, y, angle, distance, 400, 1000);
                MakeNote(56362 + Offset, x, y, angle + Math.PI, distance, 400, 1000);
            }
            x += 200;
            y += 140;
            {
                var angle = Random(-Math.PI);
                MakeNote(56539 + Offset, x, y, angle, distance, 400, 2000);
                MakeNote(56539 + Offset, x, y, angle + Math.PI, distance, 400, 2000);
            }
        }

        private void Part5(int tStart, int tEnd)
        {
            MakeCharacters(56539 + Offset, 1.0);
            MakeCharacters(59362 + Offset, 61833 + Offset, 1.05);
            MakeCharacters(62186 + Offset, 1.1);
            MakeCharacters(65010 + Offset, 1.15);
        }

        private void Part6(int tStart, int tEnd)
        {
        }

        private void Part7(int tStart, int tEnd)
        {
        }

        private void Part8(int tStart, int tEnd)
        {
            var x = 0.0;
            var y = 80.0;
            var distance = 0;

            // Drums
            for (var i = 0; i < 3; ++i)
            {
                var angle = Random(-Math.PI);
                MakeNote(91127 + Offset + BeatDuration * i * 3 / 4, 140, 340, angle, distance, 400, 2000);
                MakeNote(91127 + Offset + BeatDuration * i * 3 / 4, 140, 340, angle + Math.PI, distance, 400, 2000);
            }
            for (var i = 0; i < 3; ++i)
            {
                var angle = Random(-Math.PI);
                MakeNote(91833 + Offset + BeatDuration * i * 3 / 4, 255, 240, angle, distance, 400, 2000);
                MakeNote(91833 + Offset + BeatDuration * i * 3 / 4, 255, 240, angle + Math.PI, distance, 400, 2000);
            }
            for (var i = 0; i < 4; ++i)
            {
                var angle = Random(-Math.PI);
                MakeNote(92539 + Offset + BeatDuration * i * 3 / 4, 120, 120, angle, distance, 400, 2000);
                MakeNote(92539 + Offset + BeatDuration * i * 3 / 4, 120, 120, angle + Math.PI, distance, 400, 2000);
            }

            // Piano
            x = 80;
            y = 220;
            distance = 60;
            for (var i = 0; i < 9; ++i)
            {
                var angle = Math.PI / 2 - Math.PI * (i - 9) / 8;
                MakeNote(93245 + Offset + BeatDuration * i / 3, x, y, angle, distance);
            }
            x = 130;
            y = 330;
            for (var i = 0; i < 6; ++i)
            {
                var angle = -Math.PI / 2 + Math.PI * i / 6;
                MakeNote(94304 + Offset + BeatDuration * i / 6, x, y, angle, distance);
            }
            for (var i = 0; i < 64; ++i)
            {
                var angle = Math.PI * i / 8;
                MakeNote(94657 + Offset + BeatDuration * i / 32, x, y, angle, distance * (1.2 - i * 0.01), 200, 260);
            }
            for (var i = 0; i < 64; ++i)
            {
                var angle = Math.PI * i / 8;
                MakeNote(95362 + Offset + BeatDuration * i / 32, x, y, angle, distance * (1.2 + i * 0.01), 200, 260);
            }
        }

        private void Part9(int tStart, int tEnd)
        {
        }

        private void Part10(int tStart, int tEnd)
        {
        }

        private void Part11(int tStart, int tEnd)
        {
        }

        private void Outro(int tStart, int tEnd)
        {
        }

        private void MakeNote(int time, double angle, double distance)
        {
            MakeNote(time, 140, 340, angle, distance);
        }

        private void MakeNote(int time, double x, double y, double angle, double distance)
        {
            MakeNote(time, x, y, angle, distance, 200, 250);
        }

        private void MakeNote(int time, double x, double y, double angle, double distance, int inTime, int outTime)
        {
            var fallDistance = 400;

            var t0 = time - inTime;
            var t1 = time - inTime * 2 / 5;
            var t1b = time - Math.Min(200, inTime * 2 / 5);
            var t2 = time;
            var t3 = time + outTime / 5;
            var t4 = time + outTime;

            var x0 = x + Math.Cos(angle) * (distance + fallDistance);
            var y0 = y + Math.Sin(angle) * (distance + fallDistance);
            var x1 = x + Math.Cos(angle) * (distance);
            var y1 = y + Math.Sin(angle) * (distance);

            var lightX = x + Math.Cos(angle) * (distance - 70);
            var lightY = y + Math.Sin(angle) * (distance - 70);

            var hue1 = Random(340, 360);
            var hue2 = Random(240, 260);

            var note = spritePools.Get(t0, t3, "sb/pl.png", OsbOrigin.Centre, true);
            note.Move(OsbEasing.In, t0, t2, x0, y0, x1, y1);
            note.Fade(OsbEasing.Out, t0, t2, 0, 1);
            note.Fade(OsbEasing.In, t2, t3, 1, 0);
            note.Rotate(t0, angle);
            note.ScaleVec(OsbEasing.In, t0, t3, 0.8, 0.2, 1, 1);
            note.ColorHsb(t0, outTime >= 260 ? hue1 : hue2, 0.5, 1);

            var backNote = spritePools.Get(t1b, t4, "sb/pl.png", OsbOrigin.Centre, true);
            backNote.Move(OsbEasing.In, t1b, t2, lightX, lightY, x1, y1);
            backNote.Fade(OsbEasing.In, t1b, t2, 0, 1);
            backNote.Fade(OsbEasing.Out, t2, t4, 1, 0);
            backNote.Rotate(t1b, angle);
            backNote.ScaleVec(OsbEasing.Out, t1b, t2, 0, 0, 0.4, 0.8);
            backNote.ColorHsb(t1b, outTime >= 260 ? hue1 : hue2, 0.4, 1);
            backNote.ColorHsb(t2, outTime >= 260 ? hue2 : hue1, 0.4, 1);

            var light = spritePools.Get(t2, t4, "sb/l.png", OsbOrigin.CentreLeft, true);
            light.Move(t2, lightX, lightY);
            light.Fade(OsbEasing.Out, t2, t4, 1, 0);
            light.ScaleVec(OsbEasing.In, t2, t4, 1, 0.4, 1, 0.1);
            light.Rotate(t2, angle);
            light.ColorHsb(t2, outTime >= 260 ? hue1 : hue2, 0.4, 1);

            if (outTime >= 260)
                MakeNoteParticles(t2, x1, y1, angle, outTime / 1000.0);
        }

        private void MakeNoteParticles(int t, double x, double y, double angle, double effectStrengh)
        {
            int particleCount = 3 + (int)(Random(4, 12) * (effectStrengh * 0.8));
            for (var i = 0; i < particleCount; ++i)
            {
                var pt0 = t;
                var pt1 = pt0 + BeatDuration / 8 + Random(BeatDuration / 4);
                var pt2 = pt1 + BeatDuration / 6 + Random(BeatDuration / 3);
                var pt3 = pt2 + BeatDuration / 4 + Random(BeatDuration / 2);

                var pscale = Random(0.2, 0.4);

                var pStartAngle = angle + Math.PI / 2 + Math.PI / 8 - Random(Math.PI / 4);
                var pStartDistance = Random(-15.0, 15.0);
                var px0 = x + Math.Cos(pStartAngle) * pStartDistance;
                var py0 = y + Math.Sin(pStartAngle) * pStartDistance;

                var pangle0 = angle - Math.PI / 16 + Random(Math.PI / 8);
                var pdistance = (5 + Random(1.0) * Random(1.0) * Random(1.0) * 250) * (effectStrengh * 0.84);

                var px1 = px0 + Math.Cos(pangle0) * pdistance;
                var py1 = py0 + Math.Sin(pangle0) * pdistance;

                var pangle1 = pangle0 - Math.PI / 8 + Random(Math.PI / 4);
                pdistance *= 0.8;

                var px2 = px1 + Math.Cos(pangle1) * pdistance;
                var py2 = py1 + Math.Sin(pangle1) * pdistance;

                var pangle2 = pangle1 - Math.PI / 8 + Random(Math.PI / 4);
                pdistance *= 0.6;

                var px3 = px2 + Math.Cos(pangle2) * pdistance;
                var py3 = py2 + Math.Sin(pangle2) * pdistance;

                var squish = Random(1.2, 2.2);

                var particle = spritePools.Get(pt0, pt3, "sb/pl.png", OsbOrigin.Centre, true, 1);
                particle.MoveX((OsbEasing)Random(3), pt0, pt1, px0, px1);
                particle.MoveY((OsbEasing)Random(3), pt0, pt1, py0, py1);
                particle.MoveX((OsbEasing)Random(3), pt1, pt2, px1, px2);
                particle.MoveY((OsbEasing)Random(3), pt1, pt2, py1, py2);
                particle.MoveX((OsbEasing)Random(3), pt2, pt3, px2, px3);
                particle.MoveY((OsbEasing)Random(3), pt2, pt3, py2, py3);
                particle.Rotate(OsbEasing.In, pt0, pt1, pangle0, pangle1);
                particle.Rotate(OsbEasing.In, pt1, pt2, pangle1, pangle2);
                particle.Fade(pt0, 1);
                particle.Fade(pt3, 0);
                particle.ScaleVec(OsbEasing.In, pt0, pt3, pscale * squish, pscale / squish, 0, 0);
                particle.ColorHsb(pt0, Random(240, 260), Random(0.4, 0.8), Random(0.8, 1));
            }
        }

        private void MakeCharacters(int t, double baseScale)
        {
            MakeCharacters(t, -1, baseScale);
        }

        private void MakeCharacters(int t, int interruptTime, double baseScale)
        {
            var t0 = t;
            var t1 = t0 + BeatDuration * 3 / 2;
            var t2 = t1 + BeatDuration * 3 / 2;
            var t3 = t2 + BeatDuration * 3 / 2;
            var t4 = t + BeatDuration * 6;
            var t5 = t4 + BeatDuration * 2 * 7 / 8;

            var scale = BgScaling * baseScale;
            var x = 640 + 107 + 450 * (baseScale - 1);
            var y = 480 + 200 * (baseScale - 1);

            var c3 = spritePools.Get(t3, t5, "sb/c3.png", OsbOrigin.BottomRight);
            c3.Scale(t3, scale);
            c3.Move(t3, x - (401 + 309 + 281) * scale, y);
            c3.ColorHsb(OsbEasing.Out, t3, t4, 0, 1, 1, 0, 0, 1);
            if (interruptTime > 0)
            {
                c3.ColorHsb(OsbEasing.In, t4, interruptTime, 0, 0, 1, 0, 0, 0.5);
                c3.ColorHsb(interruptTime, 0, 0, 0);
            }
            else c3.ColorHsb(OsbEasing.In, t4, t5, 0, 0, 1, 0, 0, 0);

            var c2 = spritePools.Get(t2, t5, "sb/c2.png", OsbOrigin.BottomRight);
            c2.Scale(t2, scale);
            c2.Move(t2, x - (401 + 309) * scale, y);
            c2.ColorHsb(OsbEasing.Out, t2, t3, 0, 1, 1, 0, 0, 1);
            if (interruptTime > 0)
            {
                c2.ColorHsb(OsbEasing.In, t4, interruptTime, 0, 0, 1, 0, 0, 0.5);
                c2.ColorHsb(interruptTime, 0, 0, 0);
            }
            else c2.ColorHsb(OsbEasing.In, t4, t5, 0, 0, 1, 0, 0, 0);

            var c1 = spritePools.Get(t1, t5, "sb/c1.png", OsbOrigin.BottomRight);
            c1.Scale(t1, scale);
            c1.Move(t1, x - (401) * scale, y);
            c1.ColorHsb(OsbEasing.Out, t1, t2, 0, 1, 1, 0, 0, 1);
            if (interruptTime > 0)
            {
                c1.ColorHsb(OsbEasing.In, t4, interruptTime, 0, 0, 1, 0, 0, 0.5);
                c1.ColorHsb(interruptTime, 0, 0, 0);
            }
            else c1.ColorHsb(OsbEasing.In, t4, t5, 0, 0, 1, 0, 0, 0);

            var c0 = spritePools.Get(t0, t5, "sb/c0.png", OsbOrigin.BottomRight);
            c0.Scale(t0, scale);
            c0.Move(t0, x, y);
            c0.ColorHsb(OsbEasing.Out, t0, t1, 0, 1, 1, 0, 0, 1);
            if (interruptTime > 0)
            {
                c0.ColorHsb(OsbEasing.In, t4, interruptTime, 0, 0, 1, 0, 0, 0.5);
                c0.ColorHsb(interruptTime, 0, 0, 0);
            }
            else c0.ColorHsb(OsbEasing.In, t4, t5, 0, 0, 1, 0, 0, 0);
        }
    }
}
