using OpenTK.Graphics;
using System;
using System.Collections.Generic;

namespace StorybrewCommon.Mapset
{
    ///<summary> Represents an osu! beatmap difficulty. </summary>
    public abstract class Beatmap : MarshalByRefObject
    {
        ///<summary> In milliseconds </summary>
        public const int ControlPointLeniency = 5;

        ///<summary> This beatmap difficulty name, also called version. </summary>
        public abstract string Name { get; }

        ///<summary> The osu! beatmap ID of this beatmap. </summary>
        public abstract long Id { get; }

        ///<summary> The audio filename that the beatmap uses. </summary>
        public abstract string AudioFilename { get; }

        ///<summary> The background image path that the beatmap uses. </summary>
        public abstract string BackgroundPath { get; }

        ///<summary> The HP drain rate of this difficulty. </summary>
        public abstract double HpDrainRate { get; }

        ///<summary> The hit object size of this difficulty. </summary>
        public abstract double CircleSize { get; }

        ///<summary> The overall difficulty of this difficulty. </summary>
        public abstract double OverallDifficulty { get; }

        ///<summary> The object approach rate of this difficulty. </summary>
        public abstract double ApproachRate { get; }

        ///<summary> The slider velocity multiplier of this difficulty. </summary>
        public abstract double SliderMultiplier { get; }

        ///<summary> The slider tick rate of this difficulty. </summary>
        public abstract double SliderTickRate { get; }

        ///<summary> The object stacking leniency of this difficulty. </summary>
        public abstract double StackLeniency { get; }

        ///<summary> Hit objects of this difficulty. </summary>
        public abstract IEnumerable<OsuHitObject> HitObjects { get; }

        ///<summary> Timestamps in milliseconds of bookmarks </summary>
        public abstract IEnumerable<int> Bookmarks { get; }

        ///<summary> Returns all controls points (red or green lines). </summary>
        public abstract IEnumerable<ControlPoint> ControlPoints { get; }

        ///<summary> Returns all timing points (red lines). </summary>
        public abstract IEnumerable<ControlPoint> TimingPoints { get; }

        ///<summary> Returns the hit circle combo colors of this difficulty. </summary>
        public abstract IEnumerable<Color4> ComboColors { get; }

        ///<summary> Returns the breaks of this difficulty. </summary>
        public abstract IEnumerable<OsuBreak> Breaks { get; }

        ///<summary> Finds the control point (red or green line) active at a specific time. </summary>
        public abstract ControlPoint GetControlPointAt(int time);

        ///<summary> Finds the timing point (red line) active at a specific time. </summary>
        public abstract ControlPoint GetTimingPointAt(int time);

        ///<summary/>
        public static double GetDifficultyRange(double difficulty, double min, double mid, double max)
        {
            if (difficulty > 5) return mid + (max - mid) * (difficulty - 5) / 5;
            if (difficulty < 5) return mid - (mid - min) * (5 - difficulty) / 5;
            return mid;
        }
    }
}