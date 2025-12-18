using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.Options;
using RSSAIzer.Backend.Db;
using RSSAIzer.Backend.Models;
using RSSAIzer.Backend.Options;
using RSSAIzer.Backend.Serialization;

namespace RSSAIzer.Backend.Features;

internal interface ISettingsService
{
    /// <summary>
    /// Loads application settings.
    /// </summary>
    public Task<Result<SettingsModel>> LoadSettings(CancellationToken ct);

    /// <summary>
    /// Saves application settings.
    /// </summary>
    public Task<Result> SaveSettings(SettingsModel settings, CancellationToken ct);
}

/// <summary>
/// Manages application settings with database persistence.
/// </summary>
internal sealed class SettingsService(
    ISettingsRepository repository,
    ILogger<SettingsService> logger,
    IOptions<SettingsOptions> options
) : ISettingsService
{
    public async Task<Result<SettingsModel>> LoadSettings(CancellationToken ct)
    {
        try
        {
            var result = await repository.LoadSettings(ct);
            if (!result.IsSuccess)
            {
                logger.LogError("Failed to load settings from database: {Error}", result.Errors);
                return Result.Fail(result.Errors);
            }
            if (result is { IsSuccess: true, Value: not null })
            {
                return Result.Ok(result.Value);
            }

            logger.LogInformation(
                "No settings found in database. Creating and saving default settings."
            );
            var defaultSettingsResult = LoadDefaultSettings();
            if (!defaultSettingsResult.IsSuccess)
            {
                logger.LogError(
                    "Failed to load default settings: {Error}",
                    defaultSettingsResult.Errors
                );
                return Result.Fail(defaultSettingsResult.Errors);
            }

            var saveResult = await repository.SaveSettings(defaultSettingsResult.Value, ct);
            return !saveResult.IsSuccess
                ? Result.Fail(saveResult.Errors)
                : Result.Ok(defaultSettingsResult.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while loading settings from database");
            return Result.Fail(
                new Error("Exception occurred while loading settings from database").CausedBy(ex)
            );
        }
    }

    public async Task<Result> SaveSettings(SettingsModel settings, CancellationToken ct)
    {
        return await repository.SaveSettings(settings, ct);
    }

    private Result<SettingsModel> LoadDefaultSettings()
    {
        var settingsJsonResult = Result.Try(() =>
            JsonSerializer.Deserialize<SettingsJson>(
                options.Value.DefaultSettings,
                SerializationOptions.HumanReadableSerializerOptions
            )
        );
        if (!settingsJsonResult.IsSuccess || settingsJsonResult.Value == null)
        {
            logger.LogError(
                "Failed to deserialize default settings from JSON: {Error}",
                settingsJsonResult.Errors
            );
            return Result.Fail(
                new Error("Failed to deserialize default settings from JSON").CausedBy(
                    settingsJsonResult.Errors
                )
            );
        }

        var mappingResult = Result.Try(() => MapSettingsJsonToModel(settingsJsonResult.Value));
        if (!mappingResult.IsSuccess)
        {
            logger.LogError(
                "Failed to load default settings from JSON, data is invalid: {Error}",
                mappingResult.Errors
            );
            return Result.Fail(
                new Error("Failed to load default settings from JSON, data is invalid").CausedBy(
                    mappingResult.Errors
                )
            );
        }

        return Result.Ok(mappingResult.Value);
    }

    private static SettingsModel MapSettingsJsonToModel(SettingsJson defaultSettings)
    {
        return new(
            defaultSettings.EmailRecipient,
            new(defaultSettings.DigestTime),
            new(
                new(defaultSettings.SmtpSettings.Host),
                defaultSettings.SmtpSettings.Port,
                defaultSettings.SmtpSettings.Username,
                defaultSettings.SmtpSettings.Password,
                defaultSettings.SmtpSettings.UseSsl
            ),
            new(
                defaultSettings.OpenAiSettings.ApiKey,
                defaultSettings.OpenAiSettings.Model,
                defaultSettings.OpenAiSettings.Endpoint
            ),
            new(
                new(defaultSettings.PromptSettings.PostSummaryUserPrompt),
                new(defaultSettings.PromptSettings.PostImportanceUserPrompt),
                new(defaultSettings.PromptSettings.DigestSummaryUserPrompt)
            )
        );
    }
}
