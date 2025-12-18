using FluentResults;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using RSSAIzer.Backend.Models;
using RSSAIzer.Backend.Options;

namespace RSSAIzer.Backend.Features.DigestFromRssGeneration;

internal interface IAiSummarizer
{
    Task<Result<PostSummaryModel>> GenerateSummary(
        PostModel post,
        Prompts prompts,
        CancellationToken ct
    );

    Task<Result<DigestSummaryModel>> GeneratePostsSummary(
        List<PostModel> posts,
        Prompts prompts,
        CancellationToken ct
    );
}

internal record Prompts(
    TemplateWithContent PostSummaryUserPrompt,
    TemplateWithContent PostImportanceUserPrompt,
    TemplateWithContent DigestSummaryUserPrompt
);

internal sealed class AiSummarizer(
    ISettingsService settingsService,
    IOptions<AiOptions> aiOptions,
    ILogger<AiSummarizer> logger
) : IAiSummarizer
{
    private readonly AiOptions _aiOptions = aiOptions.Value;
    private ChatClient? _chatClient;
    private (string Model, string ApiKey, Uri Endpoint)? _lastClientSettings;

    private async Task<Result<ChatClient>> GetChatClient(CancellationToken ct)
    {
        var settingsResult = await settingsService.LoadSettings(ct);
        if (settingsResult.IsFailed)
        {
            return Result.Fail(settingsResult.Errors);
        }

        if (
            _chatClient != null
            && _lastClientSettings != null
            && settingsResult.Value.OpenAiSettings.Model == _lastClientSettings.Value.Model
            && settingsResult.Value.OpenAiSettings.ApiKey == _lastClientSettings.Value.ApiKey
            && settingsResult.Value.OpenAiSettings.Endpoint == _lastClientSettings.Value.Endpoint
        )
        {
            return Result.Ok(_chatClient);
        }

        _chatClient = new(
            model: settingsResult.Value.OpenAiSettings.Model,
            credential: new(settingsResult.Value.OpenAiSettings.ApiKey),
            options: new() { Endpoint = settingsResult.Value.OpenAiSettings.Endpoint }
        );

        _lastClientSettings = (
            settingsResult.Value.OpenAiSettings.Model,
            settingsResult.Value.OpenAiSettings.ApiKey,
            settingsResult.Value.OpenAiSettings.Endpoint
        );

        return Result.Ok(_chatClient);
    }

    public async Task<Result<PostSummaryModel>> GenerateSummary(
        PostModel post,
        Prompts prompts,
        CancellationToken ct
    )
    {
        try
        {
            var clientResult = await GetChatClient(ct);
            if (clientResult.IsFailed)
            {
                return Result.Fail(clientResult.Errors);
            }

            var messages = (ChatMessage[])
                [
                    new SystemChatMessage(_aiOptions.PostSummarySystemPrompt),
                    new UserChatMessage(
                        prompts.PostSummaryUserPrompt.ReplacePlaceholder(
                            post.HtmlContent.HtmlString
                        )
                    ),
                ];

            var completionResult = await clientResult.Value.CompleteChatAsync(
                messages,
                cancellationToken: ct
            );

            var importance = await EvaluatePostImportance(post, prompts, ct);
            if (importance.IsFailed)
            {
                return Result.Fail(importance.Errors);
            }

            return Result.Ok(
                new PostSummaryModel(
                    Feed: post.Feed,
                    Summary: completionResult.Value.Content[0].Text.Trim(),
                    Url: post.Url,
                    PublishedAt: post.PublishedAt,
                    Importance: importance.Value
                )
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate summary for post {PostUrl}", post.Url);
            return Result.Fail(
                new Error($"Failed to generate summary for post {post.Url}").CausedBy(ex)
            );
        }
    }

    private async Task<Result<Importance>> EvaluatePostImportance(
        PostModel post,
        Prompts prompts,
        CancellationToken ct
    )
    {
        try
        {
            var clientResult = await GetChatClient(ct);
            if (clientResult.IsFailed)
            {
                return Result.Fail(clientResult.Errors);
            }

            var messages = (ChatMessage[])
                [
                    new SystemChatMessage(_aiOptions.PostImportanceSystemPrompt),
                    new UserChatMessage(
                        prompts.PostImportanceUserPrompt.ReplacePlaceholder(
                            post.HtmlContent.HtmlString
                        )
                    ),
                ];

            var completion = await clientResult.Value.CompleteChatAsync(
                messages,
                cancellationToken: ct
            );
            var importanceValue = int.Parse(completion.Value.Content[0].Text.Trim());

            return Result.Ok(new Importance(importanceValue));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to evaluate importance for post {Url}", post.Url);
            return Result.Fail(
                new Error($"Failed to evaluate importance for post {post.Url}").CausedBy(ex)
            );
        }
    }

    public async Task<Result<DigestSummaryModel>> GeneratePostsSummary(
        List<PostModel> posts,
        Prompts prompts,
        CancellationToken ct
    )
    {
        try
        {
            var clientResult = await GetChatClient(ct);
            if (clientResult.IsFailed)
            {
                return Result.Fail(clientResult.Errors);
            }

            var postsContent = string.Join("\n\n", posts.Select(p => p.HtmlContent));

            var messages = (ChatMessage[])
                [
                    new SystemChatMessage(_aiOptions.DigestSummarySystemPrompt),
                    new UserChatMessage(
                        prompts.DigestSummaryUserPrompt.ReplacePlaceholder(postsContent)
                    ),
                ];

            var completion = await clientResult.Value.CompleteChatAsync(
                messages,
                cancellationToken: ct
            );

            return Result.Ok(
                new DigestSummaryModel(
                    DigestId: DigestId.NewId(),
                    Title: "Daily Digest",
                    PostsSummary: completion.Value.Content[0].Text.Trim(),
                    PostsCount: posts.Count,
                    AverageImportance: posts.Count > 0
                        ? await CalculateAverageImportance(posts, prompts, ct)
                        : 0,
                    CreatedAt: DateTime.UtcNow,
                    DateFrom: posts.Min(p => p.PublishedAt),
                    DateTo: posts.Max(p => p.PublishedAt)
                )
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate posts summary");
            return Result.Fail(new Error("Summary generation failed").CausedBy(ex));
        }
    }

    private async Task<double> CalculateAverageImportance(
        List<PostModel> posts,
        Prompts prompts,
        CancellationToken ct
    )
    {
        var importanceResults = await Task.WhenAll(
            posts.Select(p => EvaluatePostImportance(p, prompts, ct))
        );

        var successfulResults = importanceResults
            .Where(r => r.IsSuccess)
            .Select(r => r.Value.Number)
            .ToArray();

        return successfulResults.Length != 0 ? successfulResults.Average() : 0;
    }
}
