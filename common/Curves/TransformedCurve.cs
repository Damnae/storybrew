﻿using OpenTK;

namespace StorybrewCommon.Curves
{
    [Serializable]
    public class TransformedCurve : Curve
    {
        private readonly Curve curve;
        private Vector2 offset;
        private readonly float scale;
        private readonly bool reversed;

        public Curve Curve => curve;

        public TransformedCurve(Curve curve, Vector2 offset, float scale, bool reversed = false)
        {
            this.curve = curve;
            this.offset = offset;
            this.scale = scale;
            this.reversed = reversed;
        }

        public Vector2 StartPosition => (reversed ? curve.EndPosition : curve.StartPosition) * scale + offset;
        public Vector2 EndPosition => (reversed ? curve.StartPosition : curve.EndPosition) * scale + offset;
        public double Length => curve.Length * scale;

        public Vector2 PositionAtDistance(double distance) => curve.PositionAtDistance(reversed ? curve.Length - distance : distance) * scale + offset;
        public Vector2 PositionAtDelta(double delta) => curve.PositionAtDelta(reversed ? 1.0 - delta : delta) * scale + offset;
    }
}
