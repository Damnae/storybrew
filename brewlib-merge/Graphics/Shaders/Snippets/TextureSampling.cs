namespace BrewLib.Graphics.Shaders.Snippets
{
    public class TextureSampling : Assign
    {
        public TextureSampling(ShaderVariable result, ShaderVariable sampler, ShaderVariable coord)
            : base(result, () => $"texture2D({sampler.Ref}, {coord.Ref})") { }
    }
}