using OpenTK.Graphics.OpenGL;

namespace BrewLib.Graphics
{
    public class VertexAttribute
    {
        public const string PositionAttributeName = "a_position";
        public const string NormalAttributeName = "a_normal";
        public const string TextureCoordAttributeName = "a_textureCoord";
        public const string ColorAttributeName = "a_color";
        public const string BoneWeightAttributeName = "a_boneWeight";
        public const string PresenceAttributeName = "a_presence";

        public string Name;
        public VertexAttribPointerType Type = VertexAttribPointerType.Float;
        public int ComponentSize = 4;
        public int ComponentCount = 1;
        public bool Normalized = false;
        public int Offset;
        public AttributeUsage Usage = AttributeUsage.Undefined;

        public string ShaderTypeName => ComponentCount == 1 ? "float" : "vec" + ComponentCount;
        public int Size => ComponentCount * ComponentSize;

        public override bool Equals(object other)
        {
            if (other == this) return true;

            var otherAttribute = other as VertexAttribute;
            if (otherAttribute == null) return false;
            if (Name != otherAttribute.Name) return false;
            if (Type != otherAttribute.Type) return false;
            if (ComponentSize != otherAttribute.ComponentSize) return false;
            if (ComponentCount != otherAttribute.ComponentCount) return false;
            if (Normalized != otherAttribute.Normalized) return false;
            if (Offset != otherAttribute.Offset) return false;
            if (Usage != otherAttribute.Usage) return false;

            return true;
        }

        public override string ToString()
            => $"{Name} {ComponentCount}x {Type} (used as {Usage})";

        public static VertexAttribute CreatePosition2d()
            => new VertexAttribute() { Name = PositionAttributeName, ComponentCount = 2, Usage = AttributeUsage.Position };

        public static VertexAttribute CreatePosition3d()
            => new VertexAttribute() { Name = PositionAttributeName, ComponentCount = 3, Usage = AttributeUsage.Position };

        public static VertexAttribute CreateNormal()
            => new VertexAttribute() { Name = NormalAttributeName, ComponentCount = 3, Usage = AttributeUsage.Normal };

        public static VertexAttribute CreateDiffuseCoord(int index = 0)
            => new VertexAttribute() { Name = TextureCoordAttributeName + index, ComponentCount = 2, Usage = AttributeUsage.DiffuseMapCoord };

        public static VertexAttribute CreateColor(bool packed)
            => packed ?
                new VertexAttribute() { Name = ColorAttributeName, ComponentCount = 4, ComponentSize = 1, Type = VertexAttribPointerType.UnsignedByte, Normalized = true, Usage = AttributeUsage.Color } :
                new VertexAttribute() { Name = ColorAttributeName, ComponentCount = 4, Usage = AttributeUsage.Color };

        public static VertexAttribute CreateBoneWeight(int index = 0)
            => new VertexAttribute() { Name = BoneWeightAttributeName + index, ComponentCount = 2, Usage = AttributeUsage.BoneWeight };

        public static VertexAttribute CreateVec4(string name, bool packed, AttributeUsage usage)
            => packed ?
                new VertexAttribute() { Name = name, ComponentCount = 4, ComponentSize = 1, Type = VertexAttribPointerType.UnsignedByte, Normalized = true, Usage = usage } :
                new VertexAttribute() { Name = name, ComponentCount = 4, Usage = usage };

        public static VertexAttribute CreatePresence()
            => new VertexAttribute() { Name = PresenceAttributeName, ComponentCount = 1, Usage = AttributeUsage.Presence };
    }

    public enum AttributeUsage
    {
        Undefined,
        Position,
        Color,
        Normal,
        DiffuseMapCoord,
        NormalMapCoord,
        BoneWeight,
        Presence
    }
}