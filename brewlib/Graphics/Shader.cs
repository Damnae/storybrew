using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace BrewLib.Graphics
{
    public class Shader : IDisposable
    {
        private int vertexShaderId = -1;
        private int fragmentShaderId = -1;
        private int programId = -1;

        private bool isInitialized;
        private bool started;
        private string log = string.Empty;

        private Dictionary<string, Property<ActiveAttribType>> attributes;
        private Dictionary<string, Property<ActiveUniformType>> uniforms;

        public string Log
        {
            get
            {
                if (!isInitialized)
                    return log;

                if (log == string.Empty)
                    log = GL.GetProgramInfoLog(programId);

                return log;
            }
        }

        /// <summary>
        /// An unique id across all shaders, used for sorting
        /// </summary>
        public int SortId => programId;

        /// <summary>
        /// [requires: v2.0]
        /// </summary>
        public Shader(string vertexShaderCode, string fragmentShaderCode)
        {
            initialize(vertexShaderCode, fragmentShaderCode);

            if (isInitialized)
                Trace.WriteLine(string.IsNullOrWhiteSpace(log) ?
                    $"Shader {programId} initialized" :
                    $"Shader {programId} initialized:\n{log}");
            else
            {
                Dispose(true);
                throw new Exception($"Failed to initialize shader:\n\n{log}");
            }

            retrieveAttributes();
            retrieveUniforms();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            isInitialized = false;

            if (!disposing)
                return;

            if (started)
                End();

            if (programId != -1)
            {
                GL.DeleteProgram(programId);
                programId = -1;
            }

            if (vertexShaderId != -1)
            {
                GL.DeleteShader(vertexShaderId);
                vertexShaderId = -1;
            }

            if (fragmentShaderId != -1)
            {
                GL.DeleteShader(fragmentShaderId);
                fragmentShaderId = -1;
            }
        }

        public void Begin()
        {
            if (started) throw new InvalidOperationException("Already started");

            DrawState.ProgramId = programId;
            started = true;
        }

        public void End()
        {
            if (!started) throw new InvalidOperationException("Not started");

            started = false;
        }

        public int GetAttributeLocation(string name)
        {
            Property<ActiveAttribType> property;
            if (attributes.TryGetValue(name, out property))
                return property.Location;

            return -1;
        }

        public int GetUniformLocation(string name, int index = -1, string field = null)
        {
#if DEBUG
            if (!started) throw new InvalidOperationException("Not started, most likely an due to an attempt to set the uniform on the wrong shader");
#endif

            var identifier = GetUniformIdentifier(name, index, field);

            Property<ActiveUniformType> property;
            if (uniforms.TryGetValue(identifier, out property))
                return property.Location;

            throw new ArgumentException($"{identifier} isn't a valid uniform identifier");
        }

        public bool HasUniform(string name, int index = -1, string field = null)
            => uniforms.ContainsKey(GetUniformIdentifier(name, index, field));

        public static string GetUniformIdentifier(string name, int index, string field)
            => name + (index >= 0 ? "[" + index + "]" : string.Empty) + (field != null ? "." + field : string.Empty);

        private void initialize(string vertexShaderCode, string fragmentShaderCode)
        {
            if (string.IsNullOrEmpty(vertexShaderCode))
                throw new ArgumentException("vertexShaderCode");

            if (string.IsNullOrEmpty(vertexShaderCode))
                throw new ArgumentException("fragmentShaderCode");

            Dispose(true);

            vertexShaderId = compileShader(ShaderType.VertexShader, vertexShaderCode);
            fragmentShaderId = compileShader(ShaderType.FragmentShader, fragmentShaderCode);

            if (vertexShaderId == -1 || fragmentShaderId == -1)
                return;

            programId = linkProgram();

            isInitialized = programId != -1;
        }

        private int compileShader(ShaderType type, string code)
        {
            var id = GL.CreateShader(type);
            GL.ShaderSource(id, code);
            GL.CompileShader(id);

            int compileStatus;
            GL.GetShader(id, ShaderParameter.CompileStatus, out compileStatus);
            if (compileStatus == 0)
            {
                log += $"--- {type} ---\n{addLineExtracts(GL.GetShaderInfoLog(id), code)}";
                return -1;
            }

            return id;
        }

        private int linkProgram()
        {
            var id = GL.CreateProgram();
            GL.AttachShader(id, vertexShaderId);
            GL.AttachShader(id, fragmentShaderId);
            GL.LinkProgram(id);

            int linkStatus;
            GL.GetProgram(id, GetProgramParameterName.LinkStatus, out linkStatus);
            if (linkStatus == 0)
            {
                log += GL.GetProgramInfoLog(id);
                return -1;
            }

            return id;
        }

        private void retrieveAttributes()
        {
            int attributeCount;
            GL.GetProgram(programId, GetProgramParameterName.ActiveAttributes, out attributeCount);

            attributes = new Dictionary<string, Property<ActiveAttribType>>(attributeCount);
            for (int i = 0; i < attributeCount; i++)
            {
                int size;
                ActiveAttribType type;
                var name = GL.GetActiveAttrib(programId, i, out size, out type);
                var location = GL.GetAttribLocation(programId, name);
                attributes[name] = new Property<ActiveAttribType>(name, size, type, location);
            }
        }

        private void retrieveUniforms()
        {
            int uniformCount;
            GL.GetProgram(programId, GetProgramParameterName.ActiveUniforms, out uniformCount);

            uniforms = new Dictionary<string, Property<ActiveUniformType>>(uniformCount);
            for (int i = 0; i < uniformCount; i++)
            {
                int size;
                ActiveUniformType type;
                var name = GL.GetActiveUniform(programId, i, out size, out type);
                var location = GL.GetUniformLocation(programId, name);
                uniforms[name] = new Property<ActiveUniformType>(name, size, type, location);
            }
        }

        private string addLineExtracts(string log, string code)
        {
            var errorRegex = new Regex(@"^ERROR: (\d+):(\d+): ", RegexOptions.IgnoreCase);
            var splitCode = code.Replace("\r\n", "\n").Split('\n');

            var sb = new StringBuilder();
            foreach (var line in log.Split('\n'))
            {
                sb.AppendLine(line);

                var match = errorRegex.Match(line);
                if (match.Success)
                {
                    var character = int.Parse(match.Groups[1].Value);
                    var lineNumber = int.Parse(match.Groups[2].Value) - 1;

                    if (lineNumber > 0)
                    {
                        sb.Append("  ");
                        sb.AppendLine(splitCode[lineNumber - 1]);
                    }

                    sb.Append("> ");
                    sb.AppendLine(splitCode[lineNumber]);
                    sb.AppendLine(new string(' ', character + 2) + "^");
                }
            }
            return sb.ToString();
        }

        public override string ToString()
            => $"program:{programId} vs:{vertexShaderId} fs:{fragmentShaderId}";

        private struct Property<TType>
        {
            public string Name;
            public int Size;
            public TType Type;
            public int Location;

            public Property(string name, int size, TType type, int location)
            {
                Name = name;
                Size = size;
                Type = type;
                Location = location;
            }

            public override string ToString()
                => $"{Size}@{Location} {Type}x{Size}";
        }
    }
}
