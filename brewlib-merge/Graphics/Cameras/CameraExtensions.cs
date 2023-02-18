using OpenTK;

namespace BrewLib.Graphics.Cameras
{
    public static class CameraExtensions
    {
        // Vector

        public static Vector3 ToScreen(this Camera camera, Vector2 worldCoords) => camera.ToScreen(new Vector3(worldCoords));
        public static Vector2 ToCamera(this Camera from, Camera to, Vector2 coords) => to.FromScreen(from.ToScreen(coords).Xy).Xy;
        public static Vector2 ToCamera(this Camera from, Camera to, Vector3 coords) => to.FromScreen(from.ToScreen(coords).Xy).Xy;

        // Box2

        public static Box2 ToScreen(this Camera camera, Box2 worldBox2) => new Box2(
            camera.ToScreen(new Vector2(worldBox2.Left, worldBox2.Top)).Xy, camera.ToScreen(new Vector2(worldBox2.Right, worldBox2.Bottom)).Xy);

        public static Box2 FromScreen(this Camera camera, Box2 screenBox2) => new Box2(
            camera.FromScreen(new Vector2(screenBox2.Left, screenBox2.Top)).Xy, camera.FromScreen(new Vector2(screenBox2.Right, screenBox2.Bottom)).Xy);

        public static Box2 ToCamera(this Camera from, Camera to, Box2 box2) => new Box2(
            from.ToCamera(to, new Vector2(box2.Left, box2.Top)), from.ToCamera(to, new Vector2(box2.Right, box2.Bottom)));
    }
}