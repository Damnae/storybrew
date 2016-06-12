using OpenTK;
using System;
using System.Drawing;

namespace StorybrewEditor.Graphics.Cameras
{
    public class CameraIso : CameraBase
    {
        private Vector3 target = new Vector3();
        public Vector3 Target
        {
            get { return target; }
            set
            {
                if (target == value) return;
                target = value;
                Invalidate();
            }
        }

        public CameraIso()
        {
            NearPlane = -1000;
            FarPlane = 1000;
        }

        protected override void Recalculate(out Matrix4 view, out Matrix4 projection, out Rectangle internalViewport, out Rectangle extendedViewport)
        {
            var screenViewport = Viewport;

            var distanceSqrt = (float)Math.Sqrt(1 / 3f);
            Direction = new Vector3(distanceSqrt, -distanceSqrt, -distanceSqrt);
            Position = target - Direction;
            Up = new Vector3(0, 0, 1);

            internalViewport = extendedViewport = screenViewport;
            projection = Matrix4.CreateOrthographicOffCenter(
                -screenViewport.Width / 2.0f,
                (screenViewport.Width / 2),
                -(screenViewport.Height / 2),
                screenViewport.Height / 2,
                NearPlane, FarPlane);
            view = Matrix4.LookAt(Position, Position + Direction, Up);
        }
    }
}
