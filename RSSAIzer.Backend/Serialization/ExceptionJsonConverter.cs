using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RSSAIzer.Backend.Serialization;

internal sealed class ExceptionJsonConverter : JsonConverter<Exception>
{
    public override Exception Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var doc = JsonDocument.ParseValue(ref reader);
        using (doc)
        {
            var root = doc.RootElement;
            var typeName = root.GetProperty("$type").GetString()!;
            var exceptionType = Type.GetType(typeName) ?? typeof(Exception);

            var exception = CreateException(exceptionType, root, options);
            PopulateException(exception, root, options);
            return exception;
        }
    }

    private static Exception CreateException(
        Type exceptionType,
        JsonElement root,
        JsonSerializerOptions options
    )
    {
        try
        {
            // Try to use the most common constructor
            var message = root.GetProperty("Message").GetString();
            var inner = DeserializeInnerException(root.GetProperty("InnerException"), options);
            return (Exception)Activator.CreateInstance(exceptionType, message, inner)!;
        }
        catch
        {
            // Fallback for parameterless constructors
            try
            {
                var ex = (Exception)Activator.CreateInstance(exceptionType)!;
                SetPrivateField(ex, "_message", root.GetProperty("Message").GetString());
                return ex;
            }
            catch
            {
                // Ultimate fallback
                return new(root.GetProperty("Message").GetString());
            }
        }
    }

    private static void PopulateException(
        Exception exception,
        JsonElement root,
        JsonSerializerOptions options
    )
    {
        SetPrivateField(exception, "_stackTraceString", root.GetProperty("StackTrace").GetString());
        SetPrivateField(exception, "_source", root.GetProperty("Source").GetString());
        exception.HResult = root.GetProperty("HResult").GetInt32();

        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(
            root.GetProperty("Data").GetRawText(),
            options
        );

        foreach (var item in data ?? new())
            exception.Data[item.Key] = item.Value;
    }

    private static Exception? DeserializeInnerException(
        JsonElement element,
        JsonSerializerOptions options
    ) =>
        element.ValueKind == JsonValueKind.Null
            ? null
            : JsonSerializer.Deserialize<Exception>(element.GetRawText(), options);

    private static void SetPrivateField(Exception ex, string fieldName, object? value) =>
        typeof(Exception)
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(ex, value);

    public override void Write(
        Utf8JsonWriter writer,
        Exception value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();

        writer.WriteString("$type", value.GetType().AssemblyQualifiedName);
        writer.WriteString("Message", value.Message);
        writer.WriteString("StackTrace", value.StackTrace);
        writer.WriteString("Source", value.Source);
        writer.WriteNumber("HResult", value.HResult);

        writer.WritePropertyName("Data");
        JsonSerializer.Serialize(
            writer,
            value
                .Data.Cast<DictionaryEntry>()
                .ToDictionary(de => de.Key.ToString()!, de => de.Value),
            options
        );

        writer.WritePropertyName("InnerException");
        if (value.InnerException != null)
        {
            JsonSerializer.Serialize(writer, value.InnerException, options);
        }
        else
        {
            writer.WriteNullValue();
        }

        writer.WriteEndObject();
    }
}
