using OpenTK;
using System;
using System.Drawing;

namespace BrewLib.Graphics.Cameras
{
    public interface Camera : IDisposable
    {
        // Inputs 

        Rectangle Viewport { get; set; }
        Vector3 Position { get; set; }
        Vector3 Forward { get; set; }
        Vector3 Up { get; set; }
        float NearPlane { get; }
        float FarPlane { get; }

        // Outputs

        Matrix4 Projection { get; }
        Matrix4 View { get; }
        Matrix4 ProjectionView { get; }
        Matrix4 InvertedProjectionView { get; }
        Rectangle InternalViewport { get; }
        Rectangle ExtendedViewport { get; }

        Vector3 FromScreen(Vector2 screenCoords);
        Box2 FromScreen(Box2 screenBox2);
        Vector3 ToScreen(Vector3 worldCoords);
        Vector3 ToScreen(Vector2 worldCoords);
        Box2 ToScreen(Box2 worldBox2);

        event EventHandler Changed;
    }
}