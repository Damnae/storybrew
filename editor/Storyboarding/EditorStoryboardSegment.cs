using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Util;
using OpenTK;
using StorybrewCommon.Storyboarding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace StorybrewEditor.Storyboarding
{
    public class EditorStoryboardSegment : StoryboardSegment, DisplayableObject, HasPostProcess
    {
        public Effect Effect { get; }
        public EditorStoryboardLayer Layer { get; }
        public override string Identifier { get; }

        private double startTime;
        public override double StartTime => startTime;

        private double endTime;
        public override double EndTime => endTime;

        public bool Highlight;

        public override Vector2 Origin { get; set; }
        public override Vector2 Position { get; set; }
        public override double Rotation { get; set; }
        public override double Scale { get; set; } = 1f;
        public override bool ReverseDepth { get; set; }

        public event ChangedHandler OnChanged;
        protected void RaiseChanged(string propertyName)
            => EventHelper.InvokeStrict(() => OnChanged, d => ((ChangedHandler)d)(this, new ChangedEventArgs(propertyName)));

        private readonly List<StoryboardObject> storyboardObjects = new List<StoryboardObject>();
        private readonly List<DisplayableObject> displayableObjects = new List<DisplayableObject>();
        private readonly List<EventObject> eventObjects = new List<EventObject>();
        private readonly List<EditorStoryboardSegment> segments = new List<EditorStoryboardSegment>();

        private List<DisplayableObject>[] displayableBuckets;

        public EditorStoryboardSegment(Effect effect, EditorStoryboardLayer layer, string identifier = null)
        {
            Effect = effect;
            Layer = layer;
            Identifier = identifier;
        }

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
            displayableBuckets = null;
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
            displayableBuckets = null;
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

        private readonly Dictionary<string, EditorStoryboardSegment> namedSegments = new Dictionary<string, EditorStoryboardSegment>();
        public override IEnumerable<StoryboardSegment> NamedSegments => namedSegments.Values;

        public override StoryboardSegment CreateSegment(string identifier = null)
        {
            if (identifier != null)
            {
                var originalName = identifier;
                var count = 1;
                while (namedSegments.ContainsKey(identifier))
                {
                    count++;
                    identifier = $"{originalName}#{count}";
                }
            }
            return getSegment(identifier);
        }
        public override StoryboardSegment GetSegment(string identifier) => getSegment(identifier);

        private StoryboardSegment getSegment(string identifier = null)
        {
            if (identifier != null && Identifier == null)
                throw new InvalidOperationException($"Cannot add a named segment to a segment that isn't named ({identifier})");

            if (identifier == null || !namedSegments.TryGetValue(identifier, out var segment))
            {
                segment = new EditorStoryboardSegment(Effect, Layer, identifier);
                storyboardObjects.Add(segment);
                displayableObjects.Add(segment);
                displayableBuckets = null;
                segments.Add(segment);

                if (identifier != null)
                    namedSegments.Add(identifier, segment);
            }
            return segment;
        }

        public override void Discard(StoryboardObject storyboardObject)
        {
            storyboardObjects.Remove(storyboardObject);
            if (storyboardObject is DisplayableObject displayableObject)
            {
                displayableObjects.Remove(displayableObject);
                displayableBuckets = null;
            }
            if (storyboardObject is EventObject eventObject)
                eventObjects.Remove(eventObject);
            if (storyboardObject is EditorStoryboardSegment segment)
            {
                segments.Remove(segment);
                if (segment.Identifier != null)
                    namedSegments.Remove(segment.Identifier);
            }
        }

        public void TriggerEvents(double fromTime, double toTime)
        {
            foreach (var eventObject in eventObjects)
                if (fromTime <= eventObject.EventTime && eventObject.EventTime < toTime)
                    eventObject.TriggerEvent(Effect.Project, toTime);
            segments.ForEach(s => s.TriggerEvents(fromTime, toTime));
        }

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, StoryboardTransform transform, Project project, FrameStats frameStats)
        {
            var displayTime = project.DisplayTime * 1000;
            if (displayTime < StartTime || EndTime < displayTime)
                return;

            if (Layer.Highlight || Effect.Highlight)
                opacity *= (float)((Math.Sin(drawContext.Get<Editor>().TimeSource.Current * 4) + 1) * 0.5);

            var localTransform = new StoryboardTransform(transform, Origin, Position, Rotation, (float)Scale);
            if (displayableObjects.Count < 1000)
            {
                foreach (var displayableObject in displayableObjects)
                    displayableObject.Draw(drawContext, camera, bounds, opacity, localTransform, project, frameStats);
            }
            else
            {
                var bucketLength = 10000;
                var segmentDuration = EndTime - StartTime;

                var bucketCount = Math.Max(1, (int)Math.Ceiling(segmentDuration / bucketLength));
                var currentBucketIndex = (int)((displayTime - StartTime) / bucketLength);

                if (displayableBuckets == null)
                {
                    Debug.Print($"Creating {bucketCount} display buckets for {displayableObjects.Count} sprites");
                    displayableBuckets = new List<DisplayableObject>[bucketCount];
                }

                var currentBucket = displayableBuckets[currentBucketIndex];
                if (currentBucket == null)
                {
                    var bucketStartTime = StartTime + currentBucketIndex * bucketLength;
                    var bucketEndTime = StartTime + (currentBucketIndex + 1) * bucketLength;
                    displayableBuckets[currentBucketIndex] = currentBucket = new List<DisplayableObject>();

                    foreach (var displayableObject in displayableObjects)
                        if (displayableObject.StartTime <= bucketEndTime && bucketStartTime <= displayableObject.EndTime)
                        {
                            currentBucket.Add(displayableObject);
                            displayableObject.Draw(drawContext, camera, bounds, opacity, localTransform, project, frameStats);
                        }
                }
                else
                    foreach (var displayableObject in currentBucket)
                        displayableObject.Draw(drawContext, camera, bounds, opacity, localTransform, project, frameStats);
            }
        }

        public void PostProcess()
        {
            if (ReverseDepth)
            {
                storyboardObjects.Reverse();
                displayableObjects.Reverse();
            }

            foreach (var storyboardObject in storyboardObjects)
                (storyboardObject as HasPostProcess)?.PostProcess();

            startTime = double.MaxValue;
            endTime = double.MinValue;
            foreach (var sbo in storyboardObjects)
            {
                startTime = Math.Min(startTime, sbo.StartTime);
                endTime = Math.Max(endTime, sbo.EndTime);
            }
            displayableBuckets = null;
        }

        public override void WriteOsb(TextWriter writer, ExportSettings exportSettings, OsbLayer osbLayer, StoryboardTransform transform, CancellationToken token = default)
        {
            var localTransform = new StoryboardTransform(transform, Origin, Position, Rotation, (float)Scale);
            foreach (var sbo in storyboardObjects)
            {
                token.ThrowIfCancellationRequested();
                sbo.WriteOsb(writer, exportSettings, osbLayer, localTransform, token);
            }
        }

        public int CalculateSize(OsbLayer osbLayer, CancellationToken token = default)
        {
            var exportSettings = ExportSettings.SizeCalculation;

            using (var stream = new ByteCounterStream())
            using (var writer = new StreamWriter(stream, Project.Encoding))
            {
                foreach (var sbo in storyboardObjects)
                {
                    token.ThrowIfCancellationRequested();
                    sbo.WriteOsb(writer, exportSettings, osbLayer, null, token);
                }

                return (int)stream.Length;
            }
        }
    }
}

