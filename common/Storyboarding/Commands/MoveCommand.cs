using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class MoveCommand : Command<CommandPosition>
    {
        public MoveCommand(OsbEasing easing, double startTime, double endTime, CommandPosition startValue, CommandPosition endValue)
            : base("M", easing, startTime, endTime, startValue, endValue)
        {
        }

        public override CommandPosition GetTransformedStartValue(StoryboardTransform transform) => transform.ApplyToPosition(StartValue);
        public override CommandPosition GetTransformedEndValue(StoryboardTransform transform) => transform.ApplyToPosition(EndValue);

        public override CommandPosition ValueAtProgress(double progress)
            => StartValue + (EndValue - StartValue) * progress;

        public override CommandPosition Midpoint(Command<CommandPosition> endCommand, double progress)
            => new CommandPosition(StartValue.X + (endCommand.EndValue.X - StartValue.X) * progress, StartValue.Y + (endCommand.EndValue.Y - StartValue.Y) * progress);

        public override IFragmentableCommand GetFragment(double startTime, double endTime)
        {
            if (IsFragmentable)
            {
                var startValue = ValueAtTime(startTime);
                var endValue = ValueAtTime(endTime);
                return new MoveCommand(Easing, startTime, endTime, startValue, endValue);
            }
            return this;
        }

        //public static IEnumerable<MoveCommand> InterpolateFrom(IEnumerable<MoveXCommand> xcommands, IEnumerable<MoveYCommand> ycommands)
        //{
        //    xcommands = xcommands.OrderBy((e) => e.StartTime);
        //    ycommands = ycommands.OrderBy((e) => e.StartTime);

            
        //    double endTime = Math.Max(xcommands.Last().EndTime, ycommands.Last().EndTime);

        //    Command<CommandDecimal> xCommand;
        //    Command<CommandDecimal> yCommand;

        //    int xIndex = 0, yIndex = 0;
        //    int xStart, yStart, xEnd, yEnd;

        //    while (xcommands.Any() || ycommands.Any())
        //    {
        //        xCommand = xcommands.ElementAtOrDefault(xIndex);
        //        yCommand = ycommands.ElementAtOrDefault(yIndex);

        //        xStart = (int)xCommand.StartTime;
        //        xEnd = (int)xCommand.EndTime;

        //        yStart = (int)yCommand.StartTime;
        //        yEnd = (int)yCommand.EndTime;

        //        //exact match
        //        if (xStart == yStart && xEnd == yEnd &&
        //            xCommand.Easing == yCommand.Easing)
        //        {
        //            yield return new MoveCommand(
        //                xCommand.Easing, xCommand.StartTime, xCommand.EndTime,
        //                new CommandPosition(xCommand.StartValue, yCommand.StartValue), new CommandPosition(xCommand.EndValue, yCommand.EndValue));

        //            xIndex++;
        //            yIndex++;
                    
        //        }

        //        if (xStart > yEnd)
        //        {

        //        }  

        //}
    }
}