using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Storyboarding.Display;

namespace StorybrewCommon.Storyboarding
{
    public class OsbSpriteWriter

    {
        private OsbSprite osbSprite;
        private AnimatedValue<CommandPosition> moveTimeline;
        private AnimatedValue<CommandDecimal> moveXTimeline;
        private AnimatedValue<CommandDecimal> moveYTimeline;
        private AnimatedValue<CommandDecimal> scaleTimeline;
        private AnimatedValue<CommandScale> scaleVecTimeline;
        private AnimatedValue<CommandDecimal> rotateTimeline;
        private AnimatedValue<CommandDecimal> fadeTimeline;
        private AnimatedValue<CommandColor> colorTimeline;
        protected readonly TextWriter TextWriter;
        protected readonly ExportSettings ExportSettings;
        protected readonly OsbLayer OsbLayer;

        public OsbSpriteWriter(OsbSprite osbSprite, AnimatedValue<CommandPosition> moveTimeline,
                                                    AnimatedValue<CommandDecimal> moveXTimeline,
                                                    AnimatedValue<CommandDecimal> moveYTimeline,
                                                    AnimatedValue<CommandDecimal> scaleTimeline,
                                                    AnimatedValue<CommandScale> scaleVecTimeline,
                                                    AnimatedValue<CommandDecimal> rotateTimeline,
                                                    AnimatedValue<CommandDecimal> fadeTimeline,
                                                    AnimatedValue<CommandColor> colorTimeline, 
                                                    TextWriter writer, ExportSettings exportSettings, OsbLayer layer)
        {
            this.osbSprite = osbSprite;
            this.moveTimeline = moveTimeline;
            this.moveXTimeline = moveXTimeline;
            this.moveYTimeline = moveYTimeline;
            this.scaleTimeline = scaleTimeline;
            this.scaleVecTimeline = scaleVecTimeline;
            this.rotateTimeline = rotateTimeline;
            this.fadeTimeline = fadeTimeline;
            this.colorTimeline = colorTimeline;
            TextWriter = writer;
            ExportSettings = exportSettings;
            OsbLayer = layer;
        }

        public void WriteOsb()
        {
            if (osbSprite.MaxCommandCount > 0 && osbSprite.CommandCount > osbSprite.MaxCommandCount && IsFragmentable())
            {
                var fragmentationTimes = GetFragmentationTimes();
                var commands = osbSprite.Commands.ToList();

                while (commands.Count > 0)
                {
                    var segment = getNextSegment(fragmentationTimes, commands);
                    var sprite = CreateSprite(segment);                       
                    writeOsbSprite(sprite);
                }
            }
            else writeOsbSprite(osbSprite);
        }

        protected virtual OsbSprite CreateSprite(List<ICommand> segment)
        {
            var sprite = new OsbSprite()
            {
                TexturePath = osbSprite.TexturePath,
                InitialPosition = osbSprite.InitialPosition,
                Origin = osbSprite.Origin,
            };

            foreach (var command in segment)
                sprite.AddCommand(command);

            return sprite;
        }

        private void writeOsbSprite(OsbSprite sprite)
        {
            WriteHeader(sprite);
            foreach (var command in sprite.Commands)
                command.WriteOsb(TextWriter, ExportSettings, 1);
        }

        protected virtual void WriteHeader(OsbSprite sprite)
        {
            TextWriter.Write($"Sprite,{OsbLayer},{sprite.Origin},\"{sprite.TexturePath.Trim()}\"");
            if (!moveTimeline.HasCommands && !moveXTimeline.HasCommands)
                TextWriter.Write($",{sprite.InitialPosition.X.ToString(ExportSettings.NumberFormat)}");
            else TextWriter.Write($",0");
            if (!moveTimeline.HasCommands && !moveYTimeline.HasCommands)
                TextWriter.WriteLine($",{sprite.InitialPosition.Y.ToString(ExportSettings.NumberFormat)}");
            else TextWriter.WriteLine($",0");
        }

        protected virtual bool IsFragmentable()
        {
            if (osbSprite.CommandCount < osbSprite.MaxCommandCount)
                return false;

            return !(moveTimeline.HasOverlap ||
                     moveXTimeline.HasOverlap ||
                     moveYTimeline.HasOverlap ||
                     rotateTimeline.HasOverlap ||
                     scaleTimeline.HasOverlap ||
                     scaleVecTimeline.HasOverlap ||
                     fadeTimeline.HasOverlap ||
                     colorTimeline.HasOverlap);
        }

        protected virtual HashSet<int> GetFragmentationTimes()
        {
            var fragmentationTimes = new HashSet<int>();
            var nonFragmentableCommands = osbSprite.Commands.Where(c => !c.IsFragmentable());

            fragmentationTimes.UnionWith(Enumerable.Range((int)osbSprite.StartTime, (int)(osbSprite.EndTime - osbSprite.StartTime)));
            
            foreach (var command in nonFragmentableCommands)
            {
                var range = Enumerable.Range((int)command.StartTime + 1, (int)(command.EndTime - command.StartTime - 1));
                fragmentationTimes.ExceptWith(range);
            }

            return fragmentationTimes;
        }

        private List<ICommand> getNextSegment(HashSet<int> fragmentationTimes, List<ICommand> commands)
        {
            List<ICommand> segment = new List<ICommand>();

            var startTime = fragmentationTimes.Min();
            int endTime;
            var maxCommandCount = osbSprite.MaxCommandCount;

            //split the last 2 segments evenly so we don't have weird 5 command leftovers
            if (commands.Count < osbSprite.MaxCommandCount * 2 && commands.Count > osbSprite.MaxCommandCount)
                maxCommandCount = (int)Math.Ceiling(commands.Count / 2.0);

            if (commands.Count < maxCommandCount)
                endTime = fragmentationTimes.Max() + 1;
            else
            {
                var cEndTime = (int)commands.OrderBy(c => c.StartTime).ElementAt(maxCommandCount - 1).StartTime;
                if (fragmentationTimes.Contains(cEndTime))
                    endTime = cEndTime;
                else
                {
                    endTime = fragmentationTimes.Where(t => t < cEndTime).Max();
                    if (endTime == startTime) // segment can't be <= MaxCommandCount, so we use the smallest available
                        endTime = fragmentationTimes.First(t => t > startTime);
                }
            }

            foreach (var cmd in commands.Where(c => c.StartTime < endTime))
            {
                var sTime = Math.Max(startTime, (int)Math.Round(cmd.StartTime));
                var eTime = Math.Min(endTime, (int)Math.Round(cmd.EndTime));
                ICommand command;
                if (sTime == (int)Math.Round(cmd.StartTime) && eTime == (int)Math.Round(cmd.EndTime))
                {
                    command = cmd;
                }
                else
                {
                    var type = cmd.GetType();
                    var easingProp = type.GetProperty("Easing");
                    var valueAtMethod = type.GetMethod("ValueAtTime");
                    var startValue = valueAtMethod.Invoke(cmd, new object[] { sTime });
                    var endValue = valueAtMethod.Invoke(cmd, new object[] { eTime });
                    var easing = easingProp.GetValue(cmd);

                    if (!(cmd is ParameterCommand))
                        command = (ICommand)Activator.CreateInstance(type, new object[] { easing, sTime, eTime, startValue, endValue });
                    else
                        command = (ICommand)Activator.CreateInstance(type, new object[] { easing, sTime, eTime, startValue });
                }

                segment.Add(command);
            }

            addStaticCommands(segment, startTime);

            fragmentationTimes.RemoveWhere(t => t < endTime);
            commands.RemoveAll(c => c.EndTime <= endTime);

            return segment;
        }

        private void addStaticCommands(List<ICommand> segment, int startTime)
        {
            if (moveTimeline.HasCommands && !segment.Any(c => c is MoveCommand && c.StartTime == startTime))
            {
                var value = moveTimeline.ValueAtTime(startTime);
                segment.Add(new MoveCommand(OsbEasing.None, startTime, startTime, value, value));
            }

            if (moveXTimeline.HasCommands && !segment.Any(c => c is MoveXCommand && c.StartTime == startTime))
            {
                var value = moveXTimeline.ValueAtTime(startTime);
                segment.Add(new MoveXCommand(OsbEasing.None, startTime, startTime, value, value));
            }

            if (moveYTimeline.HasCommands && !segment.Any(c => c is MoveYCommand && c.StartTime == startTime))
            {
                var value = moveYTimeline.ValueAtTime(startTime);
                segment.Add(new MoveYCommand(OsbEasing.None, startTime, startTime, value, value));
            }

            if (rotateTimeline.HasCommands && !segment.Any(c => c is RotateCommand && c.StartTime == startTime))
            {
                var value = rotateTimeline.ValueAtTime(startTime);
                segment.Add(new RotateCommand(OsbEasing.None, startTime, startTime, value, value));
            }

            if (scaleTimeline.HasCommands && !segment.Any(c => c is ScaleCommand && c.StartTime == startTime))
            {
                var value = scaleTimeline.ValueAtTime(startTime);
                segment.Add(new ScaleCommand(OsbEasing.None, startTime, startTime, value, value));
            }

            if (scaleVecTimeline.HasCommands && !segment.Any(c => c is VScaleCommand && c.StartTime == startTime))
            {
                var value = scaleVecTimeline.ValueAtTime(startTime);
                segment.Add(new VScaleCommand(OsbEasing.None, startTime, startTime, value, value));
            }

            if (colorTimeline.HasCommands && !segment.Any(c => c is ColorCommand && c.StartTime == startTime))
            {
                var value = colorTimeline.ValueAtTime(startTime);
                segment.Add(new ColorCommand(OsbEasing.None, startTime, startTime, value, value));
            }

            if (fadeTimeline.HasCommands && !segment.Any(c => c is FadeCommand && c.StartTime == startTime))
            {
                var value = fadeTimeline.ValueAtTime(startTime);
                segment.Add(new FadeCommand(OsbEasing.None, startTime, startTime, value, value));
            }
        } 
    }

    static class OsbSpriteExtensions
    {
        public static bool IsFragmentable(this ICommand command)
        {
            if (command is ParameterCommand)
                return true;

            if (command.StartTime == command.EndTime)
                return true;

            var type = command.GetType();
            var easingProp = type.GetProperty("Easing");
            var easing = easingProp.GetValue(command);
            return (OsbEasing)easing == OsbEasing.None;
        }
    }
}
