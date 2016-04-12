using System;
using System.Collections.Generic;

namespace StorybrewCommon.Mapset
{
    public abstract class Beatmap : MarshalByRefObject
    {
        /// <summary>
        /// In milliseconds
        /// </summary>
        public const int ControlPointLeniency = 5;

        /// <summary>
        /// Timestamps in milliseconds of bookmarks
        /// </summary>
        public abstract IEnumerable<int> Bookmarks { get; }

        /// <summary>
        /// Returns all controls points (red or green lines).
        /// </summary>
        public abstract IEnumerable<ControlPoint> ControlPoints { get; }

        /// <summary>
        /// Returns all timing points (red lines).
        /// </summary>
        public abstract IEnumerable<ControlPoint> TimingPoints { get; }
        
        /// <summary>
        /// Finds the control point (red or green line) active at a specific time.
        /// </summary>
        public abstract ControlPoint GetControlPointAt(int time);
        
        /// <summary>
        /// Finds the timing point (red line) active at a specific time.
        /// </summary>
        public abstract ControlPoint GetTimingPointAt(int time);
    }
}
