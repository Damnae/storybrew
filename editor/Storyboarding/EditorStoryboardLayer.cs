using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Util;
using OpenTK;
using StorybrewCommon.Storyboarding;
using System;
using System.IO;

namespace StorybrewEditor.Storyboarding
{
    public class EditorStoryboardLayer : StoryboardLayer, IComparable<EditorStoryboardLayer>
    {
        public Guid Guid { get; set; } = Guid.NewGuid();

        string name = "";
        public string _Name
        {
            get => name;
            set
            {
                if (name == value) return;
                name = value;
                RaiseChanged(nameof(_Name));
            }
        }

        public Effect Effect { get; }

        bool visible = true;
        public bool Visible
        {
            get => visible;
            set
            {
                if (visible == value) return;
                visible = value;
                RaiseChanged(nameof(Visible));
            }
        }

        OsbLayer osbLayer = OsbLayer.Background;
        public OsbLayer OsbLayer
        {
            get => osbLayer;
            set
            {
                if (osbLayer == value) return;
                osbLayer = value;
                RaiseChanged(nameof(OsbLayer));
            }
        }

        bool diffSpecific;
        public bool DiffSpecific
        {
            get => diffSpecific;
            set
            {
                if (diffSpecific == value) return;
                diffSpecific = value;
                RaiseChanged(nameof(DiffSpecific));
            }
        }

        double startTime, endTime;
        public override double StartTime => startTime;
        public override double EndTime => endTime;

        public override bool ReverseDepth
        {
            get => segment.ReverseDepth;
            set => segment.ReverseDepth = value;
        }

        public bool Highlight;

        public int EstimatedSize { get; set; }

        public event ChangedHandler OnChanged;
        protected void RaiseChanged(string propertyName)
            => EventHelper.InvokeStrict(() => OnChanged, d => ((ChangedHandler)d)(this, new ChangedEventArgs(propertyName)));

        readonly EditorStoryboardSegment segment;

        public EditorStoryboardLayer(string identifier, Effect effect) : base(identifier)
        {
            Effect = effect;
            segment = new EditorStoryboardSegment(effect, this);
        }

        public int GetActiveSpriteCount(double time) => Visible ? segment.GetActiveSpriteCount(time) : 0;
        public int GetCommandCost(double time) => Visible ? segment.GetCommandCost(time) : 0;

        public override OsbSprite CreateSprite(string path, OsbOrigin origin, Vector2 initialPosition) => segment.CreateSprite(path, origin, initialPosition);
        public override OsbSprite CreateSprite(string path, OsbOrigin origin) => segment.CreateSprite(path, origin, new Vector2(320, 240));

        public override OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, Vector2 initialPosition)
            => segment.CreateAnimation(path, frameCount, frameDelay, loopType, origin, initialPosition);

        public override OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin = OsbOrigin.Centre)
            => segment.CreateAnimation(path, frameCount, frameDelay, loopType, origin);

        public override OsbSample CreateSample(string path, double time, double volume)
            => segment.CreateSample(path, time, volume);

        public override StoryboardSegment CreateSegment() => segment.CreateSegment();
        public override void Discard(StoryboardObject storyboardObject) => segment.Discard(storyboardObject);

        public void TriggerEvents(double fromTime, double toTime)
        {
            if (!Visible) return;
            segment.TriggerEvents(fromTime, toTime);
        }
        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, FrameStats frameStats)
        {
            if (!Visible) return;
            segment.Draw(drawContext, camera, bounds, opacity, Effect.Project, frameStats);
        }
        public void PostProcess()
        {
            segment.PostProcess();

            startTime = segment.StartTime;
            if (startTime == double.MaxValue) startTime = 0;

            endTime = segment.EndTime;
            if (endTime == double.MinValue) endTime = 0;

            EstimatedSize = segment.CalculateSize(osbLayer);
        }

        public void WriteOsb(TextWriter writer, ExportSettings exportSettings) => WriteOsb(writer, exportSettings, osbLayer);
        public override void WriteOsb(TextWriter writer, ExportSettings exportSettings, OsbLayer layer) => segment.WriteOsb(writer, exportSettings, osbLayer);

        public void CopySettings(EditorStoryboardLayer other, bool copyGuid = false)
        {
            if (copyGuid) Guid = other.Guid;
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

        public override string ToString() => $"name:{name}, id:{Name}, layer:{osbLayer}, diffSpec:{diffSpecific}";
    }
}