using System.Text.Json;

namespace RSSAIzer.Backend.Serialization;

internal static class SerializationOptions
{
    public static readonly JsonSerializerOptions ExceptionSerializerOptions = new()
    {
        Converters = { new ExceptionJsonConverter() },
    };

    public static readonly JsonSerializerOptions FeedsSerializerOptions = new()
    {
        Converters = { new FeedUrlJsonConverter() },
    };

    public static readonly JsonSerializerOptions HumanReadableSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };
}
