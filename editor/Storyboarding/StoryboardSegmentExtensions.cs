using OpenTK;
using StorybrewCommon.Storyboarding;

namespace StorybrewEditor.Storyboarding
{
    public static class StoryboardSegmentExtensions
    {
        public static EditorStoryboardSegment AsEditorSegment(this StoryboardSegment segment)
            => segment as EditorStoryboardSegment ?? (segment as EditorStoryboardLayer)?.InternalSegment;

        public static StoryboardTransform BuildCompleteTransform(this StoryboardSegment segment)
            => segment.BuildTransform(segment.BuildCompleteParentTransform());

        public static StoryboardTransform BuildCompleteParentTransform(this StoryboardSegment segment)
        {
            var editorSegment = segment.AsEditorSegment();
            return editorSegment.Parent != null ? BuildCompleteTransform(editorSegment.Parent) : StoryboardTransform.Identity;
        }

        public static StoryboardTransform BuildTransform(this StoryboardSegment segment, StoryboardTransform parentTransform)
        {
            var editorSegment = segment.AsEditorSegment();
            return new StoryboardTransform(parentTransform,
                segment.Origin, segment.Position, segment.Rotation, (float)segment.Scale,
                editorSegment.PlacementPosition, editorSegment.PlacementRotation, (float)editorSegment.PlacementScale);
        }
        public static StoryboardTransform BuildTransformWithoutPlacement(this StoryboardSegment segment, StoryboardTransform parentTransform)
            => new StoryboardTransform(parentTransform, segment.Origin, segment.Position, segment.Rotation, (float)segment.Scale, Vector2.Zero, 0, 1);
    }
}
