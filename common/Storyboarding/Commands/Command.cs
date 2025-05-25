using StorybrewCommon.Animations;
using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Storyboarding.Display;

namespace StorybrewCommon.Storyboarding.Commands
{
    public abstract class Command<TValue> : ITypedCommand<TValue>, IOffsetable
        where TValue : struct, CommandValue
    {
        public abstract string Identifier { get; }

        public OsbEasing Easing { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public TValue StartValue { get; set; }
        public TValue EndValue { get; set; }

        public double Duration => EndTime - StartTime;
        public virtual bool MaintainValue => true;
        public virtual bool ExportEndValue => true;
        public int Cost => 1;

        protected Command(OsbEasing easing, double startTime, double endTime, in TValue startValue, in TValue endValue)
        {
            Easing = easing;
            if (startTime <= endTime)
            {
                StartTime = startTime;
                EndTime = endTime;
                StartValue = startValue;
                EndValue = endValue;
            }
            else
            {
                // command ends before it start:
                // clamp its end time to its start time
                StartTime = startTime;
                EndTime = startTime;
                StartValue = startValue;
                EndValue = endValue;
            }
        }

        public CommandResult<TValue> AsResult(double timeOffset) => new(this, timeOffset);

        public virtual TValue GetTransformedStartValue(StoryboardTransform transform) => StartValue;
        public virtual TValue GetTransformedEndValue(StoryboardTransform transform) => EndValue;

        public void Offset(double offset)
        {
            StartTime += offset;
            EndTime += offset;
        }

        public TValue ValueAtTime(double time)
        {
            if (time < StartTime) return MaintainValue ? ValueAtProgress(0) : default;
            if (EndTime < time) return MaintainValue ? ValueAtProgress(1) : default;

            var duration = EndTime - StartTime;
            var progress = duration > 0 ? Easing.Ease((time - StartTime) / duration) : 0;
            return ValueAtProgress(progress);
        }

        public abstract TValue ValueAtProgress(double progress);
        public abstract TValue Midpoint(in Command<TValue> endCommand, double progress);

        public int CompareTo(ICommand other)
            => CommandComparer.CompareCommands(this, other);

        public virtual string ToOsbString(ExportSettings exportSettings, StoryboardTransform transform)
        {
            var startTimeString = (exportSettings.UseFloatForTime ? StartTime : (int)StartTime).ToString(exportSettings.NumberFormat);
            var endTimeString = (exportSettings.UseFloatForTime ? EndTime : (int)EndTime).ToString(exportSettings.NumberFormat);

            var tranformedStartValue = transform != null ? GetTransformedStartValue(transform) : StartValue;
            var tranformedEndValue = transform != null ? GetTransformedEndValue(transform) : EndValue;
            var startValueString = tranformedStartValue.ToOsbString(exportSettings);
            var endValueString = (ExportEndValue ? tranformedEndValue : tranformedStartValue).ToOsbString(exportSettings);

            if (startTimeString == endTimeString)
                endTimeString = string.Empty;

            string[] parameters =
            {
                Identifier, ((int)Easing).ToString(exportSettings.NumberFormat),
                startTimeString, endTimeString, startValueString
            };

            var result = string.Join(",", parameters);
            if (startValueString != endValueString)
                result += "," + endValueString;

            return result;
        }

        public virtual void WriteOsb(TextWriter writer, ExportSettings exportSettings, StoryboardTransform transform, int indentation)
            => writer.WriteLine(new string(' ', indentation) + ToOsbString(exportSettings, transform));

        public override string ToString()
            => ToOsbString(ExportSettings.Default, null);
    }
}
