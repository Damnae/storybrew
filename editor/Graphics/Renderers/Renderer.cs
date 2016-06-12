using StorybrewEditor.Graphics.Cameras;

namespace StorybrewEditor.Graphics.Renderers
{
    public interface Renderer
    {
        Camera Camera { get; set; }

        void BeginRendering();
        void EndRendering();

        void Flush(bool canBuffer = false);
    }
}
