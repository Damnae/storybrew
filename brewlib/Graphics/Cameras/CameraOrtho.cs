using OpenTK;
using System;
using System.Drawing;

namespace BrewLib.Graphics.Cameras
{
    public class CameraOrtho : CameraBase
    {
        public static CameraOrtho Default = new CameraOrtho();

        private bool yDown;

        private int virtualWidth;
        public int VirtualWidth
        {
            get { return virtualWidth; }
            set
            {
                if (virtualWidth == value) return;
                virtualWidth = value;
                Invalidate();
            }
        }

        private int virtualHeight;
        public int VirtualHeight
        {
            get { return virtualHeight; }
            set
            {
                if (virtualHeight == value) return;
                virtualHeight = value;
                Invalidate();
            }
        }

        private float zoom = 1;
        public float Zoom
        {
            get { return zoom; }
            set
            {
                if (zoom == value) return;
                zoom = value;
                Invalidate();
            }
        }

        public float HeightScaling => VirtualHeight != 0 ? (float)Viewport.Height / VirtualHeight : 1;

        public CameraOrtho(bool yDown = true)
            : this(0, 0, yDown)
        {
        }

        public CameraOrtho(int virtualWidth, int virtualHeight, bool yDown = true)
        {
            this.virtualWidth = virtualWidth;
            this.virtualHeight = virtualHeight;
            this.yDown = yDown;

            Up = new Vector3(0, yDown ? -1 : 1, 0);
            Forward = new Vector3(0, 0, yDown ? 1 : -1);

            NearPlane = -1;
            FarPlane = 1;
        }

        protected override void Recalculate(out Matrix4 view, out Matrix4 projection, out Rectangle internalViewport, out Rectangle extendedViewport)
        {
            var screenViewport = Viewport;
            var orthoViewport = screenViewport;

            if (virtualHeight != 0)
            {
                var scale = screenViewport.Height == 0 ? 1.0 : (double)virtualHeight / screenViewport.Height;
                orthoViewport.Width = (int)Math.Round(screenViewport.Width * scale);
                orthoViewport.Height = virtualHeight;
                if (virtualWidth > 0) orthoViewport.X += (orthoViewport.Width - virtualWidth) / 2;

                internalViewport = new Rectangle(0, 0, virtualWidth > 0 ? virtualWidth : orthoViewport.Width, virtualHeight);
            }
            else internalViewport = screenViewport;
            extendedViewport = orthoViewport;

            projection =
                Matrix4.CreateTranslation(orthoViewport.X - extendedViewport.Width * 0.5f, orthoViewport.Y - (yDown ? -extendedViewport.Height : extendedViewport.Height) * 0.5f, 0) *
                Matrix4.CreateOrthographic(extendedViewport.Width, extendedViewport.Height, NearPlane, FarPlane);
            view =
                Matrix4.LookAt(Position, Position + Forward, Up);
        }
    }
}
