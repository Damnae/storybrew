using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace StorybrewCommon.Util
{
    public abstract class ObjectSerializer
    {
        public abstract bool CanSerialize(string typeName);
        public abstract void WriteValue(BinaryWriter writer, object value);
        public abstract object ReadValue(BinaryReader reader);

        private static List<ObjectSerializer> serializers = new List<ObjectSerializer>()
        {
            new SimpleObjectSerializer<int>(r => r.ReadInt32(), (w, v) => w.Write((int)v)),
            new SimpleObjectSerializer<float>(r => r.ReadSingle(), (w, v) => w.Write((float)v)),
            new SimpleObjectSerializer<double>(r => r.ReadDouble(), (w, v) => w.Write((double)v)),
            new SimpleObjectSerializer<string>(r => r.ReadString(), (w, v) => w.Write((string)v)),
            new SimpleObjectSerializer<bool>(r => r.ReadBoolean(), (w, v) => w.Write((bool)v)),
            new SimpleObjectSerializer<Vector2>(r =>
            {
                var x = r.ReadSingle();
                var y = r.ReadSingle();
                return new Vector2(x, y);
            },
            (w, v) =>
            {
                var vector = (Vector2) v;
                w.Write(vector.X);
                w.Write(vector.Y);
            }),
            new SimpleObjectSerializer<Color4>(r =>
            {
                var red = r.ReadByte();
                var green = r.ReadByte();
                var blue = r.ReadByte();
                var alpha = r.ReadByte();
                return new Color4(red, green, blue, alpha);
            },
            (w, v) =>
            {
                var color = (Color4)v;
                w.Write((byte)(color.R * 255));
                w.Write((byte)(color.G * 255));
                w.Write((byte)(color.B * 255));
                w.Write((byte)(color.A * 255));
            }),
        };

        public static object Read(BinaryReader reader)
        {
            var typeName = reader.ReadString();
            if (typeName == string.Empty)
                return null;

            var serializer = GetSerializer(typeName);
            if (serializer == null)
                throw new NotSupportedException($"Cannot read objects of type {typeName}");

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
            if (serializer == null)
                throw new NotSupportedException($"Cannot write objects of type {typeName}");

            writer.Write(typeName);
            serializer.WriteValue(writer, value);
        }

        public static ObjectSerializer GetSerializer(string typeName)
        {
            foreach (var serializer in serializers)
                if (serializer.CanSerialize(typeName))
                    return serializer;

            return null;
        }

        public static bool Supports(string typeName)
            => GetSerializer(typeName) != null;
    }

    public class SimpleObjectSerializer<T> : ObjectSerializer
    {
        private Func<BinaryReader, object> read;
        private Action<BinaryWriter, object> write;

        public SimpleObjectSerializer(Func<BinaryReader, object> read, Action<BinaryWriter, object> write)
        {
            this.read = read;
            this.write = write;
        }

        public override bool CanSerialize(string typeName)
            => typeName == typeof(T).FullName;

        public override object ReadValue(BinaryReader reader)
            => read(reader);

        public override void WriteValue(BinaryWriter writer, object value)
            => write(writer, value);
    }
}
