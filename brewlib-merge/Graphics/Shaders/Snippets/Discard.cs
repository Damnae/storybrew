namespace BrewLib.Graphics.Shaders.Snippets
{
    public class Discard : ShaderSnippet
    {
        public override void Generate(ShaderContext context)
        {
            base.Generate(context);
            context.FlowDependant(() => "discard;");
        }
    }
}