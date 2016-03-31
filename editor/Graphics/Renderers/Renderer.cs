
namespace StorybrewEditor.Graphics.Renderers
{
    public interface Renderer
    {
        void BeginRendering();
        void EndRendering();

        void Flush(bool canBuffer = false);
    }
}
