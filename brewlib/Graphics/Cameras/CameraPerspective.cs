using OpenTK;
using System;
using System.Drawing;

namespace BrewLib.Graphics.Cameras
{
    public class CameraPerspective : CameraBase
    {
        private float fieldOfView;
        public float FieldOfView
        {
            get { return fieldOfView; }
            set
            {
                if (fieldOfView == value) return;
                fieldOfView = value;
                Invalidate();
            }
        }

        public CameraPerspective()
        {
            FieldOfView = 67;
            NearPlane = 0.001f;
            FarPlane = 1000;
        }

        protected override void Recalculate(out Matrix4 view, out Matrix4 projection, out Rectangle internalViewport, out Rectangle extendedViewport)
        {
            var screenViewport = Viewport;
            var fovRadians = (float)Math.Max(0.0001f, Math.Min(fieldOfView * Math.PI / 180, Math.PI - 0.0001f));
            var aspect = (float)screenViewport.Width / screenViewport.Height;

            internalViewport = extendedViewport = screenViewport;
            projection = Matrix4.CreatePerspectiveFieldOfView(fovRadians, aspect, NearPlane, FarPlane);
            view = Matrix4.LookAt(Position, Position + Forward, Up);
        }
    }
}
