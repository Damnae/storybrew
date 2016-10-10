using OpenTK.Graphics;
using StorybrewCommon.Mapset;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace StorybrewEditor.Mapset
{
    public class EditorBeatmap : Beatmap
    {
        public readonly string Path;

        public string AudioFilename { get; set; }

        private string name;
        public override string Name => name;

        private long id;
        public override long Id => id;

        private List<int> bookmarks = new List<int>();
        public override IEnumerable<int> Bookmarks => bookmarks;

        private double sliderMultiplier;
        public override double SliderMultiplier => sliderMultiplier;

        private List<OsuHitObject> hitObjects = new List<OsuHitObject>();
        public override IEnumerable<OsuHitObject> HitObjects => hitObjects;

        private List<Color4> comboColors = new List<Color4>();
        public override IEnumerable<Color4> ComboColors => comboColors;

        public EditorBeatmap(string path)
        {
            Path = path;
        }

        #region Timing

        private List<ControlPoint> controlPoints = new List<ControlPoint>();

        public override IEnumerable<ControlPoint> ControlPoints => controlPoints;
        public override IEnumerable<ControlPoint> TimingPoints
        {
            get
            {
                var timingPoints = new List<ControlPoint>();
                foreach (var controlPoint in controlPoints)
                    if (!controlPoint.IsInherited)
                        timingPoints.Add(controlPoint);
                return timingPoints;
            }
        }

        public ControlPoint GetControlPointAt(int time, Predicate<ControlPoint> predicate)
        {
            if (controlPoints == null) return null;
            var closestTimingPoint = (ControlPoint)null;
            foreach (var controlPoint in controlPoints)
            {
                if (predicate != null && !predicate(controlPoint)) continue;
                if (closestTimingPoint == null || controlPoint.Offset - time <= ControlPointLeniency)
                    closestTimingPoint = controlPoint;
                else break;
            }
            return closestTimingPoint;
        }

        public override ControlPoint GetControlPointAt(int time)
            => GetControlPointAt(time, null);

        public override ControlPoint GetTimingPointAt(int time)
            => GetControlPointAt(time, cp => !cp.IsInherited);

        #endregion

        #region .osu parsing

        public static EditorBeatmap Load(string path)
        {
            Trace.WriteLine($"Loading beatmap {path}");
            var beatmap = new EditorBeatmap(path);

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                reader.ParseSections(sectionName =>
                {
                    switch (sectionName)
                    {
                        case "General": parseGeneralSection(beatmap, reader); break;
                        case "Editor": parseEditorSection(beatmap, reader); break;
                        case "Metadata": parseMetadataSection(beatmap, reader); break;
                        case "Difficulty": parseDifficultySection(beatmap, reader); break;
                        case "Events": parseEventsSection(beatmap, reader); break;
                        case "TimingPoints": parseTimingPointsSection(beatmap, reader); break;
                        case "Colours": parseColoursSection(beatmap, reader); break;
                        case "HitObjects": parseHitObjectsSection(beatmap, reader); break;
                    }
                });

            return beatmap;
        }

        private static void parseGeneralSection(EditorBeatmap beatmap, StreamReader reader)
        {
            reader.ParseKeyValueSection((key, value) =>
            {
                switch (key)
                {
                    case "AudioFilename": beatmap.AudioFilename = value; break;
                }
            });
        }
        private static void parseEditorSection(EditorBeatmap beatmap, StreamReader reader)
        {
            reader.ParseKeyValueSection((key, value) =>
            {
                switch (key)
                {
                    case "Bookmarks":
                        foreach (var bookmark in value.Split(','))
                            beatmap.bookmarks.Add(int.Parse(bookmark));
                        break;
                }
            });
        }
        private static void parseMetadataSection(EditorBeatmap beatmap, StreamReader reader)
        {
            reader.ParseKeyValueSection((key, value) =>
            {
                switch (key)
                {
                    case "Version": beatmap.name = value; break;
                    case "BeatmapID": beatmap.id = long.Parse(value); break;
                }
            });
        }
        private static void parseDifficultySection(EditorBeatmap beatmap, StreamReader reader)
        {
            reader.ParseKeyValueSection((key, value) =>
            {
                switch (key)
                {
                    case "SliderMultiplier": beatmap.sliderMultiplier = double.Parse(value, CultureInfo.InvariantCulture); break;
                }
            });
        }
        private static void parseTimingPointsSection(EditorBeatmap beatmap, StreamReader reader)
        {
            reader.ParseSectionLines(line => beatmap.controlPoints.Add(ControlPoint.Parse(line)));
            beatmap.controlPoints.Sort();
        }
        private static void parseColoursSection(EditorBeatmap beatmap, StreamReader reader)
        {
            reader.ParseKeyValueSection((key, value) =>
            {
                var rgb = value.Split(',');
                beatmap.comboColors.Add(new Color4(byte.Parse(rgb[0]), byte.Parse(rgb[1]), byte.Parse(rgb[2]), 255));
            });
        }
        private static void parseEventsSection(EditorBeatmap beatmap, StreamReader reader) { }
        private static void parseHitObjectsSection(EditorBeatmap beatmap, StreamReader reader)
        {
            OsuHitObject previousHitObject = null;
            var colorIndex = 0;
            var comboIndex = 0;

            if (!beatmap.comboColors.Any())
            {
                beatmap.comboColors.Add(new Color4(255, 192, 0, 255));
                beatmap.comboColors.Add(new Color4(0, 202, 0, 255));
                beatmap.comboColors.Add(new Color4(18, 124, 255, 255));
                beatmap.comboColors.Add(new Color4(242, 24, 57, 255));
            }

            reader.ParseSectionLines(line =>
            {
                var hitobject = OsuHitObject.Parse(beatmap, line);
                if (hitobject.NewCombo || previousHitObject == null || (previousHitObject.Flags & HitObjectFlag.Spinner) > 0)
                {
                    hitobject.Flags |= HitObjectFlag.NewCombo;
                    colorIndex = (colorIndex + 1 + hitobject.ComboOffset) % beatmap.comboColors.Count;
                    comboIndex = 1;
                }
                else comboIndex++;

                hitobject.ComboIndex = comboIndex;
                hitobject.ColorIndex = colorIndex;
                hitobject.Color = beatmap.comboColors[colorIndex];

                beatmap.hitObjects.Add(hitobject);
                previousHitObject = hitobject;
            });
        }

        #endregion
    }
}