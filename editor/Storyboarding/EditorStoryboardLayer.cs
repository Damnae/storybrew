using OpenTK;
using StorybrewCommon.Storyboarding;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Cameras;
using System;
using System.Collections.Generic;
using System.IO;

namespace StorybrewEditor.Storyboarding
{
    public class EditorStoryboardLayer : StoryboardLayer
    {
        private string name = "";
        public string Name
        {
            get { return name; }
            set
            {
                if (name == value) return;
                name = value;
                RaiseChanged();
            }
        }

        private Effect effect;
        public Effect Effect => effect;

        private bool visible = true;
        public bool Visible
        {
            get { return visible; }
            set
            {
                if (visible == value) return;
                visible = value;
                RaiseChanged();
            }
        }

        public event EventHandler OnChanged;
        protected void RaiseChanged()
            => OnChanged?.Invoke(this, EventArgs.Empty);

        public EditorStoryboardLayer(string identifier,  Effect effect) : base(identifier)
        {
            this.effect = effect;
        }

        private List<StoryboardObject> storyboardObjects = new List<StoryboardObject>();
        private List<DisplayableObject> displayableObjects = new List<DisplayableObject>();

        public override OsbSprite CreateSprite(string path, OsbLayer layer, OsbOrigin origin)
            => CreateSprite(path, layer, origin, OsbSprite.DefaultPosition);

        public override OsbSprite CreateSprite(string path, OsbLayer layer, OsbOrigin origin, Vector2 initialPosition)
        {
            var storyboardObject = new EditorOsbSprite()
            {
                TexturePath = path,
                Layer = layer,
                Origin = origin,
                InitialPosition = initialPosition,
            };
            storyboardObjects.Add(storyboardObject);
            displayableObjects.Add(storyboardObject);
            return storyboardObject;
        }

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, OsbLayer osbLayer)
        {
            if (!Visible) return;
            foreach (var displayableObject in displayableObjects)
            {
                if (displayableObject.Layer != osbLayer) continue;
                displayableObject.Draw(drawContext, camera, bounds, opacity, effect.Project);
            }
        }

        public void CopySettings(EditorStoryboardLayer other)
        {
            Visible = other.Visible;
        }

        public void WriteOsbSprites(TextWriter writer, ExportSettings exportSettings, OsbLayer osbLayer)
        {
            foreach (var sbo in storyboardObjects)
            {
                if (sbo.Layer != osbLayer) continue;
                sbo.WriteOsb(writer, exportSettings);
            }
        }
    }
}
