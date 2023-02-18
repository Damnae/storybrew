using BrewLib.Graphics;
using BrewLib.Graphics.Drawables;
using BrewLib.UserInterface;
using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using StorybrewCommon.Mapset;
using StorybrewEditor.Storyboarding;
using System;

namespace StorybrewEditor.UserInterface
{
    public class TimelineSlider : Slider
    {
        static readonly Color4 tickBlue = new Color4(50, 128, 255, 225);
        static readonly Color4 tickYellow = new Color4(255, 255, 0, 225);
        static readonly Color4 tickRed = new Color4(255, 0, 0, 225);
        static readonly Color4 tickViolet = new Color4(200, 0, 200, 225);
        static readonly Color4 tickWhite = new Color4(255, 255, 255, 220);
        static readonly Color4 tickMagenta = new Color4(144, 64, 144, 225);
        static readonly Color4 tickGrey = new Color4(160, 160, 160, 225);

        static readonly Color4 kiaiColor = new Color4(255, 146, 18, 140);
        static readonly Color4 breakColor = new Color4(255, 255, 255, 140);
        static readonly Color4 bookmarkColor = new Color4(58, 110, 170, 240);
        static readonly Color4 repeatColor = new Color4(58, 110, 170, 80);
        static readonly Color4 highlightColor = new Color4(255, 0, 0, 80);

        Sprite line;
        readonly Label beatmapLabel;

        readonly Project project;
        float timeSpan;

        public int SnapDivisor = 4;
        public bool ShowHitObjects;

        float dragStart;

        public float RepeatStart { get; set; }
        public float RepeatEnd { get; set; }

        double highlightStart, highlightEnd;

        public TimelineSlider(WidgetManager manager, Project project) : base(manager)
        {
            this.project = project;
            line = new Sprite
            {
                Texture = DrawState.WhitePixel,
                ScaleMode = ScaleMode.Fill
            };
            Add(beatmapLabel = new Label(manager)
            {
                StyleName = "timelineBeatmapName",
                Text = project.MainBeatmap.Name,
                AnchorFrom = BoxAlignment.BottomRight,
                AnchorTo = BoxAlignment.BottomRight
            });
            StyleName = "timeline";

            project.OnMainBeatmapChanged += project_OnMainBeatmapChanged;
        }

        public void Highlight(double startTime, double endTime)
        {
            highlightStart = startTime;
            highlightEnd = endTime;
        }
        public void ClearHighlight() => highlightStart = highlightEnd = 0;

        void project_OnMainBeatmapChanged(object sender, EventArgs e) => beatmapLabel.Text = project.MainBeatmap.Name;

        protected override void DrawBackground(DrawContext drawContext, float actualOpacity)
        {
            base.DrawBackground(drawContext, actualOpacity);

            var bounds = Bounds;
            var offset = new Vector2(bounds.Left, bounds.Top);
            var lineBottomY = ShowHitObjects ? bounds.Height * .7f : bounds.Height * .6f;
            var hitObjectsY = bounds.Height * .6f;
            var pixelSize = Manager.PixelSize;

            var currentTimingPoint = project.MainBeatmap.GetTimingPointAt((int)(Value * 1000));
            var targetTimeSpan = (SnapDivisor >= 2 ? SnapDivisor >= 8 ? 1 : 2 : 4) * (170 / (float)currentTimingPoint.BPM);
            timeSpan += (targetTimeSpan - timeSpan) * .01f;

            var leftTime = (int)((Value - timeSpan) * 1000);
            var rightTime = (int)((Value + timeSpan) * 1000);
            var timeScale = bounds.Width / (rightTime - leftTime);
            var valueLength = MaxValue - MinValue;

            // Repeat
            if (RepeatStart != RepeatEnd)
            {
                line.Color = repeatColor;

                var left = timeToXTop(RepeatStart);
                var right = timeToXTop(RepeatEnd);
                if (right < left + pixelSize) right = left + pixelSize;

                line.Draw(drawContext, Manager.Camera, new Box2(left, offset.Y, right, offset.Y + bounds.Height * .4f), actualOpacity);
            }

            // Kiai
            var inKiai = false;
            var kiaiStartTime = .0;

            line.Color = kiaiColor;
            foreach (var controlPoint in project.MainBeatmap.ControlPoints)
            {
                if (controlPoint.IsKiai == inKiai) continue;

                if (inKiai)
                {
                    var kiaiLeft = timeToXTop(kiaiStartTime);
                    var kiaiRight = timeToXTop(controlPoint.Offset * .001);
                    if (kiaiRight < kiaiLeft + pixelSize) kiaiRight = kiaiLeft + pixelSize;

                    line.Draw(drawContext, Manager.Camera, new Box2(kiaiLeft, offset.Y + bounds.Height * .3f, kiaiRight, offset.Y + bounds.Height * .4f), actualOpacity);
                }
                else kiaiStartTime = controlPoint.Offset * .001;
                inKiai = controlPoint.IsKiai;
            }

            // Breaks
            line.Color = breakColor;
            foreach (var osuBreak in project.MainBeatmap.Breaks)
            {
                var breakLeft = timeToXTop(osuBreak.StartTime * .001);
                var breakRight = timeToXTop(osuBreak.EndTime * .001);
                if (breakRight < breakLeft + pixelSize) breakRight = breakLeft + pixelSize;

                line.Draw(drawContext, Manager.Camera, new Box2(breakLeft, offset.Y + bounds.Height * .3f, breakRight, offset.Y + bounds.Height * .4f), actualOpacity);
            }

            // Effect / layer highlight
            line.Color = highlightColor;
            if (highlightStart != highlightEnd)
            {
                var left = timeToXTop(highlightStart * .001);
                var right = timeToXTop(highlightEnd * .001);
                line.Draw(drawContext, Manager.Camera, new Box2(left, offset.Y + bounds.Height * .1f, right, offset.Y + bounds.Height * .4f), actualOpacity);
            }

            // Ticks
            project.MainBeatmap.ForEachTick(leftTime, rightTime, SnapDivisor, (timingPoint, time, beatCount, tickCount) =>
            {
                var tickColor = tickGrey;
                var lineSize = new Vector2(pixelSize, bounds.Height * .3f);

                var snap = tickCount % SnapDivisor;
                if (snap == 0) tickColor = tickWhite;
                else if (snap * 2 % SnapDivisor == 0) { lineSize.Y *= .8f; tickColor = tickRed; }
                else if (snap * 3 % SnapDivisor == 0) { lineSize.Y *= .4f; tickColor = tickViolet; }
                else if (snap * 4 % SnapDivisor == 0) { lineSize.Y *= .4f; tickColor = tickBlue; }
                else if (snap * 6 % SnapDivisor == 0) { lineSize.Y *= .4f; tickColor = tickMagenta; }
                else if (snap * 8 % SnapDivisor == 0) { lineSize.Y *= .4f; tickColor = tickYellow; }
                else lineSize.Y *= .4f;

                if (snap != 0 || (tickCount == 0 && timingPoint.OmitFirstBarLine) || beatCount % timingPoint.BeatPerMeasure != 0)
                    lineSize.Y *= .5f;

                var tickX = offset.X + (float)Manager.SnapToPixel((time - leftTime) * timeScale);
                var tickOpacity = tickX > beatmapLabel.TextBounds.Left - 8 ? actualOpacity * .2f : actualOpacity;

                drawLine(drawContext, new Vector2(tickX, offset.Y + lineBottomY), lineSize, tickColor, tickOpacity);
            });

            // HitObjects
            if (ShowHitObjects) foreach (var hitObject in project.MainBeatmap.HitObjects) if (leftTime < hitObject.EndTime && hitObject.StartTime < rightTime)
                    {
                        var left = Math.Max(0, (hitObject.StartTime - leftTime) * timeScale);
                        var right = Math.Min((hitObject.EndTime - leftTime) * timeScale, bounds.Width);
                        var height = Math.Max(bounds.Height * .1f - pixelSize, pixelSize);

                        drawLine(drawContext, offset + new Vector2((float)Manager.SnapToPixel(left - height / 2), hitObjectsY),
                            new Vector2((float)Manager.SnapToPixel(right - left + height), height), hitObject.Color, actualOpacity);
                    }

            // Bookmarks
            foreach (var bookmark in project.MainBeatmap.Bookmarks)
            {
                var topLineSize = new Vector2(pixelSize, bounds.Height * .3f);
                drawLine(drawContext, new Vector2(timeToXTop(bookmark * .001f), offset.Y + bounds.Height * .1f), topLineSize, bookmarkColor, actualOpacity);

                if (leftTime < bookmark && bookmark < rightTime)
                {
                    var bottomLineSize = new Vector2(pixelSize, bounds.Height * .5f);
                    drawLine(drawContext, offset + new Vector2((float)Manager.SnapToPixel((bookmark - leftTime) * timeScale), lineBottomY), bottomLineSize, bookmarkColor, actualOpacity);
                }
            }

            // Current time (top)
            {
                var x = timeToXTop(Value);
                var lineSize = new Vector2(pixelSize, bounds.Height * .4f);

                if (RepeatStart != RepeatEnd)
                {
                    drawLine(drawContext, new Vector2(timeToXTop(RepeatStart) - pixelSize, offset.Y), lineSize, Color4.White, actualOpacity);
                    drawLine(drawContext, new Vector2(x, offset.Y), lineSize * .6f, Color4.White, actualOpacity);
                    drawLine(drawContext, new Vector2(timeToXTop(RepeatEnd) + pixelSize, offset.Y), lineSize, Color4.White, actualOpacity);
                }
                else
                {
                    drawLine(drawContext, new Vector2(x - pixelSize, offset.Y), lineSize, Color4.White, actualOpacity);
                    drawLine(drawContext, new Vector2(x + pixelSize, offset.Y), lineSize, Color4.White, actualOpacity);
                }
            }

            // Current time (bottom)
            {
                var centerX = (float)Math.Round(bounds.Width * .5);
                var lineSize = new Vector2(pixelSize, bounds.Height * .4f);
                drawLine(drawContext, offset + new Vector2(centerX - pixelSize, lineBottomY), lineSize, Color4.White, actualOpacity);
                drawLine(drawContext, offset + new Vector2(centerX + pixelSize, lineBottomY), lineSize, Color4.White, actualOpacity);
            }
        }
        float timeToXTop(double time)
        {
            var progress = (time - MinValue) / (MaxValue - MinValue);
            return (float)Manager.SnapToPixel(AbsolutePosition.X + progress * Width);
        }
        void drawLine(DrawContext drawContext, Vector2 position, Vector2 size, Color4 color, float opacity)
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

            Value = (float)(time * .001);
        }
        public void Snap() => Scroll(0);

        protected override void DragStart(MouseButton button)
        {
            if (button != MouseButton.Right) return;

            dragStart = Value;
            RepeatStart = dragStart;
            RepeatEnd = dragStart;
        }
        protected override void DragUpdate(MouseButton button)
        {
            if (button != MouseButton.Right) return;

            var value = Value;
            if (value < dragStart)
            {
                RepeatStart = value;
                RepeatEnd = dragStart;
            }
            else
            {
                RepeatStart = dragStart;
                RepeatEnd = value;
            }
        }
        protected override void Layout()
        {
            base.Layout();
            beatmapLabel.Size = new Vector2(Size.X * .25f, Size.Y * .4f);
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