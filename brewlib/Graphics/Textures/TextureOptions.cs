using BrewLib.Data;
using BrewLib.Util;
using Newtonsoft.Json.Linq;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Resources;

namespace BrewLib.Graphics.Textures
{
    public class TextureOptions : IEquatable<TextureOptions>
    {
        public static readonly TextureOptions Default = new TextureOptions();

        // Settings
        public bool Srgb = true;
        public bool GenerateMipmaps = false;

        // Parameters
        public int TextureLodBias = 0;
        public TextureMinFilter TextureMinFilter = TextureMinFilter.Linear;
        public TextureMagFilter TextureMagFilter = TextureMagFilter.Linear;
        public TextureWrapMode TextureWrapS = TextureWrapMode.ClampToEdge;
        public TextureWrapMode TextureWrapT = TextureWrapMode.ClampToEdge;

        public void ApplyParameters(TextureTarget target)
        {
            if (TextureLodBias != 0)
                GL.TexEnv(TextureEnvTarget.TextureFilterControl, TextureEnvParameter.TextureLodBias, TextureLodBias);

            GL.TexParameter(target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter);
            GL.TexParameter(target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter);
            GL.TexParameter(target, TextureParameterName.TextureWrapS, (int)TextureWrapS);
            GL.TexParameter(target, TextureParameterName.TextureWrapT, (int)TextureWrapT);
            DrawState.CheckError("applying texture parameters");
        }

        public bool Equals(TextureOptions other)
        {
            return Srgb == other.Srgb &&
                GenerateMipmaps == other.GenerateMipmaps &&
                TextureLodBias == other.TextureLodBias &&
                TextureMinFilter == other.TextureMinFilter &&
                TextureMagFilter == other.TextureMagFilter &&
                TextureWrapS == other.TextureWrapS &&
                TextureWrapT == other.TextureWrapT;
        }

        public override int GetHashCode()
            => TextureLodBias + (int)TextureMinFilter + (int)TextureMagFilter + (int)TextureWrapS + (int)TextureWrapT;

        public static string GetOptionsFilename(string textureFilename)
            => Path.Combine(Path.GetDirectoryName(textureFilename), Path.GetFileNameWithoutExtension(textureFilename) + "-opt.json");

        public static TextureOptions Load(string filename, ResourceContainer resourceContainer = null)
        {
            byte[] data;
            if (File.Exists(filename))
                data = File.ReadAllBytes(filename);
            else
                data = resourceContainer?.GetBytes(filename);
            if (data == null) throw new FileNotFoundException(filename);
            return load(data.ToJObject());
        }

        private static TextureOptions load(JObject data)
        {
            var options = new TextureOptions();
            parseFields(options, data);
            return options;
        }

        private static void parseFields(object obj, JToken data)
        {
            var type = obj.GetType();
            while (type != typeof(object))
            {
                var fields = type.GetFields();
                foreach (var field in fields)
                {
                    var fieldData = data[field.Name];
                    if (fieldData != null)
                    {
                        var fieldType = field.FieldType;
                        var parser = getFieldParser(fieldType);
                        if (parser != null)
                        {
                            var value = parser.Invoke(fieldData);
                            field.SetValue(obj, value);
                        }
                        else Trace.WriteLine($"No parser for {fieldType}");
                    }
                }
                type = type.BaseType;
            }
        }

        private static Func<JToken, object> getFieldParser(Type fieldType)
        {
            if (fieldType.IsEnum)
                return data => Enum.Parse(fieldType, data.Value<string>());

            while (fieldType != typeof(object))
            {
                if (fieldParsers.TryGetValue(fieldType, out Func<JToken, object> parser))
                    return parser;

                fieldType = fieldType.BaseType;
            }
            return null;
        }

        private static Dictionary<Type, Func<JToken, object>> fieldParsers = new Dictionary<Type, Func<JToken, object>>()
        {
            [typeof(string)] = (data) => data.Value<string>(),
            [typeof(float)] = (data) => data.Value<float>(),
            [typeof(double)] = (data) => data.Value<double>(),
            [typeof(int)] = (data) => data.Value<int>(),
            [typeof(bool)] = (data) => data.Value<bool>(),
        };
    }
}
