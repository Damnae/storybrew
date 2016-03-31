using StorybrewCommon.Animations;
using StorybrewCommon.Storyboarding.CommandValues;
using System;
using System.Globalization;

namespace StorybrewCommon.Storyboarding.Commands
{
    public abstract class Command<TValue> : ITypedCommand<TValue>
        where TValue : CommandValue
    {
        public string Identifier { get; set; }
        public OsbEasing Easing { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public TValue StartValue { get; set; }
        public TValue EndValue { get; set; }
        public bool Enabled => true;

        protected Command(string identifier, OsbEasing easing, double startTime, double endTime, TValue startValue, TValue endValue)
        {
            Identifier = identifier;
            Easing = easing;
            StartTime = startTime;
            EndTime = endTime;
            StartValue = startValue;
            EndValue = endValue;
        }

        public TValue ValueAtTime(double time)
        {
            if (time < StartTime || EndTime < time)
                throw new InvalidOperationException($"No value for {time} ({StartTime} - {EndTime})");

            var duration = EndTime - StartTime;
            var progress = Easing.Ease((time - StartTime) / duration);
            return ValueAtProgress(progress);
        }

        public abstract TValue ValueAtProgress(double progress);
        public abstract TValue Midpoint(Command<TValue> endCommand, double progress);

        public virtual string ToOsbString(ExportSettings exportSettings)
        {
            var startTimeString = ((int)StartTime).ToString(CultureInfo.InvariantCulture);
            var endTimeString = ((int)EndTime).ToString(CultureInfo.InvariantCulture);
            var startValueString = StartValue.ToOsbString(exportSettings);
            var endValueString = EndValue.ToOsbString(exportSettings);

            if (startTimeString == endTimeString)
                endTimeString = string.Empty;

            string[] parameters =
            {
                Identifier, ((int)Easing).ToString(CultureInfo.InvariantCulture),
                startTimeString, endTimeString, startValueString
            };

            var result = string.Join(",", parameters);
            if (startValueString != endValueString)
                result += "," + endValueString;

            return result;
        }

        public override string ToString() 
            => ToOsbString(ExportSettings.Default);
    }
}
