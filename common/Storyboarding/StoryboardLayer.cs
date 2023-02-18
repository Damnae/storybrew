namespace StorybrewCommon.Storyboarding
{
    ///<summary> Layers for an <see cref="StoryboardObject"/>. </summary>
    public abstract class StoryboardLayer : StoryboardSegment
    {
        ///<summary> The name of the layer. </summary>
        public string Name { get; }

        ///<summary> Constructs a new layer with the given name. </summary>
        ///<param name="name"> The name of the layer. </param>
        public StoryboardLayer(string name) => Name = name;
    }
}