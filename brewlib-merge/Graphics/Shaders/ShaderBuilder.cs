using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BrewLib.Graphics.Shaders
{
    public class ShaderBuilder
    {
        public VertexDeclaration VertexDeclaration;

        readonly ShaderContext Context = new ShaderContext();
        readonly ProgramScope ProgramScope = new ProgramScope();
        readonly ShaderPartScope VertexShaderScope = new ShaderPartScope("vs");
        readonly ShaderPartScope FragmentShaderScope = new ShaderPartScope("fs");

        public ShaderSnippet VertexShader, FragmentShader;
        public readonly ShaderVariable GlPosition, GlPointSize, GlPointCoord, GlFragColor, GlFragDepth;
        public int MinVersion = 110;

        public ShaderType AddStruct() => ProgramScope.AddStruct();
        public ShaderVariable AddUniform(string name, string shaderTypeName, int count = -1) => ProgramScope.AddUniform(Context, name, shaderTypeName, count);
        public ShaderVariable AddVarying(string shaderTypeName) => ProgramScope.AddVarying(Context, shaderTypeName);
        public ShaderVariable AddVertexVariable(string shaderTypeName) => VertexShaderScope.AddVariable(Context, shaderTypeName);
        public ShaderVariable AddFragmentVariable(string shaderTypeName) => FragmentShaderScope.AddVariable(Context, shaderTypeName);

        public ShaderBuilder(VertexDeclaration vertexDeclaration)
        {
            VertexDeclaration = vertexDeclaration;
            GlPosition = new ShaderVariable(Context, "gl_Position", "vec4");
            GlPointSize = new ShaderVariable(Context, "gl_PointSize", "float");
            GlPointCoord = new ShaderVariable(Context, "gl_PointCoord", "vec2");
            GlFragColor = new ShaderVariable(Context, "gl_FragColor", "vec4");
            GlFragDepth = new ShaderVariable(Context, "gl_FragDepth", "float");
        }

        public Shader Build(bool log = false)
        {
            Context.VertexDeclaration = VertexDeclaration;
            Context.MarkUsedVariables(() => FragmentShader.Generate(Context), GlPointSize, GlFragColor, GlFragDepth);

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
        string buildCommon()
        {
            var code = new StringBuilder();

            code.AppendLine($"#version {Math.Max(MinVersion, Math.Max(VertexShader.MinVersion, FragmentShader.MinVersion))}");

            var requiredExtensions = new HashSet<string>();
            foreach (var extensionName in VertexShader.RequiredExtensions) requiredExtensions.Add(extensionName);
            foreach (var extensionName in FragmentShader.RequiredExtensions) requiredExtensions.Add(extensionName);
            foreach (var extensionName in requiredExtensions) code.AppendLine($"#extension {extensionName} : enable");

            code.AppendLine("#ifdef GL_ES");
            code.AppendLine("    precision mediump float;");
            code.AppendLine("#endif");

            ProgramScope.DeclareTypes(code);
            ProgramScope.DeclareUniforms(code);
            ProgramScope.DeclareVaryings(code, Context);

            return code.ToString();
        }
        string buildVertexShader()
        {
            var code = new StringBuilder();

            // Attributes
            foreach (var attribute in VertexDeclaration) code.AppendLine($"attribute {attribute.ShaderTypeName} {attribute.Name};");

            VertexShader.GenerateFunctions(code);

            // Main function

            code.AppendLine("void main() {");
            ProgramScope.DeclareUnusedVaryingsAsVariables(code, Context);
            VertexShaderScope.DeclareVariables(code);
            Context.GenerateCode(code, () => VertexShader.Generate(Context));
            code.AppendLine("}");
            return code.ToString();
        }
        string buildFragmentShader()
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