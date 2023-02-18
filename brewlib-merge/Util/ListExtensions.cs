using System.Collections.Generic;

namespace BrewLib.Util
{
    public static class ListExtensions
    {
        public static void Move<T>(this List<T> list, int from, int to)
        {
            if (from == to) return;

            var item = list[from];
            if (from < to) for (var index = from; index < to; index++) list[index] = list[index + 1];
            else for (var index = from; index > to; index--) list[index] = list[index - 1];
            list[to] = item;
        }
    }
}