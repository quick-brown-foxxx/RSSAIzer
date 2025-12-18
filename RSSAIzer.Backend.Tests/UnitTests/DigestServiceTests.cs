using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using RSSAIzer.Backend.Db;
using RSSAIzer.Backend.Features;
using RSSAIzer.Backend.Features.DigestFromRssGeneration;
using RSSAIzer.Backend.Features.DigestSteps;
using RSSAIzer.Backend.Models;

namespace RSSAIzer.Backend.Tests.UnitTests;

[TestFixture]
public sealed class DigestServiceTests
{
    private Mock<IDigestRepository> _digestRepositoryMock;
    private Mock<IFeedReader> _feedReaderMock;
    private Mock<IFeedsRepository> _feedsRepositoryMock;
    private Mock<IAiSummarizer> _aiSummarizerMock;
    private Mock<IDigestStepsService> _digestStepsServiceMock;
    private Mock<ILogger<DigestService>> _loggerMock;
    private Mock<ISettingsService> _settingsManagerMock;

    private DigestService _digestService;

    private readonly DateOnly _dateFrom = new(2023, 1, 1);
    private readonly DateOnly _dateTo = new(2023, 1, 2);

    [SetUp]
    public void SetUp()
    {
        _digestRepositoryMock = new();
        _feedReaderMock = new();
        _feedsRepositoryMock = new();
        _aiSummarizerMock = new();
        _digestStepsServiceMock = new();
        _loggerMock = new();
        _settingsManagerMock = new();

        _digestService = new(
            _digestRepositoryMock.Object,
            _feedReaderMock.Object,
            _feedsRepositoryMock.Object,
            _aiSummarizerMock.Object,
            _digestStepsServiceMock.Object,
            _loggerMock.Object,
            _settingsManagerMock.Object
        );
    }

    // High Importance Tests

    [Test]
    public async Task GenerateDigest_WithValidInputs_SavesDigest()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var parameters = new DigestParametersModel(_dateFrom, _dateTo);
        var feedUrl = new FeedUrl("https://example.com/feed");
        var feedModel = new FeedModel(
            feedUrl,
            "desc",
            "title",
            new("https://example.com/image.png")
        );
        var feeds = new List<FeedModel> { feedModel };
        var readPosts = new List<ReadPostModel>
        {
            new(new("content"), new("https://example.com/post1"), DateTime.UtcNow),
        };
        var postSummary = new PostSummaryModel(
            feedModel,
            "summary",
            new("https://example.com/post1"),
            DateTime.UtcNow,
            new(5)
        );
        var digestSummary = new DigestSummaryModel(
            digestId,
            "title",
            "summary",
            1,
            5,
            DateTime.UtcNow,
            _dateFrom.ToDateTime(TimeOnly.MinValue),
            _dateTo.ToDateTime(TimeOnly.MinValue)
        );
        DigestModel? savedDigest = null;

        _settingsManagerMock
            .Setup(x => x.LoadSettings(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(GetMockSettings()));
        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    feedModel.FeedUrl,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(readPosts));
        _aiSummarizerMock
            .Setup(x =>
                x.GenerateSummary(
                    It.IsAny<PostModel>(),
                    It.IsAny<Prompts>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(postSummary));
        _aiSummarizerMock
            .Setup(x =>
                x.GeneratePostsSummary(
                    It.IsAny<List<PostModel>>(),
                    It.IsAny<Prompts>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(digestSummary));
        _digestRepositoryMock
            .Setup(x =>
                x.SaveDigestForCurrentUser(It.IsAny<DigestModel>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Result.Ok())
            .Callback<DigestModel, CancellationToken>((digest, _) => savedDigest = digest);

        // Act
        var result = await _digestService.GenerateDigest(
            digestId,
            parameters,
            CancellationToken.None
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(DigestGenerationResultModelEnum.Success);
        _digestStepsServiceMock.Verify(
            x => x.AddStep(It.Is<SimpleStepModel>(s => s.Type == DigestStepTypeModelEnum.Success)),
            Times.Once
        );
        _digestRepositoryMock.Verify(
            x =>
                x.SaveDigestForCurrentUser(
                    It.Is<DigestModel>(d => d.DigestId == digestId),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        savedDigest.Should().NotBeNull();
        savedDigest.DigestId.Should().Be(digestId);
        savedDigest.PostsSummaries.Should().ContainSingle().And.Contain(postSummary);
        savedDigest.DigestSummary.Should().BeEquivalentTo(digestSummary);
        savedDigest.UsedPrompts.Should().NotBeNull();
        savedDigest.UsedPrompts.Should().ContainKey(PromptTypeEnumModel.PostSummary);
        savedDigest.UsedPrompts.Should().ContainKey(PromptTypeEnumModel.PostImportance);
        savedDigest.UsedPrompts.Should().ContainKey(PromptTypeEnumModel.DigestSummary);
    }

    [Test]
    public async Task GenerateDigest_WithNoPostsFound_ReturnsNoPosts()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var parameters = new DigestParametersModel(_dateFrom, _dateTo);
        var feedUrl = new FeedUrl("https://example.com/feed");
        var feedModel = new FeedModel(
            feedUrl,
            "desc",
            "title",
            new("https://example.com/image.png")
        );
        var feeds = new List<FeedModel> { feedModel };

        _settingsManagerMock
            .Setup(x => x.LoadSettings(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(GetMockSettings()));
        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    feedModel.FeedUrl,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(new List<ReadPostModel>()));

        // Act
        var result = await _digestService.GenerateDigest(
            digestId,
            parameters,
            CancellationToken.None
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(DigestGenerationResultModelEnum.NoPosts);
        _digestStepsServiceMock.Verify(
            x =>
                x.AddStep(
                    It.Is<SimpleStepModel>(s => s.Type == DigestStepTypeModelEnum.NoPostsFound)
                ),
            Times.Once
        );
        _digestRepositoryMock.Verify(
            x =>
                x.SaveDigestForCurrentUser(
                    It.Is<DigestModel>(d => d.DigestId == digestId),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Test]
    public async Task GenerateDigest_WithSomeFeedsFailing_StillProcessesSuccessfully()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var parameters = new DigestParametersModel(_dateFrom, _dateTo);
        var successFeedUrl = new FeedUrl("https://success.com/feed");
        var failFeedUrl = new FeedUrl("https://fail.com/feed");
        var successFeedModel = new FeedModel(
            successFeedUrl,
            "desc",
            "title",
            new("https://example.com/image.png")
        );
        var failFeedModel = new FeedModel(
            failFeedUrl,
            "desc",
            "title",
            new("https://example.com/image.png")
        );
        var feeds = new List<FeedModel> { successFeedModel, failFeedModel };
        var readPosts = new List<ReadPostModel>
        {
            new(new("content"), new("https://success.com/post1"), DateTime.UtcNow),
        };
        var postSummary = new PostSummaryModel(
            successFeedModel,
            "summary",
            new("https://success.com/post1"),
            DateTime.UtcNow,
            new(5)
        );
        var digestSummary = new DigestSummaryModel(
            digestId,
            "title",
            "summary",
            1,
            5,
            DateTime.UtcNow,
            _dateFrom.ToDateTime(TimeOnly.MinValue),
            _dateTo.ToDateTime(TimeOnly.MinValue)
        );
        DigestModel? savedDigest = null;

        _settingsManagerMock
            .Setup(x => x.LoadSettings(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(GetMockSettings()));
        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    successFeedModel.FeedUrl,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(readPosts));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    failFeedModel.FeedUrl,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Fail<List<ReadPostModel>>("Failed to fetch"));
        _aiSummarizerMock
            .Setup(x =>
                x.GenerateSummary(
                    It.IsAny<PostModel>(),
                    It.IsAny<Prompts>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(postSummary));
        _aiSummarizerMock
            .Setup(x =>
                x.GeneratePostsSummary(
                    It.IsAny<List<PostModel>>(),
                    It.IsAny<Prompts>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(digestSummary));
        _digestRepositoryMock
            .Setup(x =>
                x.SaveDigestForCurrentUser(It.IsAny<DigestModel>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Result.Ok())
            .Callback<DigestModel, CancellationToken>((digest, _) => savedDigest = digest);

        // Act
        var result = await _digestService.GenerateDigest(
            digestId,
            parameters,
            CancellationToken.None
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(DigestGenerationResultModelEnum.Success);
        savedDigest.Should().NotBeNull();
        savedDigest.DigestId.Should().Be(digestId);
        savedDigest.PostsSummaries.Should().ContainSingle().And.Contain(postSummary);
        savedDigest.DigestSummary.Should().BeEquivalentTo(digestSummary);
        savedDigest.UsedPrompts.Should().NotBeNull();
        savedDigest.UsedPrompts.Should().ContainKey(PromptTypeEnumModel.PostSummary);
        savedDigest.UsedPrompts.Should().ContainKey(PromptTypeEnumModel.PostImportance);
        savedDigest.UsedPrompts.Should().ContainKey(PromptTypeEnumModel.DigestSummary);
    }

    [Test]
    public async Task GenerateDigest_WithFeedFilter_ProcessesOnlySelectedFeeds()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var selectedFeedUrl = new FeedUrl("https://selected.com/feed");
        var notSelectedFeedUrl = new FeedUrl("https://not-selected.com/feed");
        var parameters = new DigestParametersModel(
            _dateFrom,
            _dateTo,
            new[] { selectedFeedUrl }.ToHashSet()
        );
        var selectedFeedModel = new FeedModel(
            selectedFeedUrl,
            "desc",
            "title",
            new("https://example.com/image.png")
        );
        var notSelectedFeedModel = new FeedModel(
            notSelectedFeedUrl,
            "desc",
            "title",
            new("https://example.com/image.png")
        );
        var feeds = new List<FeedModel> { selectedFeedModel, notSelectedFeedModel };
        var readPosts = new List<ReadPostModel>
        {
            new(new("content"), new("https://selected.com/post1"), DateTime.UtcNow),
        };
        var postSummary = new PostSummaryModel(
            selectedFeedModel,
            "summary",
            new("https://selected.com/post1"),
            DateTime.UtcNow,
            new(5)
        );
        var digestSummary = new DigestSummaryModel(
            digestId,
            "title",
            "summary",
            1,
            5,
            DateTime.UtcNow,
            _dateFrom.ToDateTime(TimeOnly.MinValue),
            _dateTo.ToDateTime(TimeOnly.MinValue)
        );
        DigestModel? savedDigest = null;

        _settingsManagerMock
            .Setup(x => x.LoadSettings(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(GetMockSettings()));
        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    selectedFeedModel.FeedUrl,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(readPosts));
        _aiSummarizerMock
            .Setup(x =>
                x.GenerateSummary(
                    It.IsAny<PostModel>(),
                    It.IsAny<Prompts>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(postSummary));
        _aiSummarizerMock
            .Setup(x =>
                x.GeneratePostsSummary(
                    It.IsAny<List<PostModel>>(),
                    It.IsAny<Prompts>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(digestSummary));
        _digestRepositoryMock
            .Setup(x =>
                x.SaveDigestForCurrentUser(It.IsAny<DigestModel>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Result.Ok())
            .Callback<DigestModel, CancellationToken>((digest, _) => savedDigest = digest);

        // Act
        var result = await _digestService.GenerateDigest(
            digestId,
            parameters,
            CancellationToken.None
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(DigestGenerationResultModelEnum.Success);
        _feedReaderMock.Verify(
            x =>
                x.FetchPosts(
                    selectedFeedModel.FeedUrl,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _feedReaderMock.Verify(
            x =>
                x.FetchPosts(
                    notSelectedFeedModel.FeedUrl,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        savedDigest.Should().NotBeNull();
        savedDigest.DigestId.Should().Be(digestId);
        savedDigest.PostsSummaries.Should().ContainSingle().And.Contain(postSummary);
        savedDigest.DigestSummary.Should().BeEquivalentTo(digestSummary);
        savedDigest.UsedPrompts.Should().NotBeNull();
        savedDigest.UsedPrompts.Should().ContainKey(PromptTypeEnumModel.PostSummary);
        savedDigest.UsedPrompts.Should().ContainKey(PromptTypeEnumModel.PostImportance);
        savedDigest.UsedPrompts.Should().ContainKey(PromptTypeEnumModel.DigestSummary);
    }

    // Medium Importance Tests

    [Test]
    public async Task GenerateDigest_WhenFeedsRepositoryFails_ReturnsFailure()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var parameters = new DigestParametersModel(_dateFrom, _dateTo);

        _settingsManagerMock
            .Setup(x => x.LoadSettings(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(GetMockSettings()));
        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<List<FeedModel>>("Failed to load feeds"));

        // Act
        var result = await _digestService.GenerateDigest(
            digestId,
            parameters,
            CancellationToken.None
        );

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("Failed to load feeds");
    }

    [Test]
    public async Task GenerateDigest_WithNoFeedsSelected_ReturnsFailure()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var parameters = new DigestParametersModel(_dateFrom, _dateTo, new HashSet<FeedUrl>());
        var feedUrl = new FeedUrl("https://example.com/feed");
        var feedModel = new FeedModel(
            feedUrl,
            "desc",
            "title",
            new("https://example.com/image.png")
        );
        var feeds = new List<FeedModel> { feedModel };

        _settingsManagerMock
            .Setup(x => x.LoadSettings(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(GetMockSettings()));
        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));

        // Act
        var result = await _digestService.GenerateDigest(
            digestId,
            parameters,
            CancellationToken.None
        );

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Contain("No feeds selected");
    }

    [Test]
    public async Task GenerateDigest_WhenAllFeedsFailToLoad_ReturnsFailure()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var parameters = new DigestParametersModel(_dateFrom, _dateTo);
        var failFeedUrl1 = new FeedUrl("https://fail1.com/feed");
        var failFeedUrl2 = new FeedUrl("https://fail2.com/feed");
        var failFeedModel1 = new FeedModel(
            failFeedUrl1,
            "desc",
            "title",
            new("https://example.com/image.png")
        );
        var failFeedModel2 = new FeedModel(
            failFeedUrl2,
            "desc",
            "title",
            new("https://example.com/image.png")
        );
        var feeds = new List<FeedModel> { failFeedModel1, failFeedModel2 };

        _settingsManagerMock
            .Setup(x => x.LoadSettings(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(GetMockSettings()));
        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    It.IsAny<FeedUrl>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Fail<List<ReadPostModel>>("Failed to fetch"));

        // Act
        var result = await _digestService.GenerateDigest(
            digestId,
            parameters,
            CancellationToken.None
        );

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("Failed to read all feeds");
    }

    [Test]
    public async Task GenerateDigest_WhenAiSummarizerFailsOnPostSummary_ReturnsFailure()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var parameters = new DigestParametersModel(_dateFrom, _dateTo);
        var feedUrl = new FeedUrl("https://example.com/feed");
        var feedModel = new FeedModel(
            feedUrl,
            "desc",
            "title",
            new("https://example.com/image.png")
        );
        var feeds = new List<FeedModel> { feedModel };
        var readPosts = new List<ReadPostModel>
        {
            new(new("content"), new("https://example.com/post1"), DateTime.UtcNow),
        };

        _settingsManagerMock
            .Setup(x => x.LoadSettings(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(GetMockSettings()));
        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    feedModel.FeedUrl,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(readPosts));
        _aiSummarizerMock
            .Setup(x =>
                x.GenerateSummary(
                    It.IsAny<PostModel>(),
                    It.IsAny<Prompts>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Fail<PostSummaryModel>("AI failure"));

        // Act
        var result = await _digestService.GenerateDigest(
            digestId,
            parameters,
            CancellationToken.None
        );

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("AI failure");
    }

    [Test]
    public async Task GenerateDigest_WhenAiSummarizerFailsOnDigestSummary_ReturnsFailure()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var parameters = new DigestParametersModel(_dateFrom, _dateTo);
        var feedUrl = new FeedUrl("https://example.com/feed");
        var feedModel = new FeedModel(
            feedUrl,
            "desc",
            "title",
            new("https://example.com/image.png")
        );
        var feeds = new List<FeedModel> { feedModel };
        var readPosts = new List<ReadPostModel>
        {
            new(new("content"), new("https://example.com/post1"), DateTime.UtcNow),
        };
        var postSummary = new PostSummaryModel(
            feedModel,
            "summary",
            new("https://example.com/post1"),
            DateTime.UtcNow,
            new(5)
        );

        _settingsManagerMock
            .Setup(x => x.LoadSettings(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(GetMockSettings()));
        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    feedModel.FeedUrl,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(readPosts));
        _aiSummarizerMock
            .Setup(x =>
                x.GenerateSummary(
                    It.IsAny<PostModel>(),
                    It.IsAny<Prompts>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(postSummary));
        _aiSummarizerMock
            .Setup(x =>
                x.GeneratePostsSummary(
                    It.IsAny<List<PostModel>>(),
                    It.IsAny<Prompts>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Fail<DigestSummaryModel>("AI failure"));

        // Act
        var result = await _digestService.GenerateDigest(
            digestId,
            parameters,
            CancellationToken.None
        );

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("AI failure");
    }

    [Test]
    public async Task GenerateDigest_WhenDigestRepositoryFailsToSave_ReturnsFailure()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var parameters = new DigestParametersModel(_dateFrom, _dateTo);
        var feedUrl = new FeedUrl("https://example.com/feed");
        var feedModel = new FeedModel(
            feedUrl,
            "desc",
            "title",
            new("https://example.com/image.png")
        );
        var feeds = new List<FeedModel> { feedModel };
        var readPosts = new List<ReadPostModel>
        {
            new(new("content"), new("https://example.com/post1"), DateTime.UtcNow),
        };
        var postSummary = new PostSummaryModel(
            feedModel,
            "summary",
            new("https://example.com/post1"),
            DateTime.UtcNow,
            new(5)
        );
        var digestSummary = new DigestSummaryModel(
            digestId,
            "title",
            "summary",
            1,
            5,
            DateTime.UtcNow,
            _dateFrom.ToDateTime(TimeOnly.MinValue),
            _dateTo.ToDateTime(TimeOnly.MinValue)
        );

        _settingsManagerMock
            .Setup(x => x.LoadSettings(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(GetMockSettings()));
        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    feedModel.FeedUrl,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(readPosts));
        _aiSummarizerMock
            .Setup(x =>
                x.GenerateSummary(
                    It.IsAny<PostModel>(),
                    It.IsAny<Prompts>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(postSummary));
        _aiSummarizerMock
            .Setup(x =>
                x.GeneratePostsSummary(
                    It.IsAny<List<PostModel>>(),
                    It.IsAny<Prompts>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(digestSummary));
        _digestRepositoryMock
            .Setup(x =>
                x.SaveDigestForCurrentUser(It.IsAny<DigestModel>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Result.Fail("DB error"));

        // Act
        var result = await _digestService.GenerateDigest(
            digestId,
            parameters,
            CancellationToken.None
        );

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("DB error");
    }

    private static SettingsModel GetMockSettings() =>
        new(
            EmailRecipient: "test@example.com",
            DigestTime: new(TimeOnly.MinValue),
            SmtpSettings: new(new("smtp.example.com"), 25, "user", "pass", false),
            OpenAiSettings: new("key", "model", new("http://localhost")),
            PromptSettings: new(
                new("post summary {Content}"),
                new("post importance {Content}"),
                new("digest summary {Content}")
            )
        );
}
