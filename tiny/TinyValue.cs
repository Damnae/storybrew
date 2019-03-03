using System;
using System.Globalization;
using System.IO;

namespace Tiny
{
    public enum TinyTokenType
    {
        Null,
        Boolean,
        Integer,
        Float,
        String,
        Object,
        Array,
        Invalid,
    }

    public class TinyValue : TinyToken
    {
        public const string BooleanTrue = "Yes";
        public const string BooleanFalse = "No";

        private readonly object value;
        private readonly TinyTokenType type;

        public override bool IsInline => true;
        public override bool IsEmpty => value == null;
        public override TinyTokenType Type => type;

        internal TinyValue(object value, TinyTokenType type)
        {
            this.value = value;
            this.type = type;

            switch (type)
            {
                case TinyTokenType.Object:
                case TinyTokenType.Array:
                case TinyTokenType.Invalid:
                    throw new ArgumentOutOfRangeException(nameof(type), type.ToString());
            }
        }

        public TinyValue(object value) : this(value, FindValueType(value)) { }

        public TinyValue(string value) : this(value, TinyTokenType.String) { }

        public TinyValue(bool value) : this(value, TinyTokenType.Boolean) { }
        public TinyValue(bool? value) : this(value, TinyTokenType.Boolean) { }

        public TinyValue(sbyte value) : this(value, TinyTokenType.Integer) { }
        public TinyValue(sbyte? value) : this(value, TinyTokenType.Integer) { }
        public TinyValue(byte value) : this(value, TinyTokenType.Integer) { }
        public TinyValue(byte? value) : this(value, TinyTokenType.Integer) { }
        public TinyValue(short value) : this(value, TinyTokenType.Integer) { }
        public TinyValue(short? value) : this(value, TinyTokenType.Integer) { }
        public TinyValue(ushort value) : this(value, TinyTokenType.Integer) { }
        public TinyValue(ushort? value) : this(value, TinyTokenType.Integer) { }
        public TinyValue(int value) : this(value, TinyTokenType.Integer) { }
        public TinyValue(int? value) : this(value, TinyTokenType.Integer) { }
        public TinyValue(uint value) : this(value, TinyTokenType.Integer) { }
        public TinyValue(uint? value) : this(value, TinyTokenType.Integer) { }
        public TinyValue(long? value) : this(value, TinyTokenType.Integer) { }
        public TinyValue(long value) : this(value, TinyTokenType.Integer) { }
        public TinyValue(ulong value) : this(value, TinyTokenType.Integer) { }
        public TinyValue(ulong? value) : this(value, TinyTokenType.Integer) { }

        public TinyValue(float value) : this(value, TinyTokenType.Float) { }
        public TinyValue(float? value) : this(value, TinyTokenType.Float) { }
        public TinyValue(double value) : this(value, TinyTokenType.Float) { }
        public TinyValue(double? value) : this(value, TinyTokenType.Float) { }
        public TinyValue(decimal value) : this(value, TinyTokenType.Float) { }
        public TinyValue(decimal? value) : this(value, TinyTokenType.Float) { }

        public override T Value<T>(object key)
        {
            if (key != null)
                throw new ArgumentException($"Key must be null, was {key}", "key");

            if (value is T typedValue)
                return typedValue;

            var targetType = typeof(T);
            if (type == TinyTokenType.Null)
            {
                if (targetType == typeof(TinyArray))
                    return (T)(object)new TinyArray();
                else if (targetType == typeof(TinyObject))
                    return (T)(object)new TinyObject();
            }

            if (targetType.IsEnum && (type == TinyTokenType.String || type == TinyTokenType.Integer))
            {
                if (value == null)
                    return default(T);

                return (T)Enum.Parse(targetType, value.ToString());
            }

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (value == null)
                    return default(T);

                targetType = Nullable.GetUnderlyingType(targetType);
            }

            return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        internal override void WriteInternal(TextWriter writer, TinyToken parent, int indentLevel)
        {
            if (indentLevel != 0)
                throw new InvalidOperationException();

            switch (type)
            {
                case TinyTokenType.Null:
                    writer.WriteLine();
                    break;
                case TinyTokenType.String:
                    writer.WriteLine("\"" + TinyUtil.EscapeString((string)value) + "\"");
                    break;
                case TinyTokenType.Integer:
                    writer.WriteLine(value?.ToString());
                    break;
                case TinyTokenType.Float:
                    if (value is float floatFloat)
                        writer.WriteLine(floatFloat.ToString(CultureInfo.InvariantCulture));
                    else if (value is double floatDouble)
                        writer.WriteLine(floatDouble.ToString(CultureInfo.InvariantCulture));
                    else if (value is decimal floatDecimal)
                        writer.WriteLine(floatDecimal.ToString(CultureInfo.InvariantCulture));
                    else if (value is string floatString)
                        writer.WriteLine(floatString);
                    else throw new InvalidDataException(value?.ToString());
                    break;
                case TinyTokenType.Boolean:
                    writer.WriteLine(((bool)value) ? BooleanTrue : BooleanFalse);
                    break;
                case TinyTokenType.Array:
                case TinyTokenType.Object:
                case TinyTokenType.Invalid:
                    // Should never happen :)
                    throw new InvalidDataException(type.ToString());
                default:
                    throw new NotImplementedException(type.ToString());
            }
        }

        public static TinyTokenType FindValueType(object value)
        {
            if (value == null)
                return TinyTokenType.Null;

            if (value is string)
                return TinyTokenType.String;

            if (value is sbyte || value is byte ||
                value is short || value is ushort ||
                value is int || value is uint ||
                value is long || value is ulong ||
                value is Enum)
                return TinyTokenType.Integer;

            if (value is float || value is double || value is decimal)
                return TinyTokenType.Float;

            if (value is bool)
                return TinyTokenType.Boolean;

            return TinyTokenType.Invalid;
        }

        public override string ToString() => $"{value} ({type})";
    }
}
