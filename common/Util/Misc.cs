namespace StorybrewCommon.Util
{
    public static class Misc
    {
        public static void WithRetries(Action action, int timeout = 2000, bool canThrow = true)
            => BrewLib.Util.Misc.WithRetries(action, timeout, canThrow);

        public static T WithRetries<T>(Func<T> action, int timeout = 2000, bool canThrow = true)
            => BrewLib.Util.Misc.WithRetries<T>(action, timeout, canThrow);
    }
}
