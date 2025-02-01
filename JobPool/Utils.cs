namespace JobPool
{
    public static class Utils
    {
        public static void ForEach<T>(this IEnumerable<T> values, Action<T> action)
        {
            var enu = values.GetEnumerator();
            while (enu.MoveNext())
            {
                action(enu.Current);
            }
        }
    }
}
