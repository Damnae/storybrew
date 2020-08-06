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

        private readonly Project project;
        private readonly RenderStates linesRenderStates = new RenderStates();

        public double Time;
        public bool Clip = true;
        public bool UpdateFrameStats;

        public StoryboardDrawable(Project project)
        {
            this.project = project;
        }

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity = 1)
        {
            project.DisplayTime = Time;
            if (Clip)
            {
                using (DrawState.Clip(bounds, camera))
                    project.Draw(drawContext, camera, bounds, opacity, UpdateFrameStats);
            }
            else
            {
                project.Draw(drawContext, camera, bounds, opacity, UpdateFrameStats);
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
