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
    public class OsbAnimationWriter : OsbSpriteWriter

    {
        private OsbAnimation OsbAnimation;
        public OsbAnimationWriter(OsbAnimation osbAnimation, AnimatedValue<CommandPosition> moveTimeline,
                                                             AnimatedValue<CommandDecimal> moveXTimeline,
                                                             AnimatedValue<CommandDecimal> moveYTimeline,
                                                             AnimatedValue<CommandDecimal> scaleTimeline,
                                                             AnimatedValue<CommandScale> scaleVecTimeline,
                                                             AnimatedValue<CommandDecimal> rotateTimeline,
                                                             AnimatedValue<CommandDecimal> fadeTimeline,
                                                             AnimatedValue<CommandColor> colorTimeline,
                                                             TextWriter writer, ExportSettings exportSettings, OsbLayer layer)
                                        : base(osbAnimation, moveTimeline,
                                                             moveXTimeline,
                                                             moveYTimeline,
                                                             scaleTimeline,
                                                             scaleVecTimeline,
                                                             rotateTimeline,
                                                             fadeTimeline,
                                                             colorTimeline,
                                                             writer, exportSettings, layer)
        {
            OsbAnimation = osbAnimation;
        }

        protected override OsbSprite CreateSprite(List<ICommand> segment)
        {
            if (OsbAnimation.LoopType == OsbLoopType.LoopOnce && segment.Min(c => c.StartTime) >= OsbAnimation.AnimationEndTime)
            {
                //this shouldn't loop again so we need a sprite instead
                var sprite = new OsbSprite()
                {
                    InitialPosition = OsbAnimation.InitialPosition,
                    Origin = OsbAnimation.Origin,
                    TexturePath = GetLastFramePath(),
                };

                foreach (var command in segment)
                    sprite.AddCommand(command);

                return sprite;
            }
            else
            {
                var animation = new OsbAnimation()
                {
                    TexturePath = OsbAnimation.TexturePath,
                    InitialPosition = OsbAnimation.InitialPosition,
                    Origin = OsbAnimation.Origin,
                    FrameCount = OsbAnimation.FrameCount,
                    FrameDelay = OsbAnimation.FrameDelay,
                    LoopType = OsbAnimation.LoopType,
                };

                foreach (var command in segment)
                    animation.AddCommand(command);

                return animation;
            }
        }

        private string GetLastFramePath()
        {
            var dir = Path.GetDirectoryName(OsbAnimation.TexturePath);
            var file = string.Concat(Path.GetFileNameWithoutExtension(OsbAnimation.TexturePath), OsbAnimation.FrameCount - 1, Path.GetExtension(OsbAnimation.TexturePath));
            return Path.Combine(dir, file);
        }

        protected override void WriteHeader(OsbSprite sprite)
        {
            if (sprite is OsbAnimation animation)
            {
                double frameDelay = animation.FrameDelay;
                TextWriter.WriteLine($"Animation,{OsbLayer},{animation.Origin},\"{OsbSprite.TexturePath.Trim()}\",{animation.InitialPosition.X.ToString(ExportSettings.NumberFormat)},{animation.InitialPosition.Y.ToString(ExportSettings.NumberFormat)},{animation.FrameCount},{frameDelay.ToString(ExportSettings.NumberFormat)},{animation.LoopType}");
            }
            else
            {
                base.WriteHeader(sprite);
            }           
        }

        protected override HashSet<int> GetFragmentationTimes()
        {
            HashSet<int> fragmentationTimes = base.GetFragmentationTimes();

            int tMax = fragmentationTimes.Max();
            HashSet<int> nonFragmentableTimes = new HashSet<int>();

            for (double d = OsbAnimation.StartTime; d < OsbAnimation.AnimationEndTime; d += OsbAnimation.LoopDuration)
            {
                var range = Enumerable.Range((int)d + 1, (int)(OsbAnimation.LoopDuration - 1));
                nonFragmentableTimes.UnionWith(range);
            }

            fragmentationTimes.RemoveWhere(t => nonFragmentableTimes.Contains(t) && t < tMax);

            return fragmentationTimes;
        }
    }
}
