using OpenTK;
using System;

namespace StorybrewCommon.Curves
{
    ///<summary> Represents any <see cref="StorybrewCommon.Curves.Curve"/> that has been transformed. </summary>
    [Serializable] public class TransformedCurve : Curve
    {
        readonly Curve curve;
        Vector2 offset;
        readonly float scale;
        readonly bool reversed;

        ///<summary> The transformed curve. </summary>
        public Curve Curve => curve;

        ///<summary> Constructs a transformed curve from <paramref name="curve"/> and given transformations. </summary>
        public TransformedCurve(Curve curve, Vector2 offset, float scale, bool reversed = false)
        {
            this.curve = curve;
            this.offset = offset;
            this.scale = scale;
            this.reversed = reversed;
        }

        ///<inheritdoc/>
        public Vector2 StartPosition => (reversed ? curve.EndPosition : curve.StartPosition) * scale + offset;

        ///<inheritdoc/>
        public Vector2 EndPosition => (reversed ? curve.StartPosition : curve.EndPosition) * scale + offset;

        ///<inheritdoc/>
        public double Length => curve.Length * scale;

        ///<inheritdoc/>
        public Vector2 PositionAtDistance(double distance) => curve.PositionAtDistance(reversed ? curve.Length - distance : distance) * scale + offset;
        
        ///<inheritdoc/>
        public Vector2 PositionAtDelta(double delta) => curve.PositionAtDelta(reversed ? 1.0 - delta : delta) * scale + offset;
    }
}