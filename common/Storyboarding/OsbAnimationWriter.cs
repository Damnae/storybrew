using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Storyboarding.Display;

namespace StorybrewCommon.Storyboarding
{
    public class OsbAnimationWriter : OsbSpriteWriter
    {
        private readonly OsbAnimation osbAnimation;
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
            this.osbAnimation = osbAnimation;
        }

        protected override OsbSprite CreateSprite(List<IFragmentableCommand> segment)
        {
            if (osbAnimation.LoopType == OsbLoopType.LoopOnce && segment.Min(c => c.StartTime) >= osbAnimation.AnimationEndTime)
            {
                //this shouldn't loop again so we need a sprite instead
                var sprite = new OsbSprite()
                {
                    InitialPosition = osbAnimation.InitialPosition,
                    Origin = osbAnimation.Origin,
                    TexturePath = getLastFramePath(),
                };

                foreach (var command in segment)
                    sprite.AddCommand(command);

                return sprite;
            }
            else
            {
                var animation = new OsbAnimation()
                {
                    TexturePath = osbAnimation.TexturePath,
                    InitialPosition = osbAnimation.InitialPosition,
                    Origin = osbAnimation.Origin,
                    FrameCount = osbAnimation.FrameCount,
                    FrameDelay = osbAnimation.FrameDelay,
                    LoopType = osbAnimation.LoopType,
                };

                foreach (var command in segment)
                    animation.AddCommand(command);

                return animation;
            }
        }

        protected override void WriteHeader(OsbSprite sprite, StoryboardTransform transform)
        {
            if (sprite is OsbAnimation animation)
            {
                var frameDelay = animation.FrameDelay;

                TextWriter.Write($"Animation");
                WriteHeaderCommon(sprite, transform);
                TextWriter.WriteLine($",{animation.FrameCount},{frameDelay.ToString(ExportSettings.NumberFormat)},{animation.LoopType}");
            }
            else base.WriteHeader(sprite, transform);
        }

        protected override HashSet<int> GetFragmentationTimes(IEnumerable<IFragmentableCommand> fragmentableCommands)
        {
            var fragmentationTimes = base.GetFragmentationTimes(fragmentableCommands);

            var tMax = fragmentationTimes.Max();
            var nonFragmentableTimes = new HashSet<int>();

            for (double d = osbAnimation.StartTime; d < osbAnimation.AnimationEndTime; d += osbAnimation.LoopDuration)
            {
                var range = Enumerable.Range((int)d + 1, (int)(osbAnimation.LoopDuration - 1));
                nonFragmentableTimes.UnionWith(range);
            }

            fragmentationTimes.RemoveWhere(t => nonFragmentableTimes.Contains(t) && t < tMax);

            return fragmentationTimes;
        }

        private string getLastFramePath()
        {
            var directory = Path.GetDirectoryName(osbAnimation.TexturePath);
            var file = string.Concat(Path.GetFileNameWithoutExtension(osbAnimation.TexturePath), osbAnimation.FrameCount - 1, Path.GetExtension(osbAnimation.TexturePath));
            return Path.Combine(directory, file);
        }
    }
}
