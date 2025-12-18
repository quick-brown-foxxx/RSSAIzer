using FluentResults;
using RSSAIzer.Backend.Infrastructure;
using RSSAIzer.Backend.Models;

namespace RSSAIzer.Backend.Db;

internal interface ISettingsRepository
{
    Task<Result<SettingsModel?>> LoadSettings(CancellationToken ct);
    Task<Result> SaveSettings(SettingsModel settings, CancellationToken ct);
}

internal sealed class SettingsRepository(
    ApplicationDbContext dbContext,
    ILogger<SettingsRepository> logger,
    ICurrentUserContext currentUserContext
) : ISettingsRepository
{
    public async Task<Result<SettingsModel?>> LoadSettings(CancellationToken ct)
    {
        try
        {
            var entity = await dbContext
                .Settings.Where(s => s.UserId == currentUserContext.UserId)
                .AsNoTracking()
                .SingleOrDefaultAsync(ct);
            return entity == null
                ? Result.Ok<SettingsModel?>(null)
                : Result.Ok<SettingsModel?>(MapToModel(entity));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load settings from database");
            return Result.Fail(new Error("Failed to load settings from database").CausedBy(ex));
        }
    }

    public async Task<Result> SaveSettings(SettingsModel settings, CancellationToken ct)
    {
        try
        {
            var userId = currentUserContext.UserId;

            // Ensure the user entity is tracked in the context so EF Core can validate the foreign key relationship
            var user = await dbContext.Users.FindAsync([userId], ct);
            if (user == null)
            {
                logger.LogError(
                    "User {UserId} does not exist in database. Cannot save settings.",
                    userId
                );
                return Result.Fail(
                    new Error($"User {userId} does not exist in database. Cannot save settings.")
                );
            }

            var existing = await dbContext
                .Settings.Where(s => s.UserId == userId)
                .SingleOrDefaultAsync(ct);
            if (existing != null)
            {
                var entity = MapToEntity(settings, userId);
                dbContext.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                // Set the navigation property so EF Core can validate the foreign key relationship
                var entity = MapToEntity(settings, userId, user);
                await dbContext.Settings.AddAsync(entity, ct);
            }
            await dbContext.SaveChangesAsync(ct);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save settings to database");
            return Result.Fail(new Error("Failed to save settings to database").CausedBy(ex));
        }
    }

    private static SettingsModel MapToModel(SettingsEntity entity)
    {
        return new(
            entity.EmailRecipient,
            new(TimeOnly.Parse(entity.DigestTimeUtc)),
            new(
                new(entity.SmtpSettingsHost),
                entity.SmtpSettingsPort,
                entity.SmtpSettingsUsername,
                entity.SmtpSettingsPassword,
                entity.SmtpSettingsUseSsl
            ),
            new(
                entity.OpenAiSettingsApiKey,
                entity.OpenAiSettingsModel,
                new(entity.OpenAiSettingsEndpoint)
            ),
            new(
                new(entity.PromptSettingsPostSummaryUserPrompt),
                new(entity.PromptSettingsPostImportanceUserPrompt),
                new(entity.PromptSettingsDigestSummaryUserPrompt)
            )
        );
    }

    private static SettingsEntity MapToEntity(
        SettingsModel model,
        Guid userId,
        ApplicationUser? userNav = null
    )
    {
        return new()
        {
            UserId = userId,
            UserNav = userNav,
            EmailRecipient = model.EmailRecipient,
            DigestTimeUtc = model.DigestTime.ToString(),
            SmtpSettingsHost = model.SmtpSettings.Host.ToString(),
            SmtpSettingsPort = model.SmtpSettings.Port,
            SmtpSettingsUsername = model.SmtpSettings.Username,
            SmtpSettingsPassword = model.SmtpSettings.Password,
            SmtpSettingsUseSsl = model.SmtpSettings.UseSsl,
            OpenAiSettingsApiKey = model.OpenAiSettings.ApiKey,
            OpenAiSettingsModel = model.OpenAiSettings.Model,
            OpenAiSettingsEndpoint = model.OpenAiSettings.Endpoint.ToString(),
            PromptSettingsPostSummaryUserPrompt =
                model.PromptSettings.PostSummaryUserPrompt.ToString(),
            PromptSettingsPostImportanceUserPrompt =
                model.PromptSettings.PostImportanceUserPrompt.ToString(),
            PromptSettingsDigestSummaryUserPrompt =
                model.PromptSettings.DigestSummaryUserPrompt.ToString(),
        };
    }
}
