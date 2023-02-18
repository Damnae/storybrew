namespace BrewLib.Graphics.Textures
{
    public interface BindableTexture
    {
        int TextureId { get; }
        TexturingModes TexturingMode { get; }
    }
}