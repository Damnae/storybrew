using StorybrewCommon.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StorybrewCommon.Subtitles
{
    public class SbvParser
    {
        public SubtitleSet Parse (String path) 
        {
            string[] rawLines = System.IO.File.ReadAllLines(path);
            List<SubtitleLine> lines = new List<SubtitleLine>();

            double sTime = 0;
            double eTime = 0;
            String text = "";
            foreach (String line in rawLines) 
            {
                String[] times = line.Split(','); //Get timing Strings.
                if(times.Length == 2 && !line.Contains(" ")) //Is line a blockstart?
                { 
                    if(times[0].Split(':').Length == 3 && times[1].Split(':').Length == 3) //Make sure that this is a blockstart!
                    { 
                        if(text != "") //Had the previous block content?
                        { 
                            lines.Add(new SubtitleLine(sTime, eTime, text)); //Apply previous block
                            text = ""; //Reset Text
                        }
                        
                        sTime = parseTimestamp(times[0]); //Parse timestamp for StartTime
                        eTime = parseTimestamp(times[1]); //Parse timestamp for EndTime
                        continue;
                    }
                }
                text += line; //Add line to blocktext
            }
            if(text != "") lines.Add(new SubtitleLine(sTime, eTime, text)); //Add last block
            return new SubtitleSet(lines);
        }

        private double parseTimestamp(string timestamp)
            => TimeSpan.Parse(timestamp).TotalMilliseconds;
    }
}
