using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BrewLib.Graphics.Shaders
{
    public class ShaderBuilder
    {
        public VertexDeclaration VertexDeclaration;

        private readonly ShaderContext Context = new ShaderContext();
        private readonly ProgramScope ProgramScope = new ProgramScope();
        private readonly ShaderPartScope VertexShaderScope = new ShaderPartScope("vs");
        private readonly ShaderPartScope FragmentShaderScope = new ShaderPartScope("fs");

        public ShaderSnippet VertexShader;
        public ShaderSnippet FragmentShader;
        public readonly ShaderVariable GlPosition;
        public readonly ShaderVariable GlFragColor;
        public readonly ShaderVariable GlFragDepth;

        public ShaderType AddStruct()
            => ProgramScope.AddStruct();

        /// <summary>
        /// Adds a shader input variable.
        /// </summary>
        public ShaderVariable AddUniform(string name, string shaderTypeName, int count = -1)
            => ProgramScope.AddUniform(Context, name, shaderTypeName, count);

        /// <summary>
        /// Adds a variable shared between the vertex and fragment shaders.
        /// </summary>
        public ShaderVariable AddVarying(string shaderTypeName)
            => ProgramScope.AddVarying(Context, shaderTypeName);

        /// <summary>
        /// Adds a variable to the vertex shaders.
        /// </summary>
        public ShaderVariable AddVertexVariable(string shaderTypeName)
            => VertexShaderScope.AddVariable(Context, shaderTypeName);

        /// <summary>
        /// Adds a variable to the fragment shaders.
        /// </summary>
        public ShaderVariable AddFragmentVariable(string shaderTypeName)
            => FragmentShaderScope.AddVariable(Context, shaderTypeName);

        public ShaderBuilder(VertexDeclaration vertexDeclaration)
        {
            VertexDeclaration = vertexDeclaration;
            GlPosition = new ShaderVariable(Context, "gl_Position", "vec4");
            GlFragColor = new ShaderVariable(Context, "gl_FragColor", "vec4");
            GlFragDepth = new ShaderVariable(Context, "gl_FragDepth", "float");
        }

        public Shader Build(bool log = false)
        {
            Context.VertexDeclaration = VertexDeclaration;
            Context.MarkUsedVariables(() => FragmentShader.Generate(Context), GlFragColor, GlFragDepth);

            var commonCode = buildCommon();
            var vertexShaderCode = buildVertexShader();
            var fragmentShaderCode = buildFragmentShader();

            if (log)
            {
                Trace.WriteLine("--- VERTEX ---");
                Trace.WriteLine(commonCode + vertexShaderCode);

                Trace.WriteLine("--- FRAGMENT ---");
                Trace.WriteLine(commonCode + fragmentShaderCode);
            }

            return new Shader(commonCode + vertexShaderCode, commonCode + fragmentShaderCode);
        }

        private string buildCommon()
        {
            var code = new StringBuilder();

            code.AppendLine($"#version {Math.Max(VertexShader.MinVersion, FragmentShader.MinVersion)}");

            var requiredExtensions = new HashSet<string>();
            foreach (var extensionName in VertexShader.RequiredExtensions)
                requiredExtensions.Add(extensionName);
            foreach (var extensionName in FragmentShader.RequiredExtensions)
                requiredExtensions.Add(extensionName);

            foreach (var extensionName in requiredExtensions)
                code.AppendLine($"#extension {extensionName} : enable");

            code.AppendLine("#ifdef GL_ES");
            code.AppendLine("    precision mediump float;");
            code.AppendLine("#endif");

            ProgramScope.DeclareTypes(code);
            ProgramScope.DeclareUniforms(code);
            ProgramScope.DeclareVaryings(code, Context);

            return code.ToString();
        }

        private string buildVertexShader()
        {
            var code = new StringBuilder();

            // Attributes
            foreach (var attribute in VertexDeclaration)
                code.AppendLine($"attribute {attribute.ShaderTypeName} {attribute.Name};");

            VertexShader.GenerateFunctions(code);

            // Main function

            code.AppendLine("void main() {");
            ProgramScope.DeclareUnusedVaryingsAsVariables(code, Context);
            VertexShaderScope.DeclareVariables(code);
            Context.GenerateCode(code, () => VertexShader.Generate(Context));
            code.AppendLine("}");
            return code.ToString();
        }

        private string buildFragmentShader()
        {
            var code = new StringBuilder();

            FragmentShader.GenerateFunctions(code);

            // Main function

            code.AppendLine("void main() {");
            FragmentShaderScope.DeclareVariables(code);
            Context.GenerateCode(code, () => FragmentShader.Generate(Context));
            code.AppendLine("}");
            return code.ToString();
        }
    }
}
