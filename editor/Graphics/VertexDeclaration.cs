using OpenTK.Graphics.OpenGL;
using System;
using System.Collections;
using System.Collections.Generic;

namespace StorybrewEditor.Graphics
{
    public class VertexDeclaration : IEnumerable<VertexAttribute>
    {
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

        public void ActivateFpAttributes()
        {
            foreach (var attribute in vertexAttributes)
            {
                GL.EnableClientState(attribute.ClientStateCap);
                switch (attribute.ClientStateCap)
                {
                    case ArrayCap.VertexArray:
                        GL.VertexPointer(attribute.ComponentCount, (VertexPointerType)attribute.Type, VertexSize, attribute.Offset);
                        break;

                    case ArrayCap.TextureCoordArray:
                        GL.TexCoordPointer(attribute.ComponentCount, (TexCoordPointerType)attribute.Type, VertexSize, attribute.Offset);
                        break;

                    case ArrayCap.ColorArray:
                        GL.ColorPointer(attribute.ComponentCount, (ColorPointerType)attribute.Type, VertexSize, attribute.Offset);
                        break;

                    default:
                        throw new NotSupportedException(attribute.Type.ToString());
                }
                DrawState.CheckError("binding");
            }
        }

        public void DeactivateFpAttributes()
        {
            foreach (var attribute in vertexAttributes)
                GL.DisableClientState(attribute.ClientStateCap);
        }

        #region Enumerable

        public IEnumerator<VertexAttribute> GetEnumerator()
        {
            return ((IEnumerable<VertexAttribute>)vertexAttributes).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<VertexAttribute>)vertexAttributes).GetEnumerator();
        }

        #endregion
    }
}
