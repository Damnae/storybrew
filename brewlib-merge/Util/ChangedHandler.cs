namespace BrewLib.Util
{
    public class ChangedEventArgs
    {
        public static readonly ChangedEventArgs All = new ChangedEventArgs(null);

        public readonly string PropertyName;
        public ChangedEventArgs(string propertyName) => PropertyName = propertyName;
    }
    public delegate void ChangedHandler(object sender, ChangedEventArgs e);
}