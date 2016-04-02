using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace StorybrewCommon.Mapset
{
    public class Beatmap : MarshalByRefObject
    {
        public readonly string Path;

        private List<ControlPoint> controlPoints = new List<ControlPoint>();
        public IEnumerable<ControlPoint> ControlPoints => controlPoints;
        public IEnumerable<ControlPoint> TimingPoints
        {
            get
            {
                foreach (var controlPoint in controlPoints)
                    if (!controlPoint.IsInherited)
                        yield return controlPoint;
            }
        }

        public Beatmap(string path)
        {
            Path = path;
        }

        public void Add(ControlPoint timingPoint) => controlPoints.Add(timingPoint);

        #region .osu parsing

        public static Beatmap Load(string path)
        {
            Trace.WriteLine($"Loading beatmap {path}");
            var beatmap = new Beatmap(path);

            string line;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        var sectionName = line.Substring(1, line.Length - 2);
                        switch (sectionName)
                        {
                            case "General": parseGeneralSection(beatmap, reader); break;
                            case "Metadata": parseMetadataSection(beatmap, reader); break;
                            case "Difficulty": parseDifficultySection(beatmap, reader); break;
                            case "TimingPoints": parseTimingPointsSection(beatmap, reader); break;
                            case "Events": parseEventsSection(beatmap, reader); break;
                            case "HitObjects": parseHitObjectsSection(beatmap, reader); break;
                        }
                    }
                }
            return beatmap;
        }

        private static void parseGeneralSection(Beatmap beatmap, StreamReader reader) { }
        private static void parseMetadataSection(Beatmap beatmap, StreamReader reader) { }
        private static void parseDifficultySection(Beatmap beatmap, StreamReader reader) { }
        private static void parseTimingPointsSection(Beatmap beatmap, StreamReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0) break;
                beatmap.Add(ControlPoint.Parse(line));
            }
        }
        private static void parseEventsSection(Beatmap beatmap, StreamReader reader) { }
        private static void parseHitObjectsSection(Beatmap beatmap, StreamReader reader) { }

        #endregion
    }
}
