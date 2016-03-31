using OpenTK.Graphics.OpenGL;

namespace StorybrewEditor.Graphics
{
    public class VertexAttribute
    {
        public const string PositionAttributeName = "a_position";
        public const string NormalAttributeName = "a_normal";
        public const string TextureCoordAttributeName = "a_textureCoord";
        public const string ColorAttributeName = "a_color";
        public const string BoneWeightAttributeName = "a_boneWeight";

        public string Name;
        public VertexAttribPointerType Type = VertexAttribPointerType.Float;
        public int ComponentSize = 4;
        public int ComponentCount = 1;
        public bool Normalized = false;
        public int Offset;
        public ArrayCap ClientStateCap;

        public int Size => ComponentCount * ComponentSize;

        public override string ToString()
        {
            return Name + " " + ComponentCount + "x " + Type;
        }

        public static VertexAttribute CreatePosition2d()
        {
            return new VertexAttribute() { Name = PositionAttributeName, ComponentCount = 2, ClientStateCap = ArrayCap.VertexArray };
        }

        public static VertexAttribute CreatePosition3d()
        {
            return new VertexAttribute() { Name = PositionAttributeName, ComponentCount = 3, ClientStateCap = ArrayCap.VertexArray };
        }

        public static VertexAttribute CreateNormal()
        {
            return new VertexAttribute() { Name = NormalAttributeName, ComponentCount = 3, ClientStateCap = ArrayCap.NormalArray };
        }

        public static VertexAttribute CreateTextureCoord(int index = 0)
        {
            return new VertexAttribute() { Name = TextureCoordAttributeName + index, ComponentCount = 2, ClientStateCap = ArrayCap.TextureCoordArray };
        }

        public static VertexAttribute CreateColor(bool packed)
        {
            return packed ?
                new VertexAttribute() { Name = ColorAttributeName, ComponentCount = 4, ComponentSize = 1, Type = VertexAttribPointerType.UnsignedByte, Normalized = true, ClientStateCap = ArrayCap.ColorArray } :
                new VertexAttribute() { Name = ColorAttributeName, ComponentCount = 4, ClientStateCap = ArrayCap.ColorArray };
        }

        public static VertexAttribute CreateBoneWeight(int index = 0)
        {
            return new VertexAttribute() { Name = BoneWeightAttributeName + index, ComponentCount = 2 };
        }

        public static VertexAttribute CreateVec4(string name, bool packed)
        {
            return packed ?
                new VertexAttribute() { Name = name, ComponentCount = 4, ComponentSize = 1, Type = VertexAttribPointerType.UnsignedByte, Normalized = true } :
                new VertexAttribute() { Name = name, ComponentCount = 4 };
        }
    }
}
