﻿using OpenTK;
using OpenTK.Graphics;
using BrewLib.Graphics;
using BrewLib.Graphics.Drawables;
using StorybrewEditor.Storyboarding;
using System;
using BrewLib.Util;
using BrewLib.UserInterface;

namespace StorybrewEditor.UserInterface
{
    public class TimelineSlider : Slider
    {
        private static readonly Color4 tickBlue = new Color4(50, 128, 255, 225);
        private static readonly Color4 tickYellow = new Color4(255, 255, 0, 225);
        private static readonly Color4 tickRed = new Color4(255, 0, 0, 225);
        private static readonly Color4 tickViolet = new Color4(200, 0, 200, 225);
        private static readonly Color4 tickWhite = new Color4(255, 255, 255, 220);
        private static readonly Color4 tickMagenta = new Color4(144, 64, 144, 225);
        private static readonly Color4 tickGrey = new Color4(160, 160, 160, 225);

        private static readonly Color4 kiaiColor = new Color4(255, 146, 18, 140);
        private static readonly Color4 breakColor = new Color4(255, 255, 255, 140);
        private static readonly Color4 bookmarkColor = new Color4(58, 110, 170, 240);
        private static readonly Color4 repeatColor = new Color4(58, 110, 170, 80);

        private Sprite line;
        private Label beatmapLabel;

        private Project project;
        private float timeSpan;

        public int SnapDivisor = 4;
        public bool ShowHitObjects;

        private float dragStart;
        private float repeatStart;
        private float repeatEnd;
        public float RepeatStart => repeatStart;
        public float RepeatEnd => repeatEnd;
        public bool UpdateRepeat = false;

        public TimelineSlider(WidgetManager manager, Project project) : base(manager)
        {
            this.project = project;
            line = new Sprite()
            {
                Texture = DrawState.WhitePixel,
                ScaleMode = ScaleMode.Fill,
            };
            Add(beatmapLabel = new Label(manager)
            {
                StyleName = "timelineBeatmapName",
                Text = project.MainBeatmap.Name,
                AnchorFrom = BoxAlignment.BottomRight,
                AnchorTo = BoxAlignment.BottomRight,
            });
            StyleName = "timeline";

            project.OnMainBeatmapChanged += project_OnMainBeatmapChanged;
        }

        private void project_OnMainBeatmapChanged(object sender, EventArgs e)
        {
            beatmapLabel.Text = project.MainBeatmap.Name;
        }

        protected override void DrawBackground(DrawContext drawContext, float actualOpacity)
        {
            base.DrawBackground(drawContext, actualOpacity);

            var bounds = Bounds;
            var offset = new Vector2(bounds.Left, bounds.Top);
            var lineBottomY = ShowHitObjects ? bounds.Height * 0.7f : bounds.Height * 0.6f;
            var hitObjectsY = bounds.Height * 0.6f;
            var pixelSize = Manager.PixelSize;

            var currentTimingPoint = project.MainBeatmap.GetTimingPointAt((int)(Value * 1000));
            var targetTimeSpan = (SnapDivisor >= 2 ? SnapDivisor >= 8 ? 1 : 2 : 4) * (170 / (float)currentTimingPoint.Bpm);
            timeSpan = timeSpan + (targetTimeSpan - timeSpan) * 0.01f;

            var leftTime = (int)((Value - timeSpan) * 1000);
            var rightTime = (int)((Value + timeSpan) * 1000);
            var timeScale = bounds.Width / (rightTime - leftTime);
            var valueLength = MaxValue - MinValue;

            // Repeat
            if (repeatStart != repeatEnd)
            {
                line.Color = repeatColor;

                var startProgress = repeatStart / valueLength;
                var endProgress = repeatEnd / valueLength;

                var left = (float)Manager.SnapToPixel(offset.X + startProgress * bounds.Width);
                var right = (float)Manager.SnapToPixel(offset.X + endProgress * bounds.Width);
                if (right < left + pixelSize)
                    right = left + pixelSize;

                line.Draw(drawContext, Manager.Camera, new Box2(left, offset.Y, right, offset.Y + bounds.Height * 0.4f), actualOpacity);
            }

            // Kiai
            var inKiai = false;
            var kiaiStartTime = 0.0;

            line.Color = kiaiColor;
            foreach (var controlPoint in project.MainBeatmap.ControlPoints)
            {
                if (controlPoint.IsKiai == inKiai)
                    continue;

                if (inKiai)
                {
                    var startProgress = kiaiStartTime / valueLength;
                    var endProgress = (controlPoint.Offset * 0.001) / valueLength;

                    var kiaiLeft = (float)Manager.SnapToPixel(offset.X + startProgress * bounds.Width);
                    var kiaiRight = (float)Manager.SnapToPixel(offset.X + endProgress * bounds.Width);
                    if (kiaiRight < kiaiLeft + pixelSize)
                        kiaiRight = kiaiLeft + pixelSize;

                    line.Draw(drawContext, Manager.Camera, new Box2(kiaiLeft, offset.Y + bounds.Height * 0.3f, kiaiRight, offset.Y + bounds.Height * 0.4f), actualOpacity);
                }
                else kiaiStartTime = controlPoint.Offset * 0.001;
                inKiai = controlPoint.IsKiai;
            }

            // Breaks
            line.Color = breakColor;
            foreach (var osuBreak in project.MainBeatmap.Breaks)
            {
                var startProgress = (osuBreak.StartTime * 0.001) / valueLength;
                var endProgress = (osuBreak.EndTime * 0.001) / valueLength;

                var breakLeft = (float)Manager.SnapToPixel(offset.X + startProgress * bounds.Width);
                var breakRight = (float)Manager.SnapToPixel(offset.X + endProgress * bounds.Width);
                if (breakRight < breakLeft + pixelSize)
                    breakRight = breakLeft + pixelSize;

                line.Draw(drawContext, Manager.Camera, new Box2(breakLeft, offset.Y + bounds.Height * 0.3f, breakRight, offset.Y + bounds.Height * 0.4f), actualOpacity);
            }

            // Ticks
            var leftTimingPoint = project.MainBeatmap.GetTimingPointAt(leftTime);
            var timingPoints = project.MainBeatmap.TimingPoints.GetEnumerator();

            if (timingPoints.MoveNext())
            {
                var timingPoint = timingPoints.Current;
                while (timingPoint != null)
                {
                    var nextTimingPoint = timingPoints.MoveNext() ? timingPoints.Current : null;
                    if (timingPoint.Offset < leftTimingPoint.Offset)
                    {
                        timingPoint = nextTimingPoint;
                        continue;
                    }
                    if (timingPoint != leftTimingPoint && rightTime < timingPoint.Offset) break;

                    int tickCount = 0, beatCount = 0;
                    var step = timingPoint.BeatDuration / SnapDivisor;
                    var sectionStartTime = timingPoint.Offset;
                    var sectionEndTime = Math.Min(nextTimingPoint?.Offset ?? rightTime, rightTime);
                    if (timingPoint == leftTimingPoint)
                        while (leftTime < sectionStartTime)
                        {
                            sectionStartTime -= step;
                            tickCount--;
                            if (tickCount % SnapDivisor == 0)
                                beatCount--;
                        }

                    for (var time = sectionStartTime; time < sectionEndTime; time += step)
                    {
                        if (leftTime < time)
                        {
                            var tickColor = tickGrey;
                            var lineSize = new Vector2(pixelSize, bounds.Height * 0.3f);

                            var snap = tickCount % SnapDivisor;
                            if (snap == 0) tickColor = tickWhite;
                            else if ((snap * 2) % SnapDivisor == 0) { lineSize.Y *= 0.8f; tickColor = tickRed; }
                            else if ((snap * 3) % SnapDivisor == 0) { lineSize.Y *= 0.4f; tickColor = tickViolet; }
                            else if ((snap * 4) % SnapDivisor == 0) { lineSize.Y *= 0.4f; tickColor = tickBlue; }
                            else if ((snap * 6) % SnapDivisor == 0) { lineSize.Y *= 0.4f; tickColor = tickMagenta; }
                            else if ((snap * 8) % SnapDivisor == 0) { lineSize.Y *= 0.4f; tickColor = tickYellow; }
                            else lineSize.Y *= 0.4f;

                            if (snap != 0 || (tickCount == 0 && timingPoint.OmitFirstBarLine) || beatCount % timingPoint.BeatPerMeasure != 0)
                                lineSize.Y *= 0.5f;

                            var tickX = offset.X + (float)Manager.SnapToPixel((time - leftTime) * timeScale);
                            var tickOpacity = tickX > beatmapLabel.TextBounds.Left - 8 ? actualOpacity * 0.2f : actualOpacity;

                            drawLine(drawContext, new Vector2(tickX, offset.Y + lineBottomY), lineSize, tickColor, tickOpacity);
                        }
                        if (tickCount % SnapDivisor == 0)
                            beatCount++;
                        tickCount++;
                    }
                    timingPoint = nextTimingPoint;
                }
            }

            // HitObjects
            if (ShowHitObjects)
                foreach (var hitObject in project.MainBeatmap.HitObjects)
                    if (leftTime < hitObject.EndTime && hitObject.StartTime < rightTime)
                    {
                        var left = Math.Max(0, (hitObject.StartTime - leftTime) * timeScale);
                        var right = Math.Min((hitObject.EndTime - leftTime) * timeScale, bounds.Width);
                        var height = Math.Max(bounds.Height * 0.1f - pixelSize, pixelSize);

                        drawLine(drawContext,
                            offset + new Vector2((float)Manager.SnapToPixel(left - height / 2), hitObjectsY),
                            new Vector2((float)Manager.SnapToPixel(right - left + height), height),
                            hitObject.Color, actualOpacity);
                    }

            // Bookmarks
            foreach (var bookmark in project.MainBeatmap.Bookmarks)
            {
                var progress = (bookmark * 0.001f) / (MaxValue - MinValue);
                var topLineSize = new Vector2(pixelSize, bounds.Height * 0.3f);
                drawLine(drawContext, offset + new Vector2((float)Manager.SnapToPixel(progress * bounds.Width), bounds.Height * 0.1f), topLineSize, bookmarkColor, actualOpacity);

                if (leftTime < bookmark && bookmark < rightTime)
                {
                    var bottomLineSize = new Vector2(pixelSize, bounds.Height * 0.5f);
                    drawLine(drawContext, offset + new Vector2((float)Manager.SnapToPixel((bookmark - leftTime) * timeScale), lineBottomY), bottomLineSize, bookmarkColor, actualOpacity);
                }
            }

            // Current time (top)
            {
                var x = (float)Manager.SnapToPixel(Value / (MaxValue - MinValue) * bounds.Width);
                var lineSize = new Vector2(pixelSize, bounds.Height * 0.4f);

                if (repeatStart != repeatEnd)
                {
                    var startProgress = repeatStart / valueLength;
                    var endProgress = repeatEnd / valueLength;

                    var left = (float)Manager.SnapToPixel(startProgress * bounds.Width);
                    var right = (float)Manager.SnapToPixel(endProgress * bounds.Width);

                    drawLine(drawContext, offset + new Vector2(left - pixelSize, 0), lineSize, Color4.White, actualOpacity);
                    drawLine(drawContext, offset + new Vector2(x, 0), lineSize * 0.6f, Color4.White, actualOpacity);
                    drawLine(drawContext, offset + new Vector2(right + pixelSize, 0), lineSize, Color4.White, actualOpacity);
                }
                else
                {
                    drawLine(drawContext, offset + new Vector2(x - pixelSize, 0), lineSize, Color4.White, actualOpacity);
                    drawLine(drawContext, offset + new Vector2(x + pixelSize, 0), lineSize, Color4.White, actualOpacity);
                }
            }

            // Current time (bottom)
            {
                var centerX = (float)Math.Round(bounds.Width * 0.5);
                var lineSize = new Vector2(pixelSize, bounds.Height * 0.4f);
                drawLine(drawContext, offset + new Vector2(centerX - pixelSize, lineBottomY), lineSize, Color4.White, actualOpacity);
                drawLine(drawContext, offset + new Vector2(centerX + pixelSize, lineBottomY), lineSize, Color4.White, actualOpacity);
            }
        }

        private void drawLine(DrawContext drawContext, Vector2 position, Vector2 size, Color4 color, float opacity)
        {
            line.Color = color;
            line.Draw(drawContext, Manager.Camera, new Box2(position, position + size), opacity);
        }

        public void Scroll(float direction)
        {
            var time = Value * 1000.0;
            var timingPoint = project.MainBeatmap.GetTimingPointAt((int)time);

            var stepDuration = timingPoint.BeatDuration / SnapDivisor;
            time += stepDuration * direction;

            var steps = (time - timingPoint.Offset) / stepDuration;
            time = timingPoint.Offset + Math.Round(steps) * stepDuration;

            Value = (float)(time * 0.001);
        }

        public void Snap() => Scroll(0);

        protected override void DragStart()
        {
            if (!UpdateRepeat)
                return;

            dragStart = Value;
            repeatStart = dragStart;
            repeatEnd = dragStart;
        }

        protected override void DragUpdate()
        {
            if (!UpdateRepeat)
                return;

            var value = Value;
            if (value < dragStart)
            {
                repeatStart = value;
                repeatEnd = dragStart;
            }
            else
            {
                repeatStart = dragStart;
                repeatEnd = value;
            }
        }

        protected override void Layout()
        {
            base.Layout();
            beatmapLabel.Size = new Vector2(Size.X * 0.25f, Size.Y * 0.4f);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                project.OnMainBeatmapChanged -= project_OnMainBeatmapChanged;
                line.Dispose();
            }
            line = null;

            base.Dispose(disposing);
        }
    }
}
