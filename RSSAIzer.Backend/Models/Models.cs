using RSSAIzer.Shared;

namespace RSSAIzer.Backend.Models;

public sealed record TgRssProviderModel(string Name, string BaseUrl);

public sealed record FeedModel(FeedUrl FeedUrl, string Description, string Title, Uri ImageUrl);

public sealed record PostModel(FeedModel Feed, Html HtmlContent, Uri Url, DateTime PublishedAt);

public sealed record PostSummaryModel(
    FeedModel Feed,
    string Summary,
    Uri Url,
    DateTime PublishedAt,
    Importance Importance
);

public enum PromptTypeEnumModel
{
    PostSummary,
    PostImportance,
    DigestSummary,
}

public sealed record DigestModel(
    DigestId DigestId,
    List<PostSummaryModel> PostsSummaries,
    DigestSummaryModel DigestSummary,
    Dictionary<PromptTypeEnumModel, string> UsedPrompts
);

public sealed record DigestSummaryModel(
    DigestId DigestId,
    string Title,
    string PostsSummary,
    int PostsCount,
    double AverageImportance,
    DateTime CreatedAt,
    DateTime DateFrom,
    DateTime DateTo
);

/// <param name="SelectedFeeds">Null means all feeds</param>
public sealed record DigestParametersModel(
    DateOnly DateFrom,
    DateOnly DateTo,
    IReadOnlySet<FeedUrl>? SelectedFeeds = null,
    TemplateWithContent? PostSummaryUserPromptOverride = null,
    TemplateWithContent? PostImportanceUserPromptOverride = null,
    TemplateWithContent? DigestSummaryUserPromptOverride = null
);

public enum DigestGenerationResultModelEnum
{
    Success,
    NoPosts,
}

public sealed record SettingsModel(
    string EmailRecipient,
    TimeUtc DigestTime,
    SmtpSettingsModel SmtpSettings,
    OpenAiSettingsModel OpenAiSettings,
    PromptSettingsModel PromptSettings
);

public sealed record SmtpSettingsModel(
    Host Host,
    int Port,
    string Username,
    string Password,
    bool UseSsl
);

public sealed record OpenAiSettingsModel(string ApiKey, string Model, Uri Endpoint);

public sealed record PromptSettingsModel(
    TemplateWithContent PostSummaryUserPrompt,
    TemplateWithContent PostImportanceUserPrompt,
    TemplateWithContent DigestSummaryUserPrompt
);
