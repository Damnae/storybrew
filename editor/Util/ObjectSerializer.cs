using System;
using System.Collections.Generic;
using System.IO;

namespace StorybrewEditor.Util
{
    public abstract class ObjectSerializer
    {
        public abstract bool CanSerialize(Type type);
        public abstract void WriteValue(BinaryWriter writer, object value);
        public abstract object ReadValue(BinaryReader reader);

        private static List<ObjectSerializer> serializers = new List<ObjectSerializer>()
        {
            new SimpleObjectSerializer<int>(r => r.ReadInt32(), (w, v) => w.Write((int)v)),
            new SimpleObjectSerializer<float>(r => r.ReadSingle(), (w, v) => w.Write((float)v)),
            new SimpleObjectSerializer<double>(r => r.ReadDouble(), (w, v) => w.Write((double)v)),
            new SimpleObjectSerializer<string>(r => r.ReadString(), (w, v) => w.Write((string)v)),
            new SimpleObjectSerializer<bool>(r => r.ReadBoolean(), (w, v) => w.Write((bool)v)),
        };

        public static object Read(BinaryReader reader)
        {
            var typeName = reader.ReadString();
            var type = Type.GetType(typeName);
            return GetSerializer(type).ReadValue(reader);
        }

        public static void Write(BinaryWriter writer, object value)
        {
            var type = value.GetType();
            writer.Write(type.FullName);
            GetSerializer(type).WriteValue(writer, value);
        }

        public static ObjectSerializer GetSerializer(Type type)
        {
            foreach (var serializer in serializers)
                if (serializer.CanSerialize(type))
                    return serializer;

            throw new NotSupportedException($"The type {type} isn't supported");
        }
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

        public override bool CanSerialize(Type type)
            => type == typeof(T);

        public override object ReadValue(BinaryReader reader)
            => read(reader);

        public override void WriteValue(BinaryWriter writer, object value)
            => write(writer, value);
    }
}
