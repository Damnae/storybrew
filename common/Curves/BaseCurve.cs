using OpenTK;
using System;
using System.Collections.Generic;

namespace StorybrewCommon.Curves
{
    [Serializable]
    public abstract class BaseCurve : Curve
    {
        public abstract Vector2 EndPosition { get; }
        public abstract Vector2 StartPosition { get; }

        private List<ValueTuple<float, Vector2>> distancePosition;

        private double length;
        public double Length
        {
            get
            {
                if (distancePosition == null) initialize();
                return length;
            }
        }

        private void initialize()
        {
            distancePosition = new List<ValueTuple<float, Vector2>>();
            Initialize(distancePosition, out length);
        }

        protected abstract void Initialize(List<ValueTuple<float, Vector2>> distancePosition, out double length);

        public Vector2 PositionAtDistance(double distance)
        {
            if (distancePosition == null) initialize();

            var previousDistance = 0.0f;
            var previousPosition = StartPosition;

            var nextDistance = length;
            var nextPosition = EndPosition;

            var i = 0;
            while (i < distancePosition.Count)
            {
                var distancePositionTuple = distancePosition[i];
                if (distancePositionTuple.Item1 > distance) break;

                previousDistance = distancePositionTuple.Item1;
                previousPosition = distancePositionTuple.Item2;
                i++;
            }

            if (i < distancePosition.Count - 1)
            {
                var distancePositionTuple = distancePosition[i + 1];
                nextDistance = distancePositionTuple.Item1;
                nextPosition = distancePositionTuple.Item2;
            }

            var delta = (distance - previousDistance) / (nextDistance - previousDistance);
            var previousToNext = nextPosition - previousPosition;

            return previousPosition + previousToNext * (float)delta;
        }

        public Vector2 PositionAtDelta(double delta)
            => PositionAtDistance(delta * Length);
    }
}
