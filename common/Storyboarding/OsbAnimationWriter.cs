using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Storyboarding.Display;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StorybrewCommon.Storyboarding
{
    ///<summary> Base class for writing and exporting an <see cref="OsbAnimation"/>. </summary>
    public class OsbAnimationWriter : OsbSpriteWriter
    {
        readonly OsbAnimation animation;
#pragma warning disable CS1591
        public OsbAnimationWriter(OsbAnimation animation,
            AnimatedValue<CommandPosition> move, AnimatedValue<CommandDecimal> moveX, AnimatedValue<CommandDecimal> moveY,
            AnimatedValue<CommandDecimal> scale, AnimatedValue<CommandScale> scaleVec,
            AnimatedValue<CommandDecimal> rotate,
            AnimatedValue<CommandDecimal> fade,
            AnimatedValue<CommandColor> color,
            TextWriter writer, ExportSettings exportSettings, OsbLayer layer) : base(animation,
                move, moveX, moveY,
                scale, scaleVec,
                rotate,
                fade,
                color,
                writer, exportSettings, layer) => this.animation = animation;

        protected override OsbSprite CreateSprite(List<IFragmentableCommand> segment)
        {
            if (animation.LoopType == OsbLoopType.LoopOnce && segment.Min(c => c.StartTime) >= animation.AnimationEndTime)
            {
                //this shouldn't loop again so we need a sprite instead
                var sprite = new OsbSprite
                {
                    InitialPosition = animation.InitialPosition,
                    Origin = animation.Origin,
                    TexturePath = getLastFramePath(),
                };

                foreach (var command in segment) sprite.AddCommand(command);
                return sprite;
            }
            else
            {
                var animation = new OsbAnimation
                {
                    TexturePath = this.animation.TexturePath,
                    InitialPosition = this.animation.InitialPosition,
                    Origin = this.animation.Origin,
                    FrameCount = this.animation.FrameCount,
                    FrameDelay = this.animation.FrameDelay,
                    LoopType = this.animation.LoopType,
                };

                foreach (var command in segment) animation.AddCommand(command);
                return animation;
            }
        }
        protected override void WriteHeader(OsbSprite sprite)
        {
            if (sprite is OsbAnimation animation)
            {
                var frameDelay = animation.FrameDelay;
                TextWriter.WriteLine($"Animation,{Layer},{animation.Origin},\"{animation.TexturePath.Trim()}\",{animation.InitialPosition.X.ToString(ExportSettings.NumberFormat)},{animation.InitialPosition.Y.ToString(ExportSettings.NumberFormat)},{animation.FrameCount},{frameDelay.ToString(ExportSettings.NumberFormat)},{animation.LoopType}");
            }
            else base.WriteHeader(sprite);
        }
        protected override HashSet<int> GetFragmentationTimes(IEnumerable<IFragmentableCommand> fragmentableCommands)
        {
            var fragmentationTimes = base.GetFragmentationTimes(fragmentableCommands);

            var tMax = fragmentationTimes.Max();
            var nonFragmentableTimes = new HashSet<int>();

            for (double d = animation.StartTime; d < animation.AnimationEndTime; d += animation.LoopDuration)
            {
                var range = Enumerable.Range((int)d + 1, (int)(animation.LoopDuration - 1));
                nonFragmentableTimes.UnionWith(range);
            }

            fragmentationTimes.RemoveWhere(t => nonFragmentableTimes.Contains(t) && t < tMax);
            return fragmentationTimes;
        }
        string getLastFramePath()
        {
            var directory = Path.GetDirectoryName(animation.TexturePath);
            var file = string.Concat(Path.GetFileNameWithoutExtension(animation.TexturePath),
                animation.FrameCount - 1, Path.GetExtension(animation.TexturePath));

            return Path.Combine(directory, file);
        }
    }
}