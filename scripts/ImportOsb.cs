using OpenTK;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace StorybrewScripts
{
    public class ImportOsb : StoryboardObjectGenerator
    {
        [Configurable]
        public string Path = "storyboard.osb";

        private Dictionary<string, string> variables = new Dictionary<string, string>();

        public override void Generate()
        {
            using (var stream = OpenProjectFile(Path))
            using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                reader.ParseSections(sectionName =>
                {
                    switch (sectionName)
                    {
                        case "Variables": parseVariablesSection(reader); break;
                        case "Events": parseEventsSection(reader); break;
                    }
                });
        }

        private void parseVariablesSection(StreamReader reader)
        {
            reader.ParseSectionLines(line =>
            {
                var values = line.Split('=');
                if (values.Length == 2)
                    variables.Add(values[0], values[1]);
            });
        }

        private void parseEventsSection(StreamReader reader)
        {
            OsbSprite osbSprite = null;
            var inCommandGroup = false;
            reader.ParseSectionLines(line =>
            {
                if (line.StartsWith("//")) return;

                var depth = 0;
                while (line.Substring(depth).StartsWith(" "))
                    ++depth;

                var trimmedLine = applyVariables(line.Trim());
                var values = trimmedLine.Split(',');

                if (inCommandGroup && depth < 2)
                {
                    osbSprite.EndGroup();
                    inCommandGroup = false;
                }

                switch (values[0])
                {
                    case "Sprite":
                        {
                            var layerName = values[1];
                            var origin = (OsbOrigin)Enum.Parse(typeof(OsbOrigin), values[2]);
                            var path = removePathQuotes(values[3]);
                            var x = float.Parse(values[4], CultureInfo.InvariantCulture);
                            var y = float.Parse(values[5], CultureInfo.InvariantCulture);
                            osbSprite = GetLayer(layerName).CreateSprite(path, origin, new Vector2(x, y));
                        }
                        break;
                    case "Animation":
                        {
                            var layerName = values[1];
                            var origin = (OsbOrigin)Enum.Parse(typeof(OsbOrigin), values[2]);
                            var path = removePathQuotes(values[3]);
                            var x = float.Parse(values[4], CultureInfo.InvariantCulture);
                            var y = float.Parse(values[5], CultureInfo.InvariantCulture);
                            var frameCount = int.Parse(values[6]);
                            var frameDelay = double.Parse(values[7], CultureInfo.InvariantCulture);
                            var loopType = (OsbLoopType)Enum.Parse(typeof(OsbLoopType), values[8]);
                            osbSprite = GetLayer(layerName).CreateAnimation(path, frameCount, frameDelay, loopType, origin, new Vector2(x, y));
                        }
                        break;
                    case "Sample":
                        {
                            var time = double.Parse(values[1], CultureInfo.InvariantCulture);
                            var layerName = values[2];
                            var path = removePathQuotes(values[3]);
                            var volume = float.Parse(values[4], CultureInfo.InvariantCulture);
                            GetLayer(layerName).CreateSample(path, time, volume);
                        }
                        break;
                    case "T":
                        {
                            var triggerName = values[1];
                            var startTime = double.Parse(values[2], CultureInfo.InvariantCulture);
                            var endTime = double.Parse(values[3], CultureInfo.InvariantCulture);
                            var groupNumber = values.Length > 4 ? int.Parse(values[4]) : 0;
                            osbSprite.StartTriggerGroup(triggerName, startTime, endTime, groupNumber);
                            inCommandGroup = true;
                        }
                        break;
                    case "L":
                        {
                            var startTime = double.Parse(values[1], CultureInfo.InvariantCulture);
                            var loopCount = int.Parse(values[2]);
                            osbSprite.StartLoopGroup(startTime, loopCount);
                            inCommandGroup = true;
                        }
                        break;
                    default:
                        {
                            if (string.IsNullOrEmpty(values[3]))
                                values[3] = values[2];

                            var commandType = values[0];
                            var easing = (OsbEasing)int.Parse(values[1]);
                            var startTime = double.Parse(values[2], CultureInfo.InvariantCulture);
                            var endTime = double.Parse(values[3], CultureInfo.InvariantCulture);

                            switch (commandType)
                            {
                                case "F":
                                    {
                                        var startValue = double.Parse(values[4], CultureInfo.InvariantCulture);
                                        var endValue = values.Length > 5 ? double.Parse(values[5], CultureInfo.InvariantCulture) : startValue;
                                        osbSprite.Fade(easing, startTime, endTime, startValue, endValue);
                                    }
                                    break;
                                case "S":
                                    {
                                        var startValue = double.Parse(values[4], CultureInfo.InvariantCulture);
                                        var endValue = values.Length > 5 ? double.Parse(values[5], CultureInfo.InvariantCulture) : startValue;
                                        osbSprite.Scale(easing, startTime, endTime, startValue, endValue);
                                    }
                                    break;
                                case "V":
                                    {
                                        var startX = double.Parse(values[4], CultureInfo.InvariantCulture);
                                        var startY = double.Parse(values[5], CultureInfo.InvariantCulture);
                                        var endX = values.Length > 6 ? double.Parse(values[6], CultureInfo.InvariantCulture) : startX;
                                        var endY = values.Length > 7 ? double.Parse(values[7], CultureInfo.InvariantCulture) : startY;
                                        osbSprite.ScaleVec(easing, startTime, endTime, startX, startY, endX, endY);
                                    }
                                    break;
                                case "R":
                                    {
                                        var startValue = double.Parse(values[4], CultureInfo.InvariantCulture);
                                        var endValue = values.Length > 5 ? double.Parse(values[5], CultureInfo.InvariantCulture) : startValue;
                                        osbSprite.Rotate(easing, startTime, endTime, startValue, endValue);
                                    }
                                    break;
                                case "M":
                                    {
                                        var startX = double.Parse(values[4], CultureInfo.InvariantCulture);
                                        var startY = double.Parse(values[5], CultureInfo.InvariantCulture);
                                        var endX = values.Length > 6 ? double.Parse(values[6], CultureInfo.InvariantCulture) : startX;
                                        var endY = values.Length > 7 ? double.Parse(values[7], CultureInfo.InvariantCulture) : startY;
                                        osbSprite.Move(easing, startTime, endTime, startX, startY, endX, endY);
                                    }
                                    break;
                                case "MX":
                                    {
                                        var startValue = double.Parse(values[4], CultureInfo.InvariantCulture);
                                        var endValue = values.Length > 5 ? double.Parse(values[5], CultureInfo.InvariantCulture) : startValue;
                                        osbSprite.MoveX(easing, startTime, endTime, startValue, endValue);
                                    }
                                    break;
                                case "MY":
                                    {
                                        var startValue = double.Parse(values[4], CultureInfo.InvariantCulture);
                                        var endValue = values.Length > 5 ? double.Parse(values[5], CultureInfo.InvariantCulture) : startValue;
                                        osbSprite.MoveY(easing, startTime, endTime, startValue, endValue);
                                    }
                                    break;
                                case "C":
                                    {
                                        var startX = double.Parse(values[4], CultureInfo.InvariantCulture);
                                        var startY = double.Parse(values[5], CultureInfo.InvariantCulture);
                                        var startZ = double.Parse(values[6], CultureInfo.InvariantCulture);
                                        var endX = values.Length > 7 ? double.Parse(values[7], CultureInfo.InvariantCulture) : startX;
                                        var endY = values.Length > 8 ? double.Parse(values[8], CultureInfo.InvariantCulture) : startY;
                                        var endZ = values.Length > 9 ? double.Parse(values[9], CultureInfo.InvariantCulture) : startZ;
                                        osbSprite.Color(easing, startTime, endTime, startX / 255f, startY / 255f, startZ / 255f, endX / 255f, endY / 255f, endZ / 255f);
                                    }
                                    break;
                                case "P":
                                    {
                                        var type = values[4];
                                        switch (type)
                                        {
                                            case "A": osbSprite.Additive(startTime, endTime); break;
                                            case "H": osbSprite.FlipH(startTime, endTime); break;
                                            case "V": osbSprite.FlipV(startTime, endTime); break;
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }, false);

            if (inCommandGroup)
            {
                osbSprite.EndGroup();
                inCommandGroup = false;
            }
        }

        private static string removePathQuotes(string path)
        {
            return path.StartsWith("\"") && path.EndsWith("\"") ? path.Substring(1, path.Length - 2) : path;
        }

        private string applyVariables(string line)
        {
            if (!line.Contains("$"))
                return line;

            foreach (var entry in variables)
                line = line.Replace(entry.Key, entry.Value);

            return line;
        }
    }
}
