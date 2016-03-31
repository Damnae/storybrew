using OpenTK;
using System;
using System.Drawing;

namespace StorybrewEditor.Graphics.Cameras
{
    public abstract class CameraBase : Camera
    {
        private Rectangle internalViewport;
        private Rectangle extendedViewport;
        public Rectangle InternalViewport { get { CheckDirty(); return internalViewport; } }
        public Rectangle ExtendedViewport { get { CheckDirty(); return extendedViewport; } }

        private Matrix4 projection;
        private Matrix4 view;
        private Matrix4 projectionView;
        private Matrix4 invertedProjectionView;
        public Matrix4 Projection { get { CheckDirty(); return projection; } }
        public Matrix4 View { get { CheckDirty(); return view; } }
        public Matrix4 ProjectionView { get { CheckDirty(); return projectionView; } }
        public Matrix4 InvertedProjectionView { get { CheckDirty(); return invertedProjectionView; } }

        public event EventHandler Changed;

        private float nearPlane;
        public float NearPlane
        {
            get { return nearPlane; }
            set
            {
                if (nearPlane == value) return;
                nearPlane = value;
                MarkDirty();
            }
        }

        private float farPlane;
        public float FarPlane
        {
            get { return farPlane; }
            set
            {
                if (farPlane == value) return;
                farPlane = value;
                MarkDirty();
            }
        }

        private Rectangle viewport;
        public Rectangle Viewport
        {
            get { return viewport; }
            set
            {
                if (viewport == value) return;
                viewport = value;
                MarkDirty();
            }
        }

        private Vector3 position = new Vector3();
        public Vector3 Position
        {
            get { return position; }
            set
            {
                if (position == value) return;
                position = value;
                MarkDirty();
            }
        }

        private Vector3 direction = new Vector3(0, -1, 0);
        public Vector3 Direction
        {
            get { return direction; }
            set
            {
                if (direction == value) return;
                direction = value;
                MarkDirty();
            }
        }

        private Vector3 up = new Vector3(0, 0, 1);
        public Vector3 Up
        {
            get { return up; }
            set
            {
                if (up == value) return;
                up = value;
                MarkDirty();
            }
        }

        public CameraBase()
        {
            viewport = DrawState.Viewport;
            DrawState.ViewportChanged += drawState_ViewportChanged;
            dirty = true;
        }

        public void Dispose()
        {
            DrawState.ViewportChanged -= drawState_ViewportChanged;
        }

        public Vector3 FromScreen(Vector2 screenCoords)
        {
            CheckDirty();

            var deviceX = 2 * (screenCoords.X / viewport.Width) - 1;
            var deviceY = -2 * (screenCoords.Y / viewport.Height) + 1;

            var near = Vector3.Transform(new Vector3(deviceX, deviceY, NearPlane), invertedProjectionView);
            var far = Vector3.Transform(new Vector3(deviceX, deviceY, FarPlane), invertedProjectionView);
            var direction = Vector3.Normalize(far - near);

            // The screen ray is parallel to the world plane
            if (direction.Z == 0) return Vector3.Zero;

            return near - direction * (near.Z / direction.Z);
        }

        public Vector3 ToScreen(Vector3 viewCoords)
        {
            CheckDirty();

            var devicePosition = Vector3.Transform(viewCoords, projectionView);
            return new Vector3(
                (devicePosition.X + 1) * 0.5f * viewport.Width,
                (-devicePosition.Y + 1) * 0.5f * viewport.Height,
                devicePosition.Z
            );
        }

        public void LookAt(Vector3 target)
        {
            var newDirection = (target - position).Normalized();
            if (newDirection != Vector3.Zero)
            {
                float dot = Vector3.Dot(newDirection, up);
                if (Math.Abs(dot - 1) < 0.000000001f)
                    up = direction * -1;
                else if (Math.Abs(dot + 1) < 0.000000001f)
                    up = direction;

                direction = newDirection;
                up = Vector3.Cross(Vector3.Cross(direction, up).Normalized(), direction).Normalized();

                MarkDirty();
            }
        }

        public void Rotate(Vector3 axis, float angle)
        {
            var rotation = Matrix4.CreateFromAxisAngle(axis, angle);
            Vector3.Transform(ref up, ref rotation, out up);
            Vector3.Transform(ref direction, ref rotation, out direction);
            MarkDirty();
        }

        private bool dirty;
        protected void CheckDirty()
        {
            if (!dirty) return;

            Recalculate(out view, out projection, out internalViewport, out extendedViewport);
            dirty = false;

            projectionView = view * projection;
            invertedProjectionView = projectionView.Inverted();
        }

        protected void MarkDirty()
        {
            dirty = true;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void drawState_ViewportChanged()
        {
            Viewport = DrawState.Viewport;
        }

        protected abstract void Recalculate(out Matrix4 view, out Matrix4 projection, out Rectangle internalViewport, out Rectangle extendedViewport);
    }
}
