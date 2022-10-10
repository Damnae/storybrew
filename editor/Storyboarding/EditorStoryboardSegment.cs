using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Util;
using OpenTK;
using StorybrewCommon.Storyboarding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace StorybrewEditor.Storyboarding
{
    public class EditorStoryboardSegment : StoryboardSegment
    {
        public Effect Effect { get; }

        public double StartTime => storyboardObjects.Select(l => l.StartTime).DefaultIfEmpty().Min();
        public double EndTime => storyboardObjects.Select(l => l.EndTime).DefaultIfEmpty().Max();

        public bool Highlight;

        public event ChangedHandler OnChanged;
        protected void RaiseChanged(string propertyName)
            => EventHelper.InvokeStrict(() => OnChanged, d => ((ChangedHandler)d)(this, new ChangedEventArgs(propertyName)));

        private readonly List<StoryboardObject> storyboardObjects = new List<StoryboardObject>();
        private readonly List<DisplayableObject> displayableObjects = new List<DisplayableObject>();
        private readonly List<EventObject> eventObjects = new List<EventObject>();
        private readonly List<EditorStoryboardSegment> segments = new List<EditorStoryboardSegment>();

        public EditorStoryboardSegment(Effect effect)
        {
            Effect = effect;
        }

        public int GetActiveSpriteCount(double time)
            => storyboardObjects.Count(o => (o as OsbSprite)?.IsActive(time) ?? false)
                + segments.Sum(s => s.GetActiveSpriteCount(time));

        public int GetCommandCost(double time)
            => storyboardObjects
                .Select(o => o as OsbSprite)
                .Where(s => s?.IsActive(time) ?? false)
                .Sum(s => s.CommandCost)
                + segments.Sum(s => s.GetCommandCost(time));

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

        public override StoryboardSegment CreateSegment()
        {
            var segment = new EditorStoryboardSegment(Effect);
            segments.Add(segment);
            return segment;
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
            foreach (var eventObject in eventObjects)
                if (fromTime <= eventObject.EventTime && eventObject.EventTime < toTime)
                    eventObject.TriggerEvent(Effect.Project, toTime);
            segments.ForEach(s => s.TriggerEvents(fromTime, toTime));
        }

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, FrameStats frameStats, bool highlight)
        {
            if (highlight)
                opacity *= (float)((Math.Sin(drawContext.Get<Editor>().TimeSource.Current * 4) + 1) * 0.5);

            foreach (var displayableObject in displayableObjects)
                displayableObject.Draw(drawContext, camera, bounds, opacity, Effect.Project, frameStats);

            segments.ForEach(s => s.Draw(drawContext, camera, bounds, opacity, frameStats, highlight));
        }

        public void PostProcess()
        {
            foreach (var storyboardObject in storyboardObjects)
                (storyboardObject as HasPostProcess)?.PostProcess();

            segments.ForEach(s => s.PostProcess());
        }

        public void WriteOsbSprites(TextWriter writer, ExportSettings exportSettings, OsbLayer osbLayer)
        {
            foreach (var sbo in storyboardObjects)
                sbo.WriteOsb(writer, exportSettings, osbLayer);

            segments.ForEach(s => s.WriteOsbSprites(writer, exportSettings, osbLayer));
        }

        public int CalculateSize(OsbLayer osbLayer)
        {
            var exportSettings = new ExportSettings
            {
                OptimiseSprites = false, // reduce update time for a minor inaccuracy in estimatedSize
            };

            using (var stream = new ByteCounterStream())
            using (var writer = new StreamWriter(stream, Project.Encoding))
            {
                foreach (var sbo in storyboardObjects)
                    sbo.WriteOsb(writer, exportSettings, osbLayer);
                segments.ForEach(s => s.WriteOsbSprites(writer, exportSettings, osbLayer));

                return (int)stream.Length;
            }
        }
    }
}

