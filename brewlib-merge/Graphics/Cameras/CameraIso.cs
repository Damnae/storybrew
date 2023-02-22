using OpenTK;
using System;
using System.Drawing;

namespace BrewLib.Graphics.Cameras
{
    public class CameraIso : CameraBase
    {
        Vector3 target = Vector3.Zero;
        public Vector3 Target
        {
            get => target;
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
            Forward = new Vector3(distanceSqrt, -distanceSqrt, -distanceSqrt);
            Position = target - Forward;
            Up = DefaultUp;

            internalViewport = extendedViewport = screenViewport;
            projection = Matrix4.CreateOrthographicOffCenter(
                -screenViewport.Width / 2f, screenViewport.Width / 2f,
                -(screenViewport.Height / 2f), screenViewport.Height / 2f,
                NearPlane, FarPlane);

            view = Matrix4.LookAt(Position, Position + Forward, Up);
        }
    }
}