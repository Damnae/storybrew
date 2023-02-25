using BrewLib.Data;
using BrewLib.Util;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Tiny;
using Tiny.Formats.Json;

namespace BrewLib.Graphics.Textures
{
    public class TextureOptions : IEquatable<TextureOptions>
    {
        public static readonly TextureOptions Default = new TextureOptions();

        // Settings
        public bool Srgb = true;
        public bool PreMultiply = false;
        public bool GenerateMipmaps = false;

        // Parameters
        public int TextureLodBias = 0;
        public TextureMinFilter TextureMinFilter = TextureMinFilter.Linear;
        public TextureMagFilter TextureMagFilter = TextureMagFilter.Linear;
        public TextureWrapMode TextureWrapS = TextureWrapMode.ClampToEdge;
        public TextureWrapMode TextureWrapT = TextureWrapMode.ClampToEdge;

        public void ApplyParameters(TextureTarget target)
        {
            if (TextureLodBias != 0) GL.TexEnv(TextureEnvTarget.TextureFilterControl, TextureEnvParameter.TextureLodBias, TextureLodBias);

            GL.TexParameter(target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter);
            GL.TexParameter(target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter);
            GL.TexParameter(target, TextureParameterName.TextureWrapS, (int)TextureWrapS);
            GL.TexParameter(target, TextureParameterName.TextureWrapT, (int)TextureWrapT);
            DrawState.CheckError("applying texture parameters");
        }
        public void WithBitmap(Bitmap bitmap, Action<Bitmap> action)
        {
            if (PreMultiply) using (var pinned = BitmapHelper.Premultiply(bitmap)) action(pinned.Bitmap);
            else action(bitmap);
        }
        public bool Equals(TextureOptions other) => Srgb == other.Srgb &&
            GenerateMipmaps == other.GenerateMipmaps &&
            TextureLodBias == other.TextureLodBias &&
            TextureMinFilter == other.TextureMinFilter && TextureMagFilter == other.TextureMagFilter &&
            TextureWrapS == other.TextureWrapS && TextureWrapT == other.TextureWrapT;

        public override int GetHashCode()
            => TextureLodBias + (int)TextureMinFilter + (int)TextureMagFilter + (int)TextureWrapS + (int)TextureWrapT;

        public static string GetOptionsFilename(string textureFilename)
            => Path.Combine(Path.GetDirectoryName(textureFilename), Path.GetFileNameWithoutExtension(textureFilename) + "-opt.json");

        public static TextureOptions Load(string filename, ResourceContainer resourceContainer = null)
        {
            TinyToken token = null;
            if (File.Exists(filename)) token = TinyToken.Read(filename);
            else
            {
                var data = resourceContainer?.GetString(filename, ResourceSource.Embedded);
                if (data != null) token = TinyToken.ReadString<JsonFormat>(data);
            }

            return token != null ? load(token) : null;
        }
        static TextureOptions load(TinyToken data)
        {
            var options = new TextureOptions();
            parseFields(options, data);
            return options;
        }
        static void parseFields(object obj, TinyToken data)
        {
            var type = obj.GetType();
            while (type != typeof(object))
            {
                var fields = type.GetFields();
                foreach (var field in fields)
                {
                    var fieldType = field.FieldType;
                    var fieldData = data.Value<TinyToken>(field.Name);
                    if (fieldData != null)
                    {
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
        static Func<TinyToken, object> getFieldParser(Type fieldType)
        {
            if (fieldType.IsEnum) return (data) => Enum.Parse(fieldType, data.Value<string>());

            while (fieldType != typeof(object))
            {
                if (fieldParsers.TryGetValue(fieldType, out var parser)) return parser;
                fieldType = fieldType.BaseType;
            }
            return null;
        }
        static readonly Dictionary<Type, Func<TinyToken, object>> fieldParsers = new Dictionary<Type, Func<TinyToken, object>>
        {
            [typeof(string)] = (data) => data.Value<string>(),
            [typeof(float)] = (data) => data.Value<float>(),
            [typeof(double)] = (data) => data.Value<double>(),
            [typeof(int)] = (data) => data.Value<int>(),
            [typeof(bool)] = (data) => data.Value<bool>()
        };
    }
}