using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Storyboarding.Display;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StorybrewCommon.Storyboarding
{
    ///<summary> A helper class for writing and exporting a storyboard. </summary>
    public class OsbSpriteWriter
    {
        readonly OsbSprite sprite;
        readonly AnimatedValue<CommandPosition> move;
        readonly AnimatedValue<CommandDecimal> moveX, moveY, scale, rotate, fade;
        readonly AnimatedValue<CommandScale> scaleVec;
        readonly AnimatedValue<CommandColor> color;
#pragma warning disable CS1591
        protected readonly TextWriter TextWriter;
        protected readonly ExportSettings ExportSettings;
        protected readonly OsbLayer Layer;

        public OsbSpriteWriter(OsbSprite sprite,
            AnimatedValue<CommandPosition> move, AnimatedValue<CommandDecimal> moveX, AnimatedValue<CommandDecimal> moveY,
            AnimatedValue<CommandDecimal> scale, AnimatedValue<CommandScale> scaleVec,
            AnimatedValue<CommandDecimal> rotate,
            AnimatedValue<CommandDecimal> fade,
            AnimatedValue<CommandColor> color,
            TextWriter writer, ExportSettings exportSettings, OsbLayer layer)
        {
            this.sprite = sprite;
            this.move = move;
            this.moveX = moveX;
            this.moveY = moveY;
            this.scale = scale;
            this.scaleVec = scaleVec;
            this.rotate = rotate;
            this.fade = fade;
            this.color = color;
            TextWriter = writer;
            ExportSettings = exportSettings;
            Layer = layer;
        }
        public void WriteOsb()
        {
            if (ExportSettings.OptimiseSprites && sprite.CommandSplitThreshold > 0 && sprite.CommandCount > sprite.CommandSplitThreshold && IsFragmentable())
            {
                var commands = sprite.Commands.Select(c => (IFragmentableCommand)c).ToList();
                var fragmentationTimes = GetFragmentationTimes(commands);

                while (commands.Count > 0)
                {
                    var segment = getNextSegment(fragmentationTimes, commands);
                    var sprite = CreateSprite(segment);
                    writeOsbSprite(sprite);
                }
            }
            else writeOsbSprite(sprite);
        }
        protected virtual OsbSprite CreateSprite(List<IFragmentableCommand> segment)
        {
            var sprite = new OsbSprite
            {
                TexturePath = this.sprite.TexturePath,
                InitialPosition = this.sprite.InitialPosition,
                Origin = this.sprite.Origin
            };
            foreach (var command in segment) sprite.AddCommand(command);
            return sprite;
        }
        void writeOsbSprite(OsbSprite sprite)
        {
            WriteHeader(sprite);
            foreach (var command in sprite.Commands) command.WriteOsb(TextWriter, ExportSettings, 1);
        }
        protected virtual void WriteHeader(OsbSprite sprite)
        {
            TextWriter.Write($"Sprite,{Layer},{sprite.Origin},\"{sprite.TexturePath.Trim()}\"");
            if (!move.HasCommands && !moveX.HasCommands) TextWriter.Write(
                $",{sprite.InitialPosition.X.ToString(ExportSettings.NumberFormat)}");
            else TextWriter.Write($",0");

            if (!move.HasCommands && !moveY.HasCommands) TextWriter.WriteLine(
                $",{sprite.InitialPosition.Y.ToString(ExportSettings.NumberFormat)}");
            else TextWriter.WriteLine($",0");
        }
        protected virtual bool IsFragmentable()
        {
            // if there are commands with non-deterministic results (aka triggercommands) the sprite can't reliably be split
            if (sprite.Commands.Any(c => !(c is IFragmentableCommand))) return false;

            return !(move.HasOverlap || moveX.HasOverlap || moveY.HasOverlap ||
                rotate.HasOverlap ||
                scale.HasOverlap || scaleVec.HasOverlap ||
                fade.HasOverlap ||
                color.HasOverlap);
        }
        protected virtual HashSet<int> GetFragmentationTimes(IEnumerable<IFragmentableCommand> fragCommands)
        {
            var fragTimes = new HashSet<int>(Enumerable.Range((int)sprite.StartTime, (int)(sprite.EndTime - sprite.StartTime) + 1));
            foreach (var command in fragCommands) fragTimes.ExceptWith(command.GetNonFragmentableTimes());
            return fragTimes;
        }
        List<IFragmentableCommand> getNextSegment(HashSet<int> fragmentationTimes, List<IFragmentableCommand> commands)
        {
            var segment = new List<IFragmentableCommand>();

            var startTime = fragmentationTimes.Min();
            var endTime = getSegmentEndTime(fragmentationTimes, commands);

            foreach (var cmd in commands.Where(c => c.StartTime < endTime))
            {
                var sTime = Math.Max(startTime, (int)Math.Round(cmd.StartTime));
                var eTime = Math.Min(endTime, (int)Math.Round(cmd.EndTime));

                IFragmentableCommand command;
                if (sTime != (int)Math.Round(cmd.StartTime) || eTime != (int)Math.Round(cmd.EndTime)) command = cmd.GetFragment(sTime, eTime);
                else command = cmd;

                segment.Add(command);
            }
            addStaticCommands(segment, startTime);

            fragmentationTimes.RemoveWhere(t => t < endTime);
            commands.RemoveAll(c => c.EndTime <= endTime);
            return segment;
        }
        int getSegmentEndTime(HashSet<int> fragmentationTimes, List<IFragmentableCommand> commands)
        {
            var startTime = fragmentationTimes.Min();
            int endTime;
            var maxCommandCount = sprite.CommandSplitThreshold;

            // split the last 2 segments evenly so we don't have weird 5 command leftovers
            if (commands.Count < sprite.CommandSplitThreshold * 2 && commands.Count > sprite.CommandSplitThreshold) maxCommandCount = (int)Math.Ceiling(commands.Count / 2d);
            if (commands.Count < maxCommandCount) endTime = fragmentationTimes.Max() + 1;
            else
            {
                var lastCommand = commands.OrderBy(c => c.StartTime).ElementAt(maxCommandCount - 1);
                if (fragmentationTimes.Contains((int)lastCommand.StartTime) && lastCommand.StartTime > startTime) endTime = (int)lastCommand.StartTime;
                else
                {
                    if (fragmentationTimes.Any(t => t < (int)lastCommand.StartTime))
                    {
                        endTime = fragmentationTimes.Where(t => t < (int)lastCommand.StartTime).Max();
                        if (endTime == startTime) endTime = fragmentationTimes.First(t => t > startTime);
                    }
                    else endTime = fragmentationTimes.First(t => t > startTime);
                }
            }
            return endTime;
        }
        void addStaticCommands(List<IFragmentableCommand> segment, int startTime)
        {
            if (move.HasCommands && !segment.Any(c => c is MoveCommand && c.StartTime == startTime))
            {
                var value = move.ValueAtTime(startTime);
                segment.Add(new MoveCommand(OsbEasing.None, startTime, startTime, value, value));
            }
            if (moveX.HasCommands && !segment.Any(c => c is MoveXCommand && c.StartTime == startTime))
            {
                var value = moveX.ValueAtTime(startTime);
                segment.Add(new MoveXCommand(OsbEasing.None, startTime, startTime, value, value));
            }
            if (moveY.HasCommands && !segment.Any(c => c is MoveYCommand && c.StartTime == startTime))
            {
                var value = moveY.ValueAtTime(startTime);
                segment.Add(new MoveYCommand(OsbEasing.None, startTime, startTime, value, value));
            }
            if (rotate.HasCommands && !segment.Any(c => c is RotateCommand && c.StartTime == startTime))
            {
                var value = rotate.ValueAtTime(startTime);
                segment.Add(new RotateCommand(OsbEasing.None, startTime, startTime, value, value));
            }
            if (scale.HasCommands && !segment.Any(c => c is ScaleCommand && c.StartTime == startTime))
            {
                var value = scale.ValueAtTime(startTime);
                segment.Add(new ScaleCommand(OsbEasing.None, startTime, startTime, value, value));
            }
            if (scaleVec.HasCommands && !segment.Any(c => c is VScaleCommand && c.StartTime == startTime))
            {
                var value = scaleVec.ValueAtTime(startTime);
                segment.Add(new VScaleCommand(OsbEasing.None, startTime, startTime, value, value));
            }
            if (color.HasCommands && !segment.Any(c => c is ColorCommand && c.StartTime == startTime))
            {
                var value = color.ValueAtTime(startTime);
                segment.Add(new ColorCommand(OsbEasing.None, startTime, startTime, value, value));
            }
            if (fade.HasCommands && !segment.Any(c => c is FadeCommand && c.StartTime == startTime))
            {
                var value = fade.ValueAtTime(startTime);
                segment.Add(new FadeCommand(OsbEasing.None, startTime, startTime, value, value));
            }
        }
    }
}