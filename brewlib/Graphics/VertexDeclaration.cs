using OpenTK.Graphics.OpenGL;
using System.Collections;
using System.Collections.Generic;

namespace BrewLib.Graphics
{
    public class VertexDeclaration : IEnumerable<VertexAttribute>
    {
        private static int nextId;
        public readonly int Id = nextId++;

        private VertexAttribute[] vertexAttributes;

        public VertexAttribute this[int index] => vertexAttributes[index];
        public int AttributeCount => vertexAttributes.Length;
        public int VertexSize { get; private set; }

        public VertexDeclaration(params VertexAttribute[] vertexAttributes)
        {
            this.vertexAttributes = vertexAttributes;

            VertexSize = 0;
            foreach (var attribute in vertexAttributes)
            {
                attribute.Offset = VertexSize;
                VertexSize += attribute.Size;
            }
        }

        public VertexAttribute GetAttribute(AttributeUsage usage)
        {
            foreach (var attribute in vertexAttributes)
                if (attribute.Usage == usage)
                    return attribute;
            return null;
        }

        public List<VertexAttribute> GetAttributes(AttributeUsage usage)
        {
            var attributes = new List<VertexAttribute>();
            foreach (var attribute in vertexAttributes)
                if (attribute.Usage == usage)
                    attributes.Add(attribute);
            return attributes;
        }

        public void ActivateAttributes(Shader shader)
        {
            foreach (var attribute in vertexAttributes)
            {
                var attributeLocation = shader.GetAttributeLocation(attribute.Name);
                if (attributeLocation >= 0)
                {
                    GL.EnableVertexAttribArray(attributeLocation);
                    GL.VertexAttribPointer(attributeLocation, attribute.ComponentCount, attribute.Type, attribute.Normalized, VertexSize, attribute.Offset);
                }
            }
        }

        public void DeactivateAttributes(Shader shader)
        {
            foreach (var attribute in vertexAttributes)
            {
                var attributeLocation = shader.GetAttributeLocation(attribute.Name);
                if (attributeLocation >= 0)
                    GL.DisableVertexAttribArray(attributeLocation);
            }
        }

        public override bool Equals(object other)
        {
            if (other == this) return true;

            var otherDeclaration = other as VertexDeclaration;
            if (otherDeclaration == null) return false;
            if (AttributeCount != otherDeclaration.AttributeCount) return false;
            for (var i = 0; i < AttributeCount; i++)
                if (!this[i].Equals(otherDeclaration[i]))
                    return false;

            return true;
        }

        #region Enumerable

        public IEnumerator<VertexAttribute> GetEnumerator() => ((IEnumerable<VertexAttribute>)vertexAttributes).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<VertexAttribute>)vertexAttributes).GetEnumerator();

        #endregion
    }
}
