namespace BrewLib.Time
{
    public interface ReadOnlyTimeSource
    {
        double Current { get; }

        double TimeFactor { get; }
        bool Playing { get; }
    }
    public interface TimeSource : ReadOnlyTimeSource
    {
        new double TimeFactor { get; set; }
        new bool Playing { get; set; }

        bool Seek(double time);
    }
}