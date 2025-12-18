namespace RSSAIzer.Shared;

public static class CollectionExtensions
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) =>
        source.Where(x => x is not null)!;

    public static IQueryable<T> WhereNotNull<T>(this IQueryable<T?> source) =>
        source.Where(x => x != null)!;
}
