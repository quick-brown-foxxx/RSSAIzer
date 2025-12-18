using System.Diagnostics;
using System.Text.Json;
using FluentResults;
using RSSAIzer.Backend.Features.DigestSteps;
using RSSAIzer.Backend.Models;
using RSSAIzer.Backend.Serialization;

namespace RSSAIzer.Backend.Db;

internal interface IDigestStepsRepository
{
    Task<Result<IDigestStepModel[]>> LoadStepsHistory(DigestId digestId, CancellationToken ct);
    Task<Result> SaveStepAsync(IDigestStepModel step, CancellationToken ct);
}

internal sealed class DigestStepsRepository(
    ApplicationDbContext context,
    ILogger<DigestStepsRepository> logger
) : IDigestStepsRepository
{
    public async Task<Result<IDigestStepModel[]>> LoadStepsHistory(
        DigestId digestId,
        CancellationToken ct
    )
    {
        try
        {
            var entities = await context
                .DigestSteps.Where(s => s.DigestId == digestId.Guid)
                .OrderBy(s => s.Timestamp)
                .ToListAsync(ct);

            var models = entities.Select(MapEntityToModel).ToArray();
            return Result.Ok(models);
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Failed to retrieve digest step history").CausedBy(ex));
        }
    }

    public async Task<Result> SaveStepAsync(IDigestStepModel step, CancellationToken ct)
    {
        try
        {
            var entity = MapModelToEntity(step);
            context.DigestSteps.Add(entity);
            await context.SaveChangesAsync(ct);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save digest step");
            return Result.Fail(new Error("Failed to save digest step").CausedBy(ex));
        }
    }

    private static IDigestStepModel MapEntityToModel(DigestStepEntity entity)
    {
        var digestId = new DigestId(entity.DigestId);
        var type = MapEntityEnumToModel(entity.Type);

        return entity switch
        {
            SimpleStepEntity => new SimpleStepModel
            {
                DigestId = digestId,
                Type = type,
                Message = entity.Message,
                Timestamp = entity.Timestamp,
            },
            AiProcessingStepEntity e => new AiProcessingStepModel
            {
                DigestId = digestId,
                Percentage = e.Percentage,
                Message = entity.Message,
                Timestamp = entity.Timestamp,
            },
            RssReadingStartedStepEntity e => new RssReadingStartedStepModel
            {
                DigestId = digestId,
                Feeds =
                    e.FeedsJson != null
                        ? JsonSerializer.Deserialize<FeedUrl[]>(
                            e.FeedsJson,
                            SerializationOptions.FeedsSerializerOptions
                        ) ?? []
                        : [],
                Message = entity.Message,
                Timestamp = entity.Timestamp,
            },
            RssReadingFinishedStepEntity e => new RssReadingFinishedStepModel
            {
                DigestId = digestId,
                PostsCount = e.PostsFound,
                Message = entity.Message,
                Timestamp = entity.Timestamp,
            },
            ErrorStepEntity e => new ErrorStepModel
            {
                DigestId = digestId,
                Exception =
                    e.ExceptionJsonSerialized != null
                        ? JsonSerializer.Deserialize<Exception>(
                            e.ExceptionJsonSerialized,
                            SerializationOptions.ExceptionSerializerOptions
                        )
                        : null,
                Errors =
                    e.ErrorsJsonSerialized != null
                        ? FluentErrorSerializationHelper.DeserializeErrors(e.ErrorsJsonSerialized)
                        : null,
                Message = entity.Message,
                Timestamp = entity.Timestamp,
            },
            _ => throw new UnreachableException("Unknown digest step entity"),
        };
    }

    private static DigestStepEntity MapModelToEntity(IDigestStepModel model)
    {
        var id = Guid.NewGuid();
        var type = MapModeEnumToEntity(model.Type);

        return model switch
        {
            AiProcessingStepModel m => new AiProcessingStepEntity
            {
                Id = id,
                DigestId = model.DigestId.Guid,
                Type = type,
                Message = model.Message,
                Percentage = m.Percentage,
                Timestamp = model.Timestamp,
            },
            RssReadingStartedStepModel m => new RssReadingStartedStepEntity
            {
                Id = id,
                DigestId = model.DigestId.Guid,
                Type = type,
                Message = model.Message,
                FeedsJson = JsonSerializer.Serialize(
                    m.Feeds,
                    SerializationOptions.FeedsSerializerOptions
                ),
                Timestamp = model.Timestamp,
            },
            RssReadingFinishedStepModel m => new RssReadingFinishedStepEntity
            {
                Id = id,
                DigestId = model.DigestId.Guid,
                Type = type,
                Message = model.Message,
                PostsFound = m.PostsCount,
                Timestamp = model.Timestamp,
            },
            ErrorStepModel m => new ErrorStepEntity
            {
                Id = id,
                DigestId = model.DigestId.Guid,
                Type = type,
                Message = model.Message,
                ExceptionJsonSerialized =
                    m.Exception != null
                        ? JsonSerializer.Serialize(
                            m.Exception,
                            SerializationOptions.ExceptionSerializerOptions
                        )
                        : null,
                ErrorsJsonSerialized =
                    m.Errors != null
                        ? FluentErrorSerializationHelper.SerializeErrors(m.Errors)
                        : null,
                Timestamp = model.Timestamp,
            },
            SimpleStepModel => new SimpleStepEntity
            {
                Id = id,
                DigestId = model.DigestId.Guid,
                Type = type,
                Message = model.Message,
                Timestamp = model.Timestamp,
            },
            _ => throw new UnreachableException("Unknown digest step model"),
        };
    }

    private static DigestStepTypeEntityEnum MapModeEnumToEntity(DigestStepTypeModelEnum enumType) =>
        enumType switch
        {
            DigestStepTypeModelEnum.Queued => DigestStepTypeEntityEnum.Queued,
            DigestStepTypeModelEnum.ProcessingStarted => DigestStepTypeEntityEnum.ProcessingStarted,
            DigestStepTypeModelEnum.RssReadingStarted => DigestStepTypeEntityEnum.RssReadingStarted,
            DigestStepTypeModelEnum.RssReadingFinished =>
                DigestStepTypeEntityEnum.RssReadingFinished,
            DigestStepTypeModelEnum.AiProcessing => DigestStepTypeEntityEnum.AiProcessing,
            DigestStepTypeModelEnum.Success => DigestStepTypeEntityEnum.Success,
            DigestStepTypeModelEnum.Cancelled => DigestStepTypeEntityEnum.Cancelled,
            DigestStepTypeModelEnum.Error => DigestStepTypeEntityEnum.Error,
            DigestStepTypeModelEnum.NoPostsFound => DigestStepTypeEntityEnum.NoPostsFound,
            _ => throw new UnreachableException("Unknown digest step model enum"),
        };

    private static DigestStepTypeModelEnum MapEntityEnumToModel(
        DigestStepTypeEntityEnum enumType
    ) =>
        enumType switch
        {
            DigestStepTypeEntityEnum.Queued => DigestStepTypeModelEnum.Queued,
            DigestStepTypeEntityEnum.ProcessingStarted => DigestStepTypeModelEnum.ProcessingStarted,
            DigestStepTypeEntityEnum.RssReadingStarted => DigestStepTypeModelEnum.RssReadingStarted,
            DigestStepTypeEntityEnum.RssReadingFinished =>
                DigestStepTypeModelEnum.RssReadingFinished,
            DigestStepTypeEntityEnum.AiProcessing => DigestStepTypeModelEnum.AiProcessing,
            DigestStepTypeEntityEnum.Success => DigestStepTypeModelEnum.Success,
            DigestStepTypeEntityEnum.Cancelled => DigestStepTypeModelEnum.Cancelled,
            DigestStepTypeEntityEnum.Error => DigestStepTypeModelEnum.Error,
            DigestStepTypeEntityEnum.NoPostsFound => DigestStepTypeModelEnum.NoPostsFound,
            _ => throw new UnreachableException("Unknown digest step entity enum"),
        };
}
