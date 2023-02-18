using System;
using System.Collections.Generic;

namespace StorybrewCommon.Mapset
{
#pragma warning disable CS1591
    public static class BeatmapExtensions
    {
        ///<summary> Calls tickAction with timingPoint, time, beatCount, tickCount </summary>
        public static void ForEachTick(this Beatmap beatmap, int startTime, int endTime, int snapDivisor, Action<ControlPoint, double, int, int> tickAction)
        {
            var leftTimingPoint = beatmap.GetTimingPointAt(startTime);
            var timingPoints = beatmap.TimingPoints.GetEnumerator();

            if (timingPoints.MoveNext())
            {
                var timingPoint = timingPoints.Current;
                while (timingPoint != null)
                {
                    var nextTimingPoint = timingPoints.MoveNext() ? timingPoints.Current : null;
                    if (timingPoint.Offset < leftTimingPoint.Offset)
                    {
                        timingPoint = nextTimingPoint;
                        continue;
                    }
                    if (timingPoint != leftTimingPoint && endTime + Beatmap.ControlPointLeniency < timingPoint.Offset) break;

                    int tickCount = 0, beatCount = 0;
                    var step = Math.Max(1, timingPoint.BeatDuration / snapDivisor);
                    var sectionStartTime = timingPoint.Offset;
                    var sectionEndTime = Math.Min(nextTimingPoint?.Offset ?? endTime, endTime);
                    if (timingPoint == leftTimingPoint) while (startTime < sectionStartTime)
                    {
                        sectionStartTime -= step;
                        tickCount--;
                        if (tickCount % snapDivisor == 0) beatCount--;
                    }

                    for (var time = sectionStartTime; time < sectionEndTime + Beatmap.ControlPointLeniency; time += step)
                    {
                        if (startTime < time) tickAction(timingPoint, time, beatCount, tickCount);
                        if (tickCount % snapDivisor == 0) beatCount++;
                        tickCount++;
                    }
                    timingPoint = nextTimingPoint;
                }
            }
        }
        public static void AsSliderNodes(this IEnumerable<OsuHitObject> hitobjects, Action<OsuSliderNode, OsuHitObject> action)
        {
            foreach (var hitobject in hitobjects)
            {
                switch (hitobject)
                {
                    case OsuCircle circle: action(new OsuSliderNode
                    {
                        Time = circle.StartTime,
                        Additions = circle.Additions,
                        SampleSet = circle.SampleSet,
                        AdditionsSampleSet = circle.AdditionsSampleSet,
                        CustomSampleSet = circle.CustomSampleSet,
                        Volume = circle.Volume,
                    }, hitobject); break;

                    case OsuSlider slider: foreach (var node in slider.Nodes) action(node, hitobject); break;

                    case OsuSpinner spinner: action(new OsuSliderNode
                    {
                        Time = spinner.EndTime,
                        Additions = spinner.Additions,
                        SampleSet = spinner.SampleSet,
                        AdditionsSampleSet = spinner.AdditionsSampleSet,
                        CustomSampleSet = spinner.CustomSampleSet,
                        Volume = spinner.Volume,
                    }, hitobject); break;
                }
            }
        }
    }
}