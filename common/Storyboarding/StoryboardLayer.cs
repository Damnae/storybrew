namespace StorybrewCommon.Storyboarding
{
    public abstract class StoryboardLayer : StoryboardSegment
    {
        public string Identifier { get; }

        public StoryboardLayer(string identifier)
        {
            Identifier = identifier;
        }
    }
}
