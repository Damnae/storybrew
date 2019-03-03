using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tiny
{
    public abstract class TinyToken
    {
        public abstract bool IsInline { get; }
        public abstract bool IsEmpty { get; }
        public abstract TinyTokenType Type { get; }

        public static implicit operator TinyToken(string value) => new TinyValue(value);

        public static implicit operator TinyToken(bool value) => new TinyValue(value);
        public static implicit operator TinyToken(bool? value) => new TinyValue(value);

        public static implicit operator TinyToken(sbyte value) => new TinyValue(value);
        public static implicit operator TinyToken(sbyte? value) => new TinyValue(value);
        public static implicit operator TinyToken(byte value) => new TinyValue(value);
        public static implicit operator TinyToken(byte? value) => new TinyValue(value);
        public static implicit operator TinyToken(short value) => new TinyValue(value);
        public static implicit operator TinyToken(short? value) => new TinyValue(value);
        public static implicit operator TinyToken(ushort value) => new TinyValue(value);
        public static implicit operator TinyToken(ushort? value) => new TinyValue(value);
        public static implicit operator TinyToken(int value) => new TinyValue(value);
        public static implicit operator TinyToken(int? value) => new TinyValue(value);
        public static implicit operator TinyToken(uint value) => new TinyValue(value);
        public static implicit operator TinyToken(uint? value) => new TinyValue(value);
        public static implicit operator TinyToken(long? value) => new TinyValue(value);
        public static implicit operator TinyToken(long value) => new TinyValue(value);
        public static implicit operator TinyToken(ulong value) => new TinyValue(value);
        public static implicit operator TinyToken(ulong? value) => new TinyValue(value);

        public static implicit operator TinyToken(float value) => new TinyValue(value);
        public static implicit operator TinyToken(float? value) => new TinyValue(value);
        public static implicit operator TinyToken(double value) => new TinyValue(value);
        public static implicit operator TinyToken(double? value) => new TinyValue(value);
        public static implicit operator TinyToken(decimal value) => new TinyValue(value);
        public static implicit operator TinyToken(decimal? value) => new TinyValue(value);

        public abstract T Value<T>(object key);
        public T Value<T>() => Value<T>(null);

        public static TinyToken ToToken(object value)
        {
            if (value is TinyToken token)
                return token;

            if (TinyValue.FindValueType(value) == TinyTokenType.Invalid)
            {
                if (value is IDictionary dictionary)
                {
                    var o = new TinyObject();
                    foreach (var key in dictionary.Keys)
                        o.Add((string)key, ToToken(dictionary[key]));
                    return o;
                }

                if (value is IEnumerable enumerable)
                    return new TinyArray(enumerable);
            }

            return new TinyValue(value);
        }

        public static TinyToken Read(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return Read(reader);
        }

        public static TinyToken Read(TextReader reader)
        {
            var tokens = TinyTokenizer.Tokenize(reader);
            return TinyTokenParser.Parse(tokens);
        }

        public void Write(string path)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new StreamWriter(stream, Encoding.UTF8) { NewLine = "\n", })
                Write(writer);
        }

        public void Write(TextWriter writer) => WriteInternal(writer, null, 0);

        internal abstract void WriteInternal(TextWriter writer, TinyToken parent, int indentLevel);

        protected static void WriteIndent(TextWriter writer, int indentLevel)
        {
            if (indentLevel <= 0)
                return;

            writer.Write(new string(' ', indentLevel * 2));
        }
    }

    public static class TinyTokenExtensions
    {
        public static IEnumerable<T> Values<T>(this IEnumerable<TinyToken> tokens)
        {
            foreach (var token in tokens)
                yield return token.Value<T>();
        }

        public static IEnumerable<T> Values<T>(this TinyToken token, object key)
            => token.Value<TinyArray>(key).Values<T>();
    }
}
