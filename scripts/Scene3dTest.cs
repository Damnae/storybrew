using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Animations;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Util;
using System;

namespace StorybrewScripts
{
    public class Scene3dTest : StoryboardObjectGenerator
    {
        public override void Generate()
        {
            var layer = GetLayer("");

            var scene3d = layer.CreateScene3d();

            var camera = scene3d.Camera;
            camera.NearPlane.Add(0, 50);
            camera.NearFade.Add(0, 100);
            camera.FarFade.Add(0, 600);
            camera.FarPlane.Add(0, 650);

            camera.Move(0, new Vector3(0, 0, 0));
            camera.Rotate(0, new Vector3(0, 0, -1), new Vector3(0, 1, 0));

            camera.Move(OsbEasing.InOutQuad, 1000, new Vector3(0, 0, 600));
            camera.FieldOfView.Add(1000, 67);

            camera.FieldOfView.Add(15000, 103, EasingFunctions.QuadInOut);
            camera.Move(15000, new Vector3(0, 0, 600));
            camera.Rotate(15000, new Vector3(0, 0, -1), new Vector3(0, 1, 0));

            camera.Move(OsbEasing.InOutQuad, 16000, new Vector3(350, 0, 600));
            camera.LookAt(OsbEasing.InOutQuad, 16000, new Vector3(0, 0, 0));
            camera.Move(OsbEasing.InOutQuad, 18000, new Vector3(-350, 0, 600));
            camera.LookAt(OsbEasing.InOutQuad, 18000, new Vector3(0, 0, 0));
            camera.Move(OsbEasing.InOutQuad, 20000, new Vector3(350, 0, 600));
            camera.LookAt(OsbEasing.InOutQuad, 20000, new Vector3(0, 0, 0));
            camera.Move(OsbEasing.InOutQuad, 22000, new Vector3(-350, 0, 600));
            camera.LookAt(OsbEasing.InOutQuad, 22000, new Vector3(0, 0, 0));
            camera.Move(OsbEasing.InOutQuad, 24000, new Vector3(350, 0, 600));
            camera.LookAt(OsbEasing.InOutQuad, 24000, new Vector3(0, 0, 0));
            camera.FieldOfView.Add(24000, 103);

            camera.Move(OsbEasing.InOutQuad, 30000, new Vector3(0, 0, 600));
            camera.LookAt(OsbEasing.InOutQuad, 30000, new Vector3(0, 0, 0));
            camera.FieldOfView.Add(30000, 67, EasingFunctions.QuadInOut);

            camera.Rotate(OsbEasing.InQuad, 35000, new Vector3(0, 0, -1), new Vector3(1, 0, 0));
            camera.Move(OsbEasing.InOutQuad, 35000, new Vector3(-500, 0, 500));
            camera.Rotate(OsbEasing.OutQuad, 40000, new Vector3(0, 0, -1), new Vector3(0, -1, 0));
            camera.Move(OsbEasing.InOutQuad, 40000, new Vector3(500, 0, 700));

            var root = scene3d.RootContainer;
            root.Rotate(2000, Quaternion.Identity);
            root.Rotate(OsbEasing.InQuad, 4000, Quaternion.FromAxisAngle(new Vector3(0, 0, 1), (float)Math.PI));
            root.Rotate(OsbEasing.OutQuad, 6000, Quaternion.FromAxisAngle(new Vector3(0, 0, 1), (float)Math.PI * 2));

            for (var i = 0; i < 20; i++)
            {
                var top = root.CreateSprite3d("test.png");
                top.Move(0, 0, 0, 0);
                top.Move(OsbEasing.InSine, 2000, 0, -50, 50 * i);
                top.Rotate(0, Quaternion.FromAxisAngle(new Vector3(0, 0, 1), 0));
                top.ColorHsb(0, 0, 1, (i + 10) / 20.0f);
            }

            for (var i = 0; i < 20; i++)
            {
                var left = root.CreateSprite3d("test.png");
                left.Move(0, 0, 0, 0);
                left.Move(OsbEasing.InSine, 2000, -50 * (i + 1), 0, 50 * i);
                left.Rotate(0, Quaternion.FromAxisAngle(new Vector3(0, 0, 1), (float)Math.PI / 2));
                left.ColorHsb(0, 240, 1, (i + 10) / 20.0f);
            }

            for (var i = 0; i < 20; i++)
            {
                var right = root.CreateSprite3d("test.png");
                right.Move(0, 0, 0, 0);
                right.Move(OsbEasing.InSine, 2000, 50 * (i + 1), 0, 50 * i);
                right.Rotate(0, Quaternion.FromAxisAngle(new Vector3(0, 0, 1), -(float)Math.PI / 2));
                right.ColorHsb(0, 180, 1, (i + 10) / 20.0f);
            }

            for (var i = 0; i < 20; i++)
            {
                var bottom = root.CreateSprite3d("test.png");
                bottom.Move(0, 0, 0, 0);
                bottom.Move(OsbEasing.InSine, 2000, 0, 150, 50 * i);
                bottom.Rotate(0, Quaternion.FromAxisAngle(new Vector3(0, 0, 1), (float)Math.PI));
                bottom.ColorHsb(0, 0, 0, (i + 10) / 20.0f);
            }

            var up = root.CreateSprite3d("test.png");
            up.Move(0, 0, 0, 0);
            up.Move(OsbEasing.InSine, 2000, 0, -100, 0);
        }
    }
}