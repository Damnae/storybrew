using OpenTK;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Cameras;
using StorybrewEditor.Graphics.Drawables;
using StorybrewEditor.Storyboarding;

namespace StorybrewEditor.UserInterface.Drawables
{
    public class StoryboardDrawable : Drawable
    {
        public Vector2 MinSize => Vector2.Zero;
        public Vector2 PreferredSize => new Vector2(854, 480);

        private Project storyboard;

        public double Time;

        public StoryboardDrawable(Project storyboard)
        {
            this.storyboard = storyboard;
        }

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity = 1)
        {
            storyboard.DisplayTime = Time;
            using (DrawState.Clip(bounds, camera))
                storyboard.Draw(drawContext, camera, bounds, opacity);
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
