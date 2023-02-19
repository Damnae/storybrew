using OpenTK;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StorybrewScripts
{
    class ImportOsb : StoryboardObjectGenerator
    {
        [Description("Path to the .osb to import, relative to the project folder.")]
        [Configurable] public string Path = "storyboard.osb";
        readonly Dictionary<string, string> vars = new Dictionary<string, string>();

        protected override void Generate()
        {
            using (var stream = OpenProjectFile(Path))
            using (var reader = new StreamReader(stream, new UTF8Encoding()))
            reader.ParseSections(section =>
            {
                switch (section)
                {
                    case "Variables": parseVariables(reader); break;
                    case "Events": parseEvents(reader); break;
                }
            });
        }
        void parseVariables(StreamReader reader) => reader.ParseSectionLines(line =>
        {
            var v = line.Split('=');
            if (v.Length == 2) vars.Add(v[0], v[1]);
        });
        void parseEvents(StreamReader reader)
        {
            OsbSprite sprite = null;
            var loopable = false;

            reader.ParseSectionLines(line =>
            {
                if (line.StartsWith("//")) return;

                var depth = 0;
                while (line.Substring(depth).StartsWith(" ")) ++depth;

                var trim = applyVariables(line.Trim());
                var v = trim.Split(',');

                if (loopable && depth < 2)
                {
                    sprite.EndGroup();
                    loopable = false;
                }

                switch (v[0])
                {
                    case "Sprite":
                    {
                        var origin = (OsbOrigin)Enum.Parse(typeof(OsbOrigin), v[2]);
                        var path = removeQuotes(v[3]);
                        var x = float.Parse(v[4]);
                        var y = float.Parse(v[5]);
                        sprite = GetLayer(v[1]).CreateSprite(path, origin, new Vector2(x, y));
                        break;
                    }
                    case "Animation":
                    {
                        var origin = (OsbOrigin)Enum.Parse(typeof(OsbOrigin), v[2]);
                        var path = removeQuotes(v[3]);
                        var x = float.Parse(v[4]);
                        var y = float.Parse(v[5]);
                        var frameCount = int.Parse(v[6]);
                        var frameDelay = double.Parse(v[7]);
                        var loopType = (OsbLoopType)Enum.Parse(typeof(OsbLoopType), v[8]);
                        sprite = GetLayer(v[1]).CreateAnimation(path, frameCount, frameDelay, loopType, origin, new Vector2(x, y));
                        break;
                    }
                    case "Sample":
                    {
                        GetLayer(v[2]).CreateSample(removeQuotes(v[3]), int.Parse(v[1]), float.Parse(v[4]));
                        break;
                    }
                    case "T":
                    {
                        sprite.StartTriggerGroup(v[1], int.Parse(v[2]), int.Parse(v[3]), v.Length > 4 ? int.Parse(v[4]) : 0);
                        loopable = true;
                        break;
                    }
                    case "L":
                    {
                        sprite.StartLoopGroup(int.Parse(v[1]), int.Parse(v[2]));
                        loopable = true;
                        break;
                    }
                    default:
                    {
                        if (string.IsNullOrEmpty(v[3])) v[3] = v[2];

                        var command = v[0];
                        var easing = (OsbEasing)int.Parse(v[1]);
                        var startTime = int.Parse(v[2]);
                        var endTime = int.Parse(v[3]);

                        switch (command)
                        {
                            case "F":
                            {
                                var startValue = double.Parse(v[4]);
                                var endValue = v.Length > 5 ? double.Parse(v[5]) : startValue;
                                sprite.Fade(easing, startTime, endTime, startValue, endValue);
                                break;
                            }
                            case "S":
                            {
                                var startValue = double.Parse(v[4]);
                                var endValue = v.Length > 5 ? double.Parse(v[5]) : startValue;
                                sprite.Scale(easing, startTime, endTime, startValue, endValue);
                                break;
                            }
                            case "V":
                            {
                                var startX = double.Parse(v[4]);
                                var startY = double.Parse(v[5]);
                                var endX = v.Length > 6 ? double.Parse(v[6]) : startX;
                                var endY = v.Length > 7 ? double.Parse(v[7]) : startY;
                                sprite.ScaleVec(easing, startTime, endTime, startX, startY, endX, endY);
                                break;
                            }
                            case "R":
                            {
                                var startValue = double.Parse(v[4]);
                                var endValue = v.Length > 5 ? double.Parse(v[5]) : startValue;
                                sprite.Rotate(easing, startTime, endTime, startValue, endValue);
                                break;
                            }
                            case "M":
                            {
                                var startX = double.Parse(v[4]);
                                var startY = double.Parse(v[5]);
                                var endX = v.Length > 6 ? double.Parse(v[6]) : startX;
                                var endY = v.Length > 7 ? double.Parse(v[7]) : startY;
                                sprite.Move(easing, startTime, endTime, startX, startY, endX, endY);
                                break;
                            }
                            case "MX":
                            {
                                var startValue = double.Parse(v[4]);
                                var endValue = v.Length > 5 ? double.Parse(v[5]) : startValue;
                                sprite.MoveX(easing, startTime, endTime, startValue, endValue);
                                break;
                            }
                            case "MY":
                            {
                                var startValue = double.Parse(v[4]);
                                var endValue = v.Length > 5 ? double.Parse(v[5]) : startValue;
                                sprite.MoveY(easing, startTime, endTime, startValue, endValue);
                                break;
                            }
                            case "C":
                            {
                                var startX = double.Parse(v[4]) / 255;
                                var startY = double.Parse(v[5]) / 255;
                                var startZ = double.Parse(v[6]) / 255;
                                var endX = v.Length > 7 ? double.Parse(v[7]) / 255 : startX;
                                var endY = v.Length > 8 ? double.Parse(v[8]) / 255 : startY;
                                var endZ = v.Length > 9 ? double.Parse(v[9]) / 255 : startZ;
                                sprite.Color(easing, startTime, endTime, startX, startY, startZ, endX, endY, endZ);
                                break;
                            }
                            case "P":
                            {
                                switch (v[4])
                                {
                                    case "A": sprite.Additive(startTime, endTime); break;
                                    case "H": sprite.FlipH(startTime, endTime); break;
                                    case "V": sprite.FlipV(startTime, endTime); break;
                                }
                                break;
                            }
                        }
                    }
                    break;
                }
            }, false);

            if (loopable)
            {
                sprite.EndGroup();
                loopable = false;
            }
        }

        string removeQuotes(string path) => path.StartsWith("\"") && path.EndsWith("\"") ? path.Substring(1, path.Length - 2) : path;
        string applyVariables(string line)
        {
            if (!line.Contains("$")) return line;
            foreach (var entry in vars) line = line.Replace(entry.Key, entry.Value);
            return line;
        }
    }
}