using OpenTK;
using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Graphics.Drawables;
using StorybrewEditor.Storyboarding;
using BrewLib.Graphics.Renderers;
using OpenTK.Graphics;

namespace StorybrewEditor.UserInterface.Drawables
{
    public class StoryboardDrawable : Drawable
    {
        public Vector2 MinSize => Vector2.Zero;
        public Vector2 PreferredSize => new Vector2(854, 480);

        private Project storyboard;
        private RenderStates linesRenderStates = new RenderStates();

        public double Time;
        public bool Clip = true;

        public StoryboardDrawable(Project storyboard)
        {
            this.storyboard = storyboard;
        }

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity = 1)
        {
            storyboard.DisplayTime = Time;
            if (Clip)
            {
                using (DrawState.Clip(bounds, camera))
                    storyboard.Draw(drawContext, camera, bounds, opacity);
            }
            else
            {
                storyboard.Draw(drawContext, camera, bounds, opacity);
                DrawState.Prepare(drawContext.Get<LineRenderer>(), camera, linesRenderStates)
                    .DrawSquare(new Vector3(bounds.Left, bounds.Top, 0), new Vector3(bounds.Right, bounds.Bottom, 0), Color4.Black);
            }
        }

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
