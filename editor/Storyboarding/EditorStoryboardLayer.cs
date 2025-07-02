using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Util;
using OpenTK;
using StorybrewCommon.Storyboarding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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

        private double startTime;
        public override double StartTime => startTime;

        private double endTime;
        public override double EndTime => endTime;

        public override Vector2 Origin
        {
            get => segment.Origin;
            set => segment.Origin = value;
        }
        public override Vector2 Position
        {
            get => segment.Position;
            set => segment.Position = value;
        }
        public override double Rotation
        {
            get => segment.Rotation;
            set => segment.Rotation = value;
        }
        public override double Scale
        {
            get => segment.Scale;
            set => segment.Scale = value;
        }

        public override bool ReverseDepth
        {
            get => segment.ReverseDepth;
            set => segment.ReverseDepth = value;
        }

        public bool Highlight;

        public int EstimatedSize { get; private set; }

        public event ChangedHandler OnChanged;
        protected void RaiseChanged(string propertyName)
            => EventHelper.InvokeStrict(() => OnChanged, d => ((ChangedHandler)d)(this, new ChangedEventArgs(propertyName)));

        private readonly EditorStoryboardSegment segment;

        public EditorStoryboardLayer(string identifier, Effect effect) : base(identifier)
        {
            Effect = effect;
            segment = new EditorStoryboardSegment(effect, this, "Root");
        }

        public override OsbSprite CreateSprite(string path, OsbOrigin origin, Vector2 initialPosition)
            => segment.CreateSprite(path, origin, initialPosition);

        public override OsbSprite CreateSprite(string path, OsbOrigin origin)
            => segment.CreateSprite(path, origin);

        public override OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, Vector2 initialPosition)
            => segment.CreateAnimation(path, frameCount, frameDelay, loopType, origin, initialPosition);

        public override OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin = OsbOrigin.Centre)
            => segment.CreateAnimation(path, frameCount, frameDelay, loopType, origin);

        public override OsbSample CreateSample(string path, double time, double volume)
            => segment.CreateSample(path, time, volume);

        public override IEnumerable<StoryboardSegment> NamedSegments => segment.NamedSegments;
        public override StoryboardSegment CreateSegment(string identifier = null) => segment.CreateSegment(identifier);
        public override StoryboardSegment GetSegment(string identifier) => segment.GetSegment(identifier);

        public override void Discard(StoryboardObject storyboardObject)
            => segment.Discard(storyboardObject);

        public void TriggerEvents(double fromTime, double toTime)
        {
            if (!Visible)
                return;

            segment.TriggerEvents(fromTime, toTime);
        }

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, FrameStats frameStats)
        {
            if (!Visible)
                return;

            segment.Draw(drawContext, camera, bounds, opacity, null, Effect.Project, frameStats);
        }

        public void PostProcess(CancellationToken token)
        {
            segment.PostProcess();

            startTime = segment.StartTime;
            if (startTime == double.MaxValue)
                startTime = 0;

            endTime = segment.EndTime;
            if (endTime == double.MinValue)
                endTime = 0;

            EstimatedSize = segment.CalculateSize(osbLayer, token);
        }

        public void WriteOsb(TextWriter writer, ExportSettings exportSettings, CancellationToken token = default)
            => WriteOsb(writer, exportSettings, osbLayer, null, token);

        public override void WriteOsb(TextWriter writer, ExportSettings exportSettings, OsbLayer layer, StoryboardTransform transform, CancellationToken token = default)
            => segment.WriteOsb(writer, exportSettings, osbLayer, transform, token);

        public void CopySettings(EditorStoryboardLayer other, bool copyGuid = false)
        {
            if (copyGuid)
                Guid = other.Guid;
            DiffSpecific = other.DiffSpecific;
            OsbLayer = other.OsbLayer;
            Visible = other.Visible;
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
