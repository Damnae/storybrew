using OpenTK;

namespace StorybrewCommon.Curves
{
    ///<summary> Represents types of curves. </summary>
    public interface Curve
    {
        ///<summary> The start position (the head) of the curve. </summary>
        Vector2 StartPosition { get; }

        ///<summary> The end position (the tail) of the curve. </summary>
        Vector2 EndPosition { get; }

        ///<summary> The total length of the curve from the head to the tail. </summary>
        double Length { get; }

        ///<summary> Returns the position of the curve at <paramref name="distance"/>. </summary>
        Vector2 PositionAtDistance(double distance);

        ///<summary> Returns the position of the curve at <paramref name="delta"/>. </summary>
        Vector2 PositionAtDelta(double delta);
    }
}