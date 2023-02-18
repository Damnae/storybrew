using StorybrewCommon.Mapset;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;

namespace StorybrewScripts
{
    public class HitObjectHighlight : StoryboardObjectGenerator
    {
        [Group("Timing")]
        [Configurable] public int StartTime = 0;
        [Configurable] public int EndTime = 0;
        [Configurable] public int BeatDivisor = 8;

        [Group("Sprite")]
        [Configurable] public string SpritePath = "sb/glow.png";
        [Configurable] public double SpriteScale = 1;
        [Configurable] public int FadeDuration = 200;

        public override void Generate()
        {
            var hitobjectLayer = GetLayer("");
            foreach (var hitobject in Beatmap.HitObjects)
            {
                if ((StartTime != 0 || EndTime != 0) &&
                    (hitobject.StartTime < StartTime - 5 || EndTime - 5 <= hitobject.StartTime))
                    continue;

                var stackOffset = hitobject.StackOffset;

                var hSprite = hitobjectLayer.CreateSprite(SpritePath, OsbOrigin.Centre, hitobject.Position + stackOffset);
                hSprite.Scale(OsbEasing.In, hitobject.StartTime, hitobject.EndTime + FadeDuration, SpriteScale, SpriteScale * 0.2);
                hSprite.Fade(OsbEasing.In, hitobject.StartTime, hitobject.EndTime + FadeDuration, 1, 0);
                hSprite.Additive(hitobject.StartTime, hitobject.EndTime + FadeDuration);
                hSprite.Color(hitobject.StartTime, hitobject.Color);

                if (hitobject is OsuSlider)
                {
                    var timestep = Beatmap.GetTimingPointAt((int)hitobject.StartTime).BeatDuration / BeatDivisor;
                    var startTime = hitobject.StartTime;
                    while (true)
                    {
                        var endTime = startTime + timestep;

                        var complete = hitobject.EndTime - endTime < 5;
                        if (complete) endTime = hitobject.EndTime;

                        var startPosition = hSprite.PositionAt(startTime);
                        hSprite.Move(startTime, endTime, startPosition, hitobject.PositionAtTime(endTime) + stackOffset);

                        if (complete) break;
                        startTime += timestep;
                    }
                }
            }
        }
    }
}
