using OpenTK;

namespace StorybrewCommon.Util
{
    public struct Affine2 : IEquatable<Affine2>
    {
        public static readonly Affine2 Identity = new Affine2(Vector3.UnitX, Vector3.UnitY);

        public Vector3 Row0;
        public Vector3 Row1;

        /// <summary>
        /// Column 1, Row 1
        /// </summary>
        public float M11 { get => Row0.X; set => Row0.X = value; }
        /// <summary>
        /// Column 2, Row 1
        /// </summary>
        public float M21 { get => Row0.Y; set => Row0.Y = value; }
        /// <summary>
        /// Column 3, Row 1
        /// </summary>
        public float M31 { get => Row0.Z; set => Row0.Z = value; }
        /// <summary>
        /// Column 1, Row 2
        /// </summary>
        public float M12 { get => Row1.X; set => Row1.X = value; }
        /// <summary>
        /// Column 2, Row 2
        /// </summary>
        public float M22 { get => Row1.Y; set => Row1.Y = value; }
        /// <summary>
        /// Column 3, Row 2
        /// </summary>
        public float M32 { get => Row1.Z; set => Row1.Z = value; }

        public bool IsTranslationOnly
            => Row0.X == 1 && Row0.Y == 0 &&
               Row1.X == 0 && Row1.Y == 1;

        public Affine2(Vector3 row0, Vector3 row1)
        {
            Row0 = row0;
            Row1 = row1;
        }

        public Affine2(Affine2 affine2)
        {
            Row0 = affine2.Row0;
            Row1 = affine2.Row1;
        }

        public void Translate(float x, float y)
        {
            Row0.Z += Row0.X * x + Row0.Y * y;
            Row1.Z += Row1.X * x + Row1.Y * y;
        }

        public void TranslateInverse(float x, float y)
        {
            Row0.Z += x;
            Row1.Z += y;
        }

        public void Scale(float x, float y)
        {
            Row0.X *= x;
            Row0.Y *= y;

            Row1.X *= x;
            Row1.Y *= y;
        }

        public void ScaleInverse(float x, float y)
        {
            Row0.X *= x;
            Row0.Y *= x;
            Row0.Z *= x;

            Row1.X *= y;
            Row1.Y *= y;
            Row1.Z *= y;
        }

        public void Rotate(float angle)
        {
            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);

            var row0 = Row0;
            var row1 = Row1;

            Row0.X = row0.X * cos + row0.Y * sin;
            Row0.Y = row0.X * -sin + row0.Y * cos;

            Row1.X = row1.X * cos + row1.Y * sin;
            Row1.Y = row1.X * -sin + row1.Y * cos;
        }

        public void RotateInverse(float angle)
        {
            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);

            Row0.X = cos * Row0.X + -sin * Row1.X;
            Row0.Y = cos * Row0.Y + -sin * Row1.Y;
            Row0.Z = cos * Row0.Z + -sin * Row1.Z;

            Row1.X = sin * Row0.X + cos * Row1.X;
            Row1.Y = sin * Row0.Y + cos * Row1.Y;
            Row1.Z = sin * Row0.Z + cos * Row1.Z;
        }

        public void Multiply(Affine2 other)
        {
            var row0 = Row0;
            var row1 = Row1;

            Row0.X = row0.X * other.M11 + row0.Y * other.M12;
            Row0.Y = row0.X * other.M21 + row0.Y * other.M22;
            Row0.Z = row0.X * other.M31 + row0.Y * other.M32 + row0.Z;

            Row1.X = row1.X * other.M11 + row1.Y * other.M12;
            Row1.Y = row1.X * other.M21 + row1.Y * other.M22;
            Row1.Z = row1.X * other.M31 + row1.Y * other.M32 + row1.Z;
        }

        public Vector2 Transform(in Vector2 vector)
            => new Vector2(Row0.X * vector.X + Row0.Y * vector.Y + Row0.Z, Row1.X * vector.X + Row1.Y * vector.Y + Row1.Z);

        public Vector2 TransformSeparate(in Vector2 vector)
            => new Vector2(Row0.X * vector.X + Row0.Z, Row1.Y * vector.Y + Row1.Z);

        public float TransformX(float value)
            => Row0.X * value + Row0.Z;

        public float TransformY(float value)
            => Row1.Y * value + Row1.Z;

        public bool Equals(Affine2 other) => Row0 == other.Row0 && Row1 == other.Row1;
        public static bool operator ==(Affine2 left, Affine2 right) => left.Equals(right);
        public static bool operator !=(Affine2 left, Affine2 right) => !(left == right);

        public override bool Equals(object other) => other is Affine2 affine && Equals(affine);
        public override int GetHashCode() => (Row0.GetHashCode() * 397) ^ Row1.GetHashCode();
        public override string ToString() => $"{Row0} {Row1}";
    }
}
