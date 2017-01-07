using OpenTK.Graphics;
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
        /// This beatmap diff name, also called version.
        /// </summary>
        public abstract string Name { get; }
        public abstract long Id { get; }

        public abstract double HpDrainRate { get; }
        public abstract double CircleSize { get; }
        public abstract double OverallDifficulty { get; }
        public abstract double ApproachRate { get; }
        public abstract double SliderMultiplier { get; }
        public abstract double SliderTickRate { get; }

        public abstract IEnumerable<OsuHitObject> HitObjects { get; }

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

        public abstract IEnumerable<Color4> ComboColors { get; }

        public abstract string BackgroundPath { get; }

        public abstract IEnumerable<OsuBreak> Breaks { get; }

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
