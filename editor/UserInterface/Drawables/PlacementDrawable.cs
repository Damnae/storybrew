using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Graphics.Drawables;
using BrewLib.Graphics.Renderers;
using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Storyboarding;
using StorybrewEditor.Storyboarding;
using StorybrewEditor.UserInterface.Components;
using System.Diagnostics;

namespace StorybrewEditor.UserInterface.Drawables
{
    public class PlacementDrawable : Drawable
    {
        public Vector2 MinSize => Vector2.Zero;
        public Vector2 PreferredSize => new Vector2(854, 480);

        public const float RingDistance = 120;

        public StoryboardSegment Segment { get; set; }
        public StoryboardTransform ParentTransform { get; set; }

        private readonly RenderStates linesRenderStates = new RenderStates();

        private StoryboardTransform transform;
        private float scaleFactor;
        private Vector2 offset;
        private Vector2 center;

        internal Vector2 Offset => offset;
        internal Vector2 Center => center;

        private readonly PlacementUi ui;

        internal PlacementDrawable(PlacementUi ui) : base()
        {
            this.ui = ui;
        }

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity = 1)
        {
            transform = Segment.BuildTransform(ParentTransform);
            scaleFactor = bounds.Height / 480;
            offset = new Vector2(bounds.Width*0.5f - 427 * scaleFactor, bounds.Height*0.5f - 240 * scaleFactor);
            center = StoryboardToScreen(transform.ApplyToPosition(Vector2.Zero));
            var top = StoryboardToScreen(transform.ApplyToPosition(Vector2.UnitY * -10000));
            var bottom = StoryboardToScreen(transform.ApplyToPosition(Vector2.UnitY * 10000));
            var left = StoryboardToScreen(transform.ApplyToPosition(Vector2.UnitX * -10000));
            var right = StoryboardToScreen(transform.ApplyToPosition(Vector2.UnitX * 10000));
            //center = transform.ApplyToPosition(Vector2.Zero);
            //var top = transform.ApplyToPosition(Vector2.UnitY * -10000);
            //var bottom = transform.ApplyToPosition(Vector2.UnitY * 10000);
            //var left = transform.ApplyToPosition(Vector2.UnitX * -10000);
            //var right = transform.ApplyToPosition(Vector2.UnitX * 10000);

            //var positionOffset = new Vector2(bounds.Width, bounds.Height)*0.5f;
            ////Debug.WriteLine(positionOffset);
            //center += offset;
            //top += offset;
            //bottom += offset;
            //left += offset;
            //right += offset;
            //center += positionOffset;
            //top += positionOffset;
            //bottom += positionOffset;
            //left += positionOffset;
            //right += positionOffset;

            var renderer = DrawState.Prepare(drawContext.Get<LineRenderer>(), camera, linesRenderStates);
            renderer.DrawSquare(new Vector3(bounds.Left, bounds.Top, 0), new Vector3(bounds.Right, bounds.Bottom, 0), Color4.DarkGray);

            renderer.DrawCircle(center + Vector2.One, RingDistance, Color4.Black);
            renderer.Draw(new Vector3(top + Vector2.One), new Vector3(bottom + Vector2.One), Color4.Black);
            renderer.Draw(new Vector3(left + Vector2.One), new Vector3(right + Vector2.One), Color4.Black);

            Color4 DrawColor = Color4.Blue;
            switch (ui.GetTransformType())
            {
                case PlacementUi.PlacementUITransformType.Scale:
                    DrawColor = Color4.Green;
                    break;
                case PlacementUi.PlacementUITransformType.Rotate:
                    DrawColor = Color4.Red;
                    break;
                default:break;
            }
            
            renderer.DrawCircle(center, RingDistance, DrawColor);
            renderer.Draw(new Vector3(top), new Vector3(bottom), Color4.Blue);
            renderer.Draw(new Vector3(left), new Vector3(right), Color4.Blue);
        }

        public Vector2 StoryboardToScreen(Vector2 position) => offset + position * scaleFactor;
        public Vector2 ScreenToStoryboard(Vector2 position) => position / scaleFactor - offset;
        public Vector2 ScreenToSegment(Vector2 position) => transform.ApplyToPositionInverse(ScreenToStoryboard(position));

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
