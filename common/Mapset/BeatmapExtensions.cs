using System;

namespace StorybrewCommon.Mapset
{
    public static class BeatmapExtensions
    {
        /// <summary>
        /// Calls tickAction with timingPoint, time, beatCount, tickCount
        /// </summary>
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
                    var step = timingPoint.BeatDuration / snapDivisor;
                    var sectionStartTime = timingPoint.Offset;
                    var sectionEndTime = Math.Min(nextTimingPoint?.Offset ?? endTime, endTime);
                    if (timingPoint == leftTimingPoint)
                        while (startTime < sectionStartTime)
                        {
                            sectionStartTime -= step;
                            tickCount--;
                            if (tickCount % snapDivisor == 0)
                                beatCount--;
                        }

                    for (var time = sectionStartTime; time < sectionEndTime + Beatmap.ControlPointLeniency; time += step)
                    {
                        if (startTime < time)
                            tickAction(timingPoint, time, beatCount, tickCount);

                        if (tickCount % snapDivisor == 0)
                            beatCount++;
                        tickCount++;
                    }
                    timingPoint = nextTimingPoint;
                }
            }
        }
    }
}
