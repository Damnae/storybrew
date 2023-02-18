using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace StorybrewCommon.Util
{
#pragma warning disable CS1591
    public abstract class ObjectSerializer
    {
        public abstract bool CanSerialize(string typeName);
        public abstract void WriteValue(BinaryWriter writer, object value);
        public abstract object ReadValue(BinaryReader reader);
        public abstract string ToString(object value);
        public abstract object FromString(string value);

        static readonly List<ObjectSerializer> serializers = new List<ObjectSerializer>()
        {
            new SimpleObjectSerializer<int>(r => r.ReadInt32(), (w, v) => w.Write((int)v), v => int.Parse(v), v => ((int)v).ToString()),
            new SimpleObjectSerializer<float>(r => r.ReadSingle(), (w, v) => w.Write((float)v), v => float.Parse(v), v => ((float)v).ToString()),
            new SimpleObjectSerializer<double>(r => r.ReadDouble(), (w, v) => w.Write((double)v), v => double.Parse(v), v => ((double)v).ToString()),
            new SimpleObjectSerializer<string>(r => r.ReadString(), (w, v) => w.Write((string)v)),
            new SimpleObjectSerializer<bool>(r => r.ReadBoolean(), (w, v) => w.Write((bool)v), v => bool.Parse(v), v => v.ToString()),
            new SimpleObjectSerializer<Vector2>(r =>
            {
                var x = r.ReadSingle();
                var y = r.ReadSingle();
                return new Vector2(x, y);
            }, (w, v) =>
            {
                var vector = (Vector2)v;
                w.Write(vector.X);
                w.Write(vector.Y);
            }, v =>
            {
                var split = v.Split(',');
                return new Vector2(float.Parse(split[0]), float.Parse(split[1]));
            }, v =>
            {
                var vector = (Vector2)v;
                return vector.X.ToString() + "," + vector.Y.ToString();
            }), new SimpleObjectSerializer<Vector3>(r =>
            {
                var x = r.ReadSingle();
                var y = r.ReadSingle();
                var z = r.ReadSingle();
                return new Vector3(x, y, z);
            }, (w, v) =>
            {
                var vector = (Vector3)v;
                w.Write(vector.X);
                w.Write(vector.Y);
                w.Write(vector.Z);
            }, v =>
            {
                var split = v.Split(',');
                return new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
            }, v =>
            {
                var vector = (Vector3)v;
                return vector.X.ToString() + "," + vector.Y.ToString() + "," + vector.Z.ToString();
            }), new SimpleObjectSerializer<Color4>(r =>
            {
                var red = r.ReadByte();
                var green = r.ReadByte();
                var blue = r.ReadByte();
                var alpha = r.ReadByte();
                return new Color4(red, green, blue, alpha);
            }, (w, v) =>
            {
                var color = (Color4)v;
                w.Write((byte)(color.R * 255));
                w.Write((byte)(color.G * 255));
                w.Write((byte)(color.B * 255));
                w.Write((byte)(color.A * 255));
            }, v =>
            {
                var split = v.Split(',');
                return new Color4(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3]));
            }, v =>
            {
                var color = (Color4)v;
                return color.R.ToString() + "," + color.G.ToString() + "," + color.B.ToString() + "," + color.A.ToString();
            })
        };

        public static object Read(BinaryReader reader)
        {
            var typeName = reader.ReadString();
            if (typeName == string.Empty) return null;

            var serializer = GetSerializer(typeName);
            if (serializer == null) throw new NotSupportedException($"Cannot read objects of type {typeName}");

            return serializer.ReadValue(reader);
        }
        public static void Write(BinaryWriter writer, object value)
        {
            if (value == null)
            {
                writer.Write(string.Empty);
                return;
            }

            var typeName = value.GetType().FullName;

            var serializer = GetSerializer(typeName);
            if (serializer == null) throw new NotSupportedException($"Cannot write objects of type {typeName}");

            writer.Write(typeName);
            serializer.WriteValue(writer, value);
        }
        public static object FromString(string typeName, string value)
        {
            if (typeName == string.Empty) return null;

            var serializer = GetSerializer(typeName);
            if (serializer == null) throw new NotSupportedException($"Cannot read objects of type {typeName}");

            return serializer.FromString(value);
        }
        public static string ToString(Type type, object value)
        {
            if (value == null) return string.Empty;

            var typeName = value.GetType().FullName;

            var serializer = GetSerializer(typeName);
            if (serializer == null) throw new NotSupportedException($"Cannot write objects of type {typeName}");

            return serializer.ToString(value);
        }
        public static ObjectSerializer GetSerializer(string typeName)
        {
            foreach (var serializer in serializers) if (serializer.CanSerialize(typeName)) return serializer;
            return null;
        }
        public static bool Supports(string typeName) => GetSerializer(typeName) != null;
    }
    public class SimpleObjectSerializer<T> : ObjectSerializer
    {
        readonly Func<BinaryReader, object> read;
        readonly Action<BinaryWriter, object> write;
        readonly Func<string, object> fromString;
        readonly Func<object, string> toString;

        public SimpleObjectSerializer(Func<BinaryReader, object> read, Action<BinaryWriter, object> write, Func<string, object> fromString = null, Func<object, string> toString = null)
        {
            this.read = read;
            this.write = write;
            this.fromString = fromString;
            this.toString = toString;
        }

        public override bool CanSerialize(string typeName) => typeName == typeof(T).FullName;

        public override object ReadValue(BinaryReader reader) => read(reader);
        public override void WriteValue(BinaryWriter writer, object value) => write(writer, value);

        public override object FromString(string value) => fromString?.Invoke(value) ?? value;
        public override string ToString(object value) => toString?.Invoke(value) ?? (string)value;
    }
}