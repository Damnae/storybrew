using OpenTK;
using System;
using System.Collections.Generic;

namespace StorybrewCommon.Curves
{
    [Serializable]
    public class CircleCurve : BaseCurve
    {
        private Vector2 startPoint;
        private Vector2 midPoint;
        private Vector2 endPoint;

        public override Vector2 StartPosition => startPoint;
        public override Vector2 EndPosition => endPoint;

        public CircleCurve(Vector2 startPoint, Vector2 midPoint, Vector2 endPoint)
        {
            this.startPoint = startPoint;
            this.midPoint = midPoint;
            this.endPoint = endPoint;
        }

        protected override void Initialize(List<Tuple<float, Vector2>> distancePosition, out double length)
        {
            var d = 2 * (startPoint.X * (midPoint.Y - endPoint.Y) + midPoint.X * (endPoint.Y - startPoint.Y) + endPoint.X * (startPoint.Y - midPoint.Y));
            var startPointLS = startPoint.LengthSquared;
            var midPointLS = midPoint.LengthSquared;
            var endPointLS = endPoint.LengthSquared;

            var centre = new Vector2(
                (startPointLS * (midPoint.Y - endPoint.Y) + midPointLS * (endPoint.Y - startPoint.Y) + endPointLS * (startPoint.Y - midPoint.Y)) / d,
                (startPointLS * (endPoint.X - midPoint.X) + midPointLS * (startPoint.X - endPoint.X) + endPointLS * (midPoint.X - startPoint.X)) / d
            );
            var radius = (startPoint - centre).Length;

            var startAngle = Math.Atan2(startPoint.Y - centre.Y, startPoint.X - centre.X);
            var midAngle = Math.Atan2(midPoint.Y - centre.Y, midPoint.X - centre.X);
            var endAngle = Math.Atan2(endPoint.Y - centre.Y, endPoint.X - centre.X);

            while (midAngle < startAngle) midAngle += 2 * Math.PI;
            while (endAngle < startAngle) endAngle += 2 * Math.PI;
            if (midAngle > endAngle) endAngle -= 2 * Math.PI;

            length = Math.Abs((endAngle - startAngle) * radius);
            var precision = (int)(length * 0.125f);

            for (int i = 1; i < precision; i++)
            {
                var progress = (double)i / precision;
                var angle = endAngle * progress + startAngle * (1 - progress);

                var position = new Vector2((float)(Math.Cos(angle) * radius), (float)(Math.Sin(angle) * radius)) + centre;
                distancePosition.Add(new Tuple<float, Vector2>((float)(progress * length), position));
            }
            distancePosition.Add(new Tuple<float, Vector2>((float)length, endPoint));
        }
    }
}
