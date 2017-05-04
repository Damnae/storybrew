﻿using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Util;
using OpenTK;
using StorybrewCommon.Storyboarding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StorybrewEditor.Storyboarding
{
    public class EditorStoryboardLayer : StoryboardLayer, IComparable<EditorStoryboardLayer>
    {
        private string name = "";
        public string Name
        {
            get { return name; }
            set
            {
                if (name == value) return;
                name = value;
                RaiseChanged(nameof(Name));
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
                RaiseChanged(nameof(Visible));
            }
        }

        private OsbLayer osbLayer;
        public OsbLayer OsbLayer
        {
            get { return osbLayer; }
            set
            {
                if (osbLayer == value) return;
                osbLayer = value;
                RaiseChanged(nameof(OsbLayer));
            }
        }

        private bool diffSpecific;
        public bool DiffSpecific
        {
            get { return diffSpecific; }
            set
            {
                if (diffSpecific == value) return;
                diffSpecific = value;
                RaiseChanged(nameof(DiffSpecific));
            }
        }

        public double StartTime => storyboardObjects.Select(l => l.StartTime).DefaultIfEmpty().Min();
        public double EndTime => storyboardObjects.Select(l => l.EndTime).DefaultIfEmpty().Max();

        public bool Highlight;

        public event ChangedHandler OnChanged;
        protected void RaiseChanged(string propertyName)
            => EventHelper.InvokeStrict(() => OnChanged, d => ((ChangedHandler)d)(this, new ChangedEventArgs(propertyName)));

        public EditorStoryboardLayer(string identifier, Effect effect, OsbLayer osbLayer = OsbLayer.Background) : base(identifier)
        {
            this.effect = effect;
            this.osbLayer = Enum.IsDefined(typeof(OsbLayer), (int)osbLayer) ? osbLayer : OsbLayer.Background;
        }

        private List<StoryboardObject> storyboardObjects = new List<StoryboardObject>();
        private List<DisplayableObject> displayableObjects = new List<DisplayableObject>();
        private List<EventObject> eventObjects = new List<EventObject>();

        public override OsbSprite CreateSprite(string path, OsbOrigin origin, Vector2 initialPosition)
        {
            var storyboardObject = new EditorOsbSprite()
            {
                TexturePath = path,
                Origin = origin,
                InitialPosition = initialPosition,
            };
            storyboardObjects.Add(storyboardObject);
            displayableObjects.Add(storyboardObject);
            return storyboardObject;
        }

        public override OsbSprite CreateSprite(string path, OsbOrigin origin)
            => CreateSprite(path, origin, OsbSprite.DefaultPosition);

        public override OsbAnimation CreateAnimation(string path, int frameCount, int frameDelay, OsbLoopType loopType, OsbOrigin origin, Vector2 initialPosition)
        {
            var storyboardObject = new EditorOsbAnimation()
            {
                TexturePath = path,
                Origin = origin,
                FrameCount = frameCount,
                FrameDelay = frameDelay,
                LoopType = loopType,
                InitialPosition = initialPosition,
            };
            storyboardObjects.Add(storyboardObject);
            displayableObjects.Add(storyboardObject);
            return storyboardObject;
        }

        public override OsbAnimation CreateAnimation(string path, int frameCount, int frameDelay, OsbLoopType loopType, OsbOrigin origin = OsbOrigin.Centre)
            => CreateAnimation(path, frameCount, frameDelay, loopType, origin, OsbSprite.DefaultPosition);
        
        public override OsbSample CreateSample(string path, double time, double volume)
        {
            var storyboardObject = new EditorOsbSample()
            {
                AudioPath = path,
                Time = time,
                Volume = volume,
            };
            storyboardObjects.Add(storyboardObject);
            eventObjects.Add(storyboardObject);
            return storyboardObject;
        }

        public void TriggerEvents(double fromTime, double toTime)
        {
            if (!Visible) return;
            foreach (var eventObject in eventObjects)
                if (fromTime <= eventObject.EventTime && eventObject.EventTime < toTime)
                    eventObject.TriggerEvent(effect.Project, toTime);
        }

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity)
        {
            if (!Visible) return;

            if (Highlight || effect.Highlight)
                opacity *= (float)((Math.Sin(drawContext.Get<Editor>().TimeSource.Current * 4) + 1) * 0.5);

            foreach (var displayableObject in displayableObjects)
                displayableObject.Draw(drawContext, camera, bounds, opacity, effect.Project);
        }

        public void CopySettings(EditorStoryboardLayer other)
        {
            DiffSpecific = other.DiffSpecific;
            OsbLayer = other.OsbLayer;
            Visible = other.Visible;
        }

        public void WriteOsbSprites(TextWriter writer, ExportSettings exportSettings)
        {
            foreach (var sbo in storyboardObjects)
                sbo.WriteOsb(writer, exportSettings, osbLayer);
        }

        public int CompareTo(EditorStoryboardLayer other)
        {
            var value = osbLayer - other.osbLayer;
            if (value == 0) value = (other.diffSpecific ? 1 : 0) - (diffSpecific ? 1 : 0);
            return value;
        }

        public override string ToString() => $"name:{name}, id:{Identifier}, layer:{osbLayer}, diffSpec:{diffSpecific}";
    }
}
