using StorybrewCommon.Animations;
using StorybrewCommon.Storyboarding.CommandValues;
using System.IO;

namespace StorybrewCommon.Storyboarding.Commands
{
    public abstract class Command<TValue> : ITypedCommand<TValue>, IOffsetable
        where TValue : CommandValue
    {
        public string Identifier { get; set; }
        public OsbEasing Easing { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public double Duration => EndTime - StartTime;
        public TValue StartValue { get; set; }
        public TValue EndValue { get; set; }
        public bool Active => true;

        protected Command(string identifier, OsbEasing easing, double startTime, double endTime, TValue startValue, TValue endValue)
        {
            Identifier = identifier;
            Easing = easing;
            StartTime = startTime;
            EndTime = endTime;
            StartValue = startValue;
            EndValue = endValue;
        }

        public void Offset(double offset)
        {
            StartTime += offset;
            EndTime += offset;
        }

        public TValue ValueAtTime(double time)
        {
            if (time <= StartTime) return StartValue;
            if (EndTime <= time) return EndValue;

            var duration = EndTime - StartTime;
            var progress = Easing.Ease((time - StartTime) / duration);
            return ValueAtProgress(progress);
        }

        public abstract TValue ValueAtProgress(double progress);
        public abstract TValue Midpoint(Command<TValue> endCommand, double progress);

        public virtual string ToOsbString(ExportSettings exportSettings)
        {
            var startTimeString = ((int)StartTime).ToString(exportSettings.NumberFormat);
            var endTimeString = ((int)EndTime).ToString(exportSettings.NumberFormat);
            var startValueString = StartValue.ToOsbString(exportSettings);
            var endValueString = EndValue.ToOsbString(exportSettings);

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

        public virtual void WriteOsb(TextWriter writer, ExportSettings exportSettings, int indentation)
            => writer.WriteLine(new string(' ', indentation) + ToOsbString(exportSettings));

        public override string ToString()
            => ToOsbString(ExportSettings.Default);
    }
}
