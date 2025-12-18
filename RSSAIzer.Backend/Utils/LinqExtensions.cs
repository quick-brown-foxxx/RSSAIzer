namespace RSSAIzer.Backend.Utils;

public static class LinqExtensions
{
    public static T? SingleOrDefaultIfNotExactlyOne<T>(this IEnumerable<T> source)
    {
        using var e = source.GetEnumerator();
        if (!e.MoveNext())
        {
            return default;
        }

        var single = e.Current;
        return e.MoveNext() ? default : single;
    }
}
