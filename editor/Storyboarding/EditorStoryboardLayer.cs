using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Util;
using OpenTK;
using StorybrewCommon.Storyboarding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StorybrewEditor.Storyboarding
{
    public class EditorStoryboardLayer : StoryboardLayer, IComparable<EditorStoryboardLayer>
    {
        public Guid Guid { get; set; } = Guid.NewGuid();

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

        public Effect Effect { get; }

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

        private OsbLayer osbLayer = OsbLayer.Background;
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

        public int EstimatedSize { get; private set; }

        public event ChangedHandler OnChanged;
        protected void RaiseChanged(string propertyName)
            => EventHelper.InvokeStrict(() => OnChanged, d => ((ChangedHandler)d)(this, new ChangedEventArgs(propertyName)));

        public EditorStoryboardLayer(string identifier, Effect effect) : base(identifier)
        {
            Effect = effect;
        }

        private readonly List<StoryboardObject> storyboardObjects = new List<StoryboardObject>();
        private readonly List<DisplayableObject> displayableObjects = new List<DisplayableObject>();
        private readonly List<EventObject> eventObjects = new List<EventObject>();

        public int GetActiveSpriteCount(double time)
            => Visible ? storyboardObjects
                .Count(o => (o as OsbSprite)?.IsActive(time) ?? false) : 0;

        public int GetCommandCost(double time)
            => Visible ? storyboardObjects
                .Select(o => o as OsbSprite)
                .Where(s => s?.IsActive(time) ?? false)
                .Sum(s => s.CommandCost) : 0;

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

        public override OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, Vector2 initialPosition)
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

        public override OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin = OsbOrigin.Centre)
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

        public override void Discard(StoryboardObject storyboardObject)
        {
            storyboardObjects.Remove(storyboardObject);
            if (storyboardObject is DisplayableObject displayableObject)
                displayableObjects.Remove(displayableObject);
            if (storyboardObject is EventObject eventObject)
                eventObjects.Remove(eventObject);
        }

        public void TriggerEvents(double fromTime, double toTime)
        {
            if (!Visible) return;
            foreach (var eventObject in eventObjects)
                if (fromTime <= eventObject.EventTime && eventObject.EventTime < toTime)
                    eventObject.TriggerEvent(Effect.Project, toTime);
        }

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, FrameStats frameStats)
        {
            if (!Visible) return;

            if (Highlight || Effect.Highlight)
                opacity *= (float)((Math.Sin(drawContext.Get<Editor>().TimeSource.Current * 4) + 1) * 0.5);

            foreach (var displayableObject in displayableObjects)
                displayableObject.Draw(drawContext, camera, bounds, opacity, Effect.Project, frameStats);
        }

        public void CopySettings(EditorStoryboardLayer other, bool copyGuid = false)
        {
            if (copyGuid)
                Guid = other.Guid;
            DiffSpecific = other.DiffSpecific;
            OsbLayer = other.OsbLayer;
            Visible = other.Visible;
        }

        public void PostProcess()
        {
            foreach (var storyboardObject in storyboardObjects)
                (storyboardObject as HasPostProcess)?.PostProcess();

            calculateSize();
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

        private void calculateSize()
        {
            EstimatedSize = 0;

            var exportSettings = new ExportSettings
            {
                OptimiseSprites = false, // reduce update time for a minor inaccuracy in estimatedSize
            };
            using (var stream = new ByteCounterStream())
            {
                using (var writer = new StreamWriter(stream, Project.Encoding))
                    foreach (var sbo in storyboardObjects)
                        sbo.WriteOsb(writer, exportSettings, osbLayer);

                EstimatedSize = (int)stream.Length;
            }
        }

        public override string ToString() => $"name:{name}, id:{Identifier}, layer:{osbLayer}, diffSpec:{diffSpecific}";
    }
}
