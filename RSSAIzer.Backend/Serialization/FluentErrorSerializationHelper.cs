using System.Diagnostics;
using System.Text.Json;
using FluentResults;

namespace RSSAIzer.Backend.Serialization;

public sealed record FluentErrorSerializationDto
{
    public string? Message { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
    public List<FluentErrorSerializationDto> Reasons { get; init; } = [];
    public string? ErrorType { get; init; }
    public string? SerializedException { get; init; }
}

public static class FluentErrorSerializationHelper
{
    private static Error ToError(FluentErrorSerializationDto dto)
    {
        Error error;

        if (dto.ErrorType == nameof(ExceptionalError) && dto.SerializedException != null)
        {
            var exception = JsonSerializer.Deserialize<Exception>(
                dto.SerializedException,
                SerializationOptions.ExceptionSerializerOptions
            );
            error = new ExceptionalError(dto.Message ?? string.Empty, exception);
        }
        else
        {
            error = new(dto.Message ?? string.Empty);
        }

        foreach (var item in dto.Metadata)
            error.WithMetadata(item.Key, item.Value);

        foreach (var reason in dto.Reasons)
            error.CausedBy(ToError(reason));

        return error;
    }

    private static FluentErrorSerializationDto ToDto(Error error)
    {
        var dto = new FluentErrorSerializationDto
        {
            Message = error.Message,
            Metadata = new(error.Metadata),
            Reasons = error.Reasons.OfType<Error>().Select(ToDto).ToList(),
            ErrorType = error.GetType().Name,
            SerializedException = error is ExceptionalError { Exception: not null } exceptionalError
                ? JsonSerializer.Serialize(
                    exceptionalError.Exception,
                    SerializationOptions.ExceptionSerializerOptions
                )
                : null,
        };

        return dto;
    }

    public static string SerializeErrors(List<IError> errors)
    {
        if (errors.Any(e => e is not Error))
        {
            throw new UnreachableException("Only native errors are supported");
        }

        return JsonSerializer.Serialize(errors.Select(x => ToDto((Error)x)));
    }

    public static List<IError> DeserializeErrors(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var errorDtos = JsonSerializer.Deserialize<List<FluentErrorSerializationDto>>(json);
        if (errorDtos is null)
        {
            throw new JsonException("ErrorDto deserialization returned null");
        }

        return errorDtos.Select<FluentErrorSerializationDto, IError>(ToError).ToList();
    }
}
