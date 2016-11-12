namespace BrewLib.Graphics.Renderers
{
    public partial class SpriteRendererBuffered : SpriteRenderer
    {
        public const string DefaultVertexShaderCode =
              "attribute vec4 " + VertexAttribute.PositionAttributeName + ";\n"
            + "attribute vec4 " + VertexAttribute.ColorAttributeName + ";\n"
            + "attribute vec2 " + VertexAttribute.TextureCoordAttributeName + "0;\n"
            + "uniform mat4 " + CombinedMatrixUniformName + ";\n"
            + "varying vec4 v_color;\n"
            + "varying vec2 v_textureCoord;\n"
            + "\n"
            + "void main()\n"
            + "{\n"
            + "    v_color = " + VertexAttribute.ColorAttributeName + ";\n"
            + "    v_textureCoord = " + VertexAttribute.TextureCoordAttributeName + "0;\n"
            + "    gl_Position = " + CombinedMatrixUniformName + " * " + VertexAttribute.PositionAttributeName + ";\n"
            + "}\n";

        public const string DefaultFragmentShaderCode =
              "varying vec4 v_color;\n"
            + "varying vec2 v_textureCoord;\n"
            + "uniform sampler2D " + TextureUniformName + ";\n"
            + "void main()\n"
            + "{\n"
            + "   gl_FragColor = v_color * texture2D(" + TextureUniformName + ", v_textureCoord);\n"
            + "}";

        public static Shader CreateDefaultShader()
            => new Shader(DefaultVertexShaderCode, DefaultFragmentShaderCode);
    }
}
