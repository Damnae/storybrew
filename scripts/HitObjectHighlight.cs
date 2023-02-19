using OpenTK;
using StorybrewCommon.Mapset;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Animations;

namespace StorybrewScripts
{
    class HitObjectHighlight : StoryboardObjectGenerator
    {
        [Group("Timing")]
        [Configurable] public int StartTime = 0;
        [Configurable] public int EndTime = 0;
        [Configurable] public int BeatDivisor = 8;

        [Group("Sprite")]
        [Configurable] public string SpritePath = "sb/glow.png";
        [Configurable] public double SpriteScale = 1;
        [Configurable] public int FadeDuration = 200;

        protected override void Generate()
        {
            foreach (var hitobject in Beatmap.HitObjects)
            {
                if ((StartTime != 0 || EndTime != 0) && (hitobject.StartTime < StartTime - 5 || EndTime - 5 <= hitobject.StartTime))
                    continue;

                var hSprite = GetLayer("").CreateSprite(SpritePath, OsbOrigin.Centre, hitobject.Position + hitobject.StackOffset);
                hSprite.Scale(OsbEasing.In, hitobject.StartTime, hitobject.EndTime + FadeDuration, SpriteScale, SpriteScale * 0.2);
                hSprite.Fade(OsbEasing.In, hitobject.StartTime, hitobject.EndTime + FadeDuration, 1, 0);
                hSprite.Additive(hitobject.StartTime, hitobject.EndTime + FadeDuration);
                hSprite.Color(hitobject.StartTime, hitobject.Color);

                if (hitobject is OsuSlider)
                {
                    var keyframe = new KeyframedValue<Vector2>(null);
                    var timestep = Beatmap.GetTimingPointAt((int)hitobject.StartTime).BeatDuration / BeatDivisor;
                    var startTime = hitobject.StartTime;

                    while (true)
                    {
                        var endTime = startTime + timestep;

                        var complete = hitobject.EndTime - startTime < 5;
                        if (complete) endTime = hitobject.EndTime;

                        var startPosition = hitobject.PositionAtTime(startTime);
                        keyframe.Add(startTime, startPosition);
                        keyframe.Simplify2dKeyframes(1, v => v);

                        if (complete) break;
                        startTime += timestep;
                    }
                    keyframe.ForEachPair((sTime, eTime) => hSprite.Move(sTime.Time, eTime.Time, sTime.Value, eTime.Value));
                }
            }
        }
    }
}