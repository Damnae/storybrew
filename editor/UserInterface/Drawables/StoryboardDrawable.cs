using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Graphics.Drawables;
using BrewLib.Graphics.Renderers;
using OpenTK;
using OpenTK.Graphics;
using StorybrewEditor.Storyboarding;

namespace StorybrewEditor.UserInterface.Drawables
{
    public class StoryboardDrawable : Drawable
    {
        public Vector2 MinSize => Vector2.Zero;
        public Vector2 PreferredSize => new Vector2(854, 480);

        readonly Project project;
        readonly RenderStates linesRenderStates = new RenderStates();

        public double Time;
        public bool Clip = true, UpdateFrameStats;

        public StoryboardDrawable(Project project) => this.project = project;

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity = 1)
        {
            project.DisplayTime = Time;
            if (Clip) using (DrawState.Clip(bounds, camera)) project.Draw(drawContext, camera, bounds, opacity, UpdateFrameStats);
            else
            {
                project.Draw(drawContext, camera, bounds, opacity, UpdateFrameStats);
                DrawState.Prepare(drawContext.Get<LineRenderer>(), camera, linesRenderStates).DrawSquare(new Vector3(
                    bounds.Left, bounds.Top, 0), new Vector3(bounds.Right, bounds.Bottom, 0), Color4.Black);
            }
        }

        #region IDisposable Support

        bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing) { }
                disposed = true;
            }
        }
        public void Dispose() => Dispose(true);

        #endregion
    }
}