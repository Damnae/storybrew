using OpenTK;
using System;
using System.Drawing;

namespace BrewLib.Graphics.Cameras
{
    public abstract class CameraBase : Camera
    {
        public static Vector3 DefaultForward = new Vector3(0, -1, 0);
        public static Vector3 DefaultUp = new Vector3(0, 0, 1);

        Rectangle internalViewport, extendedViewport;
        public Rectangle InternalViewport { get { Validate(); return internalViewport; } }
        public Rectangle ExtendedViewport { get { Validate(); return extendedViewport; } }

        Matrix4 projection, view, projectionView, invertedProjectionView;
        public Matrix4 Projection { get { Validate(); return projection; } }
        public Matrix4 View { get { Validate(); return view; } }
        public Matrix4 ProjectionView { get { Validate(); return projectionView; } }
        public Matrix4 InvertedProjectionView { get { Validate(); return invertedProjectionView; } }

        public event EventHandler Changed;

        float nearPlane;
        public float NearPlane
        {
            get => nearPlane;
            set
            {
                if (nearPlane == value) return;
                nearPlane = value;
                Invalidate();
            }
        }

        float farPlane;
        public float FarPlane
        {
            get => farPlane;
            set
            {
                if (farPlane == value) return;
                farPlane = value;
                Invalidate();
            }
        }

        Rectangle viewport;
        public Rectangle Viewport
        {
            get => viewport;
            set
            {
                if (viewport == value) return;
                viewport = value;
                Invalidate();
            }
        }

        Vector3 position = Vector3.Zero;
        public Vector3 Position
        {
            get => position;
            set
            {
                if (position == value) return;
                position = value;
                Invalidate();
            }
        }

        Vector3 forward = DefaultForward;
        public Vector3 Forward
        {
            get => forward;
            set
            {
                if (forward == value) return;
                forward = value;
                Invalidate();
            }
        }

        Vector3 up = DefaultUp;
        public Vector3 Up
        {
            get => up;
            set
            {
                if (up == value) return;
                up = value;
                Invalidate();
            }
        }

        public CameraBase()
        {
            viewport = DrawState.Viewport;
            DrawState.ViewportChanged += drawState_ViewportChanged;
            needsUpdate = true;
        }

        public void Dispose() => DrawState.ViewportChanged -= drawState_ViewportChanged;

        public Vector3 FromScreen(Vector2 screenCoords)
        {
            Validate();
            // TODO Vector3.Unproject() ?

            var deviceX = 2 * (screenCoords.X / viewport.Width) - 1;
            var deviceY = -2 * (screenCoords.Y / viewport.Height) + 1;

            var near = Vector4.Transform(new Vector4(deviceX, deviceY, NearPlane, 1), invertedProjectionView).Xyz;
            var far = Vector4.Transform(new Vector4(deviceX, deviceY, FarPlane, 1), invertedProjectionView).Xyz;
            var direction = Vector3.Normalize(far - near);

            if (direction.Z == 0) return Vector3.Zero;
            return near - direction * (near.Z / direction.Z);
        }
        public Box2 FromScreen(Box2 screenBox2) => new Box2(
            FromScreen(new Vector2(screenBox2.Left, screenBox2.Top)).Xy, FromScreen(new Vector2(screenBox2.Right, screenBox2.Bottom)).Xy);

        public Vector3 ToScreen(Vector3 worldCoords)
        {
            Validate();
            // TODO Vector3.Project() ?

            var transformedPosition = Vector4.Transform(new Vector4(worldCoords, 1), projectionView);
            var devicePosition = transformedPosition.Xyz / Math.Abs(transformedPosition.W);

            return new Vector3((devicePosition.X + 1) / 2 * viewport.Width, (-devicePosition.Y + 1) / 2 * viewport.Height, devicePosition.Z);
        }

        public Vector3 ToScreen(Vector2 worldCoords) => ToScreen(new Vector3(worldCoords));
        public Box2 ToScreen(Box2 worldBox2) => new Box2(
            ToScreen(new Vector2(worldBox2.Left, worldBox2.Top)).Xy, ToScreen(new Vector2(worldBox2.Right, worldBox2.Bottom)).Xy);

        public void LookAt(Vector3 target)
        {
            var newForward = (target - position).Normalized();
            if (newForward != Vector3.Zero)
            {
                var dot = Vector3.Dot(newForward, up);
                if (Math.Abs(dot - 1) < .000000001) up = forward * -1;
                else if (Math.Abs(dot + 1) < .000000001) up = forward;

                forward = newForward;
                up = Vector3.Cross(Vector3.Cross(forward, up).Normalized(), forward).Normalized();

                Invalidate();
            }
        }
        public void Rotate(Vector3 axis, float angle)
        {
            var rotation = Matrix3.CreateFromAxisAngle(axis, angle);
            Vector3.Transform(ref up, ref rotation, out up);
            Vector3.Transform(ref forward, ref rotation, out forward);
            Invalidate();
        }

        bool needsUpdate;
        protected void Validate()
        {
            if (!needsUpdate) return;

            Recalculate(out view, out projection, out internalViewport, out extendedViewport);
            needsUpdate = false;

            projectionView = view * projection;
            invertedProjectionView = projectionView.Inverted();
        }
        protected void Invalidate()
        {
            needsUpdate = true;
            Changed?.Invoke(this, EventArgs.Empty);
        }
        void drawState_ViewportChanged() => Viewport = DrawState.Viewport;

        protected abstract void Recalculate(out Matrix4 view, out Matrix4 projection, out Rectangle internalViewport, out Rectangle extendedViewport);
    }
}