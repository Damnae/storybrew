using OpenTK.Graphics;
using StorybrewCommon.Mapset;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace StorybrewEditor.Mapset
{
    public class EditorBeatmap : Beatmap
    {
        public readonly string Path;

        public string AudioFilename { get; set; }

        private string name = string.Empty;
        public override string Name => name;

        private long id;
        public override long Id => id;

        private List<int> bookmarks = new List<int>();
        public override IEnumerable<int> Bookmarks => bookmarks;

        private double hpDrainRate = 5;
        public override double HpDrainRate => hpDrainRate;

        private double circleSize = 5;
        public override double CircleSize => circleSize;

        private double overallDifficulty = 5;
        public override double OverallDifficulty => overallDifficulty;

        private double approachRate = 5;
        public override double ApproachRate => approachRate;

        private double sliderMultiplier = 1.4;
        public override double SliderMultiplier => sliderMultiplier;

        private double sliderTickRate = 1;
        public override double SliderTickRate => sliderTickRate;

        private List<OsuHitObject> hitObjects = new List<OsuHitObject>();
        public override IEnumerable<OsuHitObject> HitObjects => hitObjects;

        private List<Color4> comboColors = new List<Color4>()
        {
            new Color4(255, 192, 0, 255),
            new Color4(0, 202, 0, 255),
            new Color4(18, 124, 255, 255),
            new Color4(242, 24, 57, 255),
        };
        public override IEnumerable<Color4> ComboColors => comboColors;

        public string backgroundPath;
        public override string BackgroundPath => backgroundPath;

        private List<OsuBreak> breaks = new List<OsuBreak>();
        public override IEnumerable<OsuBreak> Breaks => breaks;

        public EditorBeatmap(string path)
        {
            Path = path;
        }

        public override string ToString() => Name;

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
            return closestTimingPoint ?? ControlPoint.Default;
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
            try
            {
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
            catch (Exception e)
            {
                throw new BeatmapLoadingException($"Failed to load beatmap \"{System.IO.Path.GetFileNameWithoutExtension(path)}\".", e);
            }
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
                    case "HPDrainRate": beatmap.hpDrainRate = double.Parse(value, CultureInfo.InvariantCulture); break;
                    case "CircleSize": beatmap.circleSize = double.Parse(value, CultureInfo.InvariantCulture); break;
                    case "OverallDifficulty": beatmap.overallDifficulty = double.Parse(value, CultureInfo.InvariantCulture); break;
                    case "ApproachRate": beatmap.approachRate = double.Parse(value, CultureInfo.InvariantCulture); break;
                    case "SliderMultiplier": beatmap.sliderMultiplier = double.Parse(value, CultureInfo.InvariantCulture); break;
                    case "SliderTickRate": beatmap.sliderTickRate = double.Parse(value, CultureInfo.InvariantCulture); break;
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
            beatmap.comboColors.Clear();
            reader.ParseKeyValueSection((key, value) =>
            {
                if (!key.StartsWith("Combo"))
                    return;

                var rgb = value.Split(',');
                beatmap.comboColors.Add(new Color4(byte.Parse(rgb[0]), byte.Parse(rgb[1]), byte.Parse(rgb[2]), 255));
            });
        }
        private static void parseEventsSection(EditorBeatmap beatmap, StreamReader reader)
        {
            reader.ParseSectionLines(line =>
            {
                if (line.StartsWith("//")) return;
                if (line.StartsWith(" ")) return;

                var values = line.Split(',');
                switch (values[0])
                {
                    case "0":
                        beatmap.backgroundPath = removePathQuotes(values[2]);
                        break;
                    case "2":
                        beatmap.breaks.Add(OsuBreak.Parse(beatmap, line));
                        break;
                }
            }, false);
        }
        private static void parseHitObjectsSection(EditorBeatmap beatmap, StreamReader reader)
        {
            OsuHitObject previousHitObject = null;
            var colorIndex = 0;
            var comboIndex = 0;

            reader.ParseSectionLines(line =>
            {
                var hitobject = OsuHitObject.Parse(beatmap, line);

                if (hitobject.NewCombo || previousHitObject == null || (previousHitObject.Flags & HitObjectFlag.Spinner) > 0)
                {
                    hitobject.Flags |= HitObjectFlag.NewCombo;

                    var colorIncrement = hitobject.ComboOffset;
                    if ((hitobject.Flags & HitObjectFlag.Spinner) == 0)
                        colorIncrement++;
                    colorIndex = (colorIndex + colorIncrement) % beatmap.comboColors.Count;
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

        private static string removePathQuotes(string path)
            => path.StartsWith("\"") && path.EndsWith("\"") ? path.Substring(1, path.Length - 2) : path;

        #endregion
    }
}