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
    class OsbSpriteWriter
    {
        protected OsbSprite OsbSprite;
        AnimatedValue<CommandPosition> moveTimeline;
        AnimatedValue<CommandDecimal> moveXTimeline;
        AnimatedValue<CommandDecimal> moveYTimeline;
        AnimatedValue<CommandDecimal> scaleTimeline;
        AnimatedValue<CommandScale> scaleVecTimeline;
        AnimatedValue<CommandDecimal> rotateTimeline;
        AnimatedValue<CommandDecimal> fadeTimeline;
        AnimatedValue<CommandColor> colorTimeline;
        protected TextWriter TextWriter;
        protected ExportSettings ExportSettings;
        protected OsbLayer OsbLayer;

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
            OsbSprite = osbSprite;
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
            if (OsbSprite.CommandCount == 0)
                return;

            if (OsbSprite.CommandCount > OsbSprite.MaxCommandCount && IsFragmentable())
            {
                HashSet<int> fragmentationTimes = GetFragmentationTimes();

                List<ICommand> commands = OsbSprite.Commands.ToList();

                while (commands.Count > 0)
                {
                    var segment = GetNextSegment(fragmentationTimes, commands);
                    var sprite = CreateSprite(segment);                       
                    WriteOsbSprite(sprite);
                }
            }
            else
            {
                WriteOsbSprite(OsbSprite);
            }
        }

        protected virtual OsbSprite CreateSprite(List<ICommand> segment)
        {
            var sprite = new OsbSprite()
            {
                TexturePath = OsbSprite.TexturePath,
                InitialPosition = OsbSprite.InitialPosition,
                Origin = OsbSprite.Origin,
            };

            foreach (var command in segment)
                sprite.AddCommand(command);

            return sprite;
        }

        private void WriteOsbSprite(OsbSprite sprite)
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
            if (OsbSprite.CommandCount < OsbSprite.MaxCommandCount)
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
            HashSet<int> fragmentationTimes = new HashSet<int>();
            var nonFragmentableCommands = OsbSprite.Commands.Where(c => !c.IsFragmentable()).ToList();

            fragmentationTimes.UnionWith(Enumerable.Range((int)OsbSprite.Commands.Min(c => c.StartTime), (int)OsbSprite.Commands.Max(c => c.EndTime)));
            //Performance seems not so fresh here (e.g. when using an Easing for the spectrum commands)
            nonFragmentableCommands.ForEach(c => fragmentationTimes.RemoveWhere(t => t > c.StartTime && t < c.EndTime));

            return fragmentationTimes;
        }

        private List<ICommand> GetNextSegment(HashSet<int> fragmentationTimes, List<ICommand> commands)
        {
            List<ICommand> segment = new List<ICommand>();

            int startTime = fragmentationTimes.Min();
            int endTime;
            int maxCommandCount = OsbSprite.MaxCommandCount;

            //split the last 2 segments evenly so we don't have weird 5 command leftovers
            if (commands.Count < OsbSprite.MaxCommandCount * 2 && commands.Count > OsbSprite.MaxCommandCount)
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

            AddStaticCommands(segment, startTime);

            fragmentationTimes.RemoveWhere(t => t < endTime);
            commands.RemoveAll(c => c.EndTime <= endTime);

            return segment;
        }

        private void AddStaticCommands(List<ICommand> segment, int startTime)
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
        public static void AddCommand(this OsbSprite sprite, ICommand command)
        {
            if (command is ColorCommand colorCommand)
                sprite.Color(colorCommand.Easing, colorCommand.StartTime, colorCommand.EndTime, colorCommand.StartValue, colorCommand.EndValue);
            else if (command is FadeCommand fadeCommand)
                sprite.Fade(fadeCommand.Easing, fadeCommand.StartTime, fadeCommand.EndTime, fadeCommand.StartValue, fadeCommand.EndValue);
            else if (command is ScaleCommand scaleCommand)
                sprite.Scale(scaleCommand.Easing, scaleCommand.StartTime, scaleCommand.EndTime, scaleCommand.StartValue, scaleCommand.EndValue);
            else if (command is VScaleCommand vScaleCommand)
                sprite.ScaleVec(vScaleCommand.Easing, vScaleCommand.StartTime, vScaleCommand.EndTime, vScaleCommand.StartValue, vScaleCommand.EndValue);
            else if (command is ParameterCommand parameterCommand)
                sprite.Parameter(parameterCommand.Easing, parameterCommand.StartTime, parameterCommand.EndTime, parameterCommand.StartValue);
            else if (command is MoveCommand moveCommand)
                sprite.Move(moveCommand.Easing, moveCommand.StartTime, moveCommand.EndTime, moveCommand.StartValue, moveCommand.EndValue);
            else if (command is MoveXCommand moveXCommand)
                sprite.MoveX(moveXCommand.Easing, moveXCommand.StartTime, moveXCommand.EndTime, moveXCommand.StartValue, moveXCommand.EndValue);
            else if (command is MoveYCommand moveYCommand)
                sprite.MoveY(moveYCommand.Easing, moveYCommand.StartTime, moveYCommand.EndTime, moveYCommand.StartValue, moveYCommand.EndValue);
            else if (command is RotateCommand rotateCommand)
                sprite.Rotate(rotateCommand.Easing, rotateCommand.StartTime, rotateCommand.EndTime, rotateCommand.StartValue, rotateCommand.EndValue);
            else if (command is LoopCommand loopCommand)
            {
                sprite.StartLoopGroup(loopCommand.StartTime, loopCommand.LoopCount);
                foreach (var cmd in loopCommand.Commands)
                    AddCommand(sprite, cmd);
                sprite.EndGroup();
            }
            else if (command is TriggerCommand triggerCommand)
            {
                sprite.StartTriggerGroup(triggerCommand.TriggerName, triggerCommand.StartTime, triggerCommand.EndTime, triggerCommand.Group);
                foreach (var cmd in triggerCommand.Commands)
                    AddCommand(sprite, cmd);
                sprite.EndGroup();
            }
        }

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
