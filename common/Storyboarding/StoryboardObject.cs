using System;
using System.IO;

namespace StorybrewCommon.Storyboarding
{
    ///<summary> Basic class for storyboarding objects. </summary>
    public abstract class StoryboardObject : MarshalByRefObject
    {
        ///<summary> Start time of this storyboard object. </summary>
        public abstract double StartTime { get; }

        ///<summary> End time of this storyboard object. </summary>
        public abstract double EndTime { get; }

#pragma warning disable CS1591
        public abstract void WriteOsb(TextWriter writer, ExportSettings exportSettings, OsbLayer layer);
    }
}