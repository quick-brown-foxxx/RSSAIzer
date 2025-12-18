using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RSSAIzer.Backend.Features.DigestFromRssGeneration;
using RSSAIzer.Backend.Models;

namespace RSSAIzer.Backend.Tests.UnitTests;

[TestFixture]
public sealed class FeedReaderTests
{
    private Mock<ILogger<FeedReader>> _loggerMock;

    private const string TestFeedXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <rss version ="2.0">
          <channel>
            <title>Test Feed</title>
            <description>This is a test feed for unit tests.</description>
            <link>http://example.com/</link>
            <item>
              <title>Post 1</title>
              <link>http://example.com/post1</link>
              <pubDate>Sun, 01 Jan 2023 00:00:00 GMT</pubDate>
              <guid>http://example.com/post1</guid>
              <description>Content for post 1</description>
            </item>
            <item>
              <title>Post 2</title>
              <link>http://example.com/post2</link>
              <pubDate>Mon, 02 Jan 2023 00:00:00 GMT</pubDate>
              <guid>http://example.com/post2</guid>
              <description>Content for post 2</description>
            </item>
            <item>
              <title>Post 3</title>
              <link>http://example.com/post3</link>
              <pubDate>Tue, 03 Jan 2023 00:00:00 GMT</pubDate>
              <guid>http://example.com/post3</guid>
              <description>Content for post 3</description>
            </item>
          </channel>
        </rss>
        """;

    private const string EmptyFeedXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version ="2.0">
          <channel>
            <title>Empty Test Feed</title>
            <description>This is an empty test feed for unit tests.</description>
            <link>http://example.com/empty</link>
          </channel>
        </rss>
        """;

    private const string MalformedFeedXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version ="2.0">
          <foo> bar </foo>
        </rss>
        """;

    private static string WriteXmlToTempFile(string xml)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, xml);
        return new Uri(path).AbsoluteUri;
    }

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new();
    }

    [Test]
    public async Task FetchPosts_WithInvalidDateRange_ReturnsFailure()
    {
        var from = new DateOnly(2023, 1, 2);
        var to = new DateOnly(2023, 1, 1);
        var feedUrl = new FeedUrl("http://example.com");
        var feedReader = new FeedReader(_loggerMock.Object);
        var result = await feedReader.FetchPosts(feedUrl, from, to, CancellationToken.None);
        result.IsFailed.Should().BeTrue();
        result
            .Errors.First()
            .Message.Should()
            .Be($"Can't fetch posts, invalid period requested, from: [{from}] to [{to}]");
    }

    [Test]
    public async Task FetchPosts_WithValidFeedAndDateRange_ReturnsFilteredPosts()
    {
        var from = new DateOnly(2023, 1, 2);
        var to = new DateOnly(2023, 1, 3);
        var fileUri = WriteXmlToTempFile(TestFeedXml);
        var feedUrl = new FeedUrl(fileUri);
        var feedReader = new FeedReader(_loggerMock.Object);
        var result = await feedReader.FetchPosts(feedUrl, from, to, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(p => p.HtmlContent.HtmlString).Should().Contain("Content for post 2");
        result.Value.Select(p => p.HtmlContent.HtmlString).Should().Contain("Content for post 3");
    }

    [Test]
    public async Task FetchPosts_WithEmptyFeed_ReturnsEmptyList()
    {
        var from = new DateOnly(2023, 1, 1);
        var to = new DateOnly(2023, 1, 3);
        var fileUri = WriteXmlToTempFile(EmptyFeedXml);
        var feedUrl = new FeedUrl(fileUri);
        var feedReader = new FeedReader(_loggerMock.Object);
        var result = await feedReader.FetchPosts(feedUrl, from, to, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public async Task FetchPosts_WithMalformedFeed_ReturnsFailure()
    {
        var from = new DateOnly(2023, 1, 1);
        var to = new DateOnly(2023, 1, 3);
        var fileUri = WriteXmlToTempFile(MalformedFeedXml);
        var feedUrl = new FeedUrl(fileUri);
        var feedReader = new FeedReader(_loggerMock.Object);
        var result = await feedReader.FetchPosts(feedUrl, from, to, CancellationToken.None);
        result.IsFailed.Should().BeTrue();
    }

    [Test]
    public async Task FetchFeedInfo_WithValidFeed_ReturnsFeedInfo()
    {
        var fileUri = WriteXmlToTempFile(TestFeedXml);
        var feedUrl = new FeedUrl(fileUri);
        var feedReader = new FeedReader(_loggerMock.Object);
        var result = await feedReader.FetchFeedInfo(feedUrl, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Test Feed");
        result.Value.Description.Should().Be("This is a test feed for unit tests.");
    }

    [Test]
    public async Task FetchFeedInfo_WithMalformedFeed_ReturnsFailure()
    {
        var fileUri = WriteXmlToTempFile(MalformedFeedXml);
        var feedUrl = new FeedUrl(fileUri);
        var feedReader = new FeedReader(_loggerMock.Object);
        var result = await feedReader.FetchFeedInfo(feedUrl, CancellationToken.None);
        result.IsFailed.Should().BeTrue();
    }

    [Test]
    public void FetchPosts_WhenCancelled_ThrowsOperationCanceledException()
    {
        var from = new DateOnly(2023, 1, 1);
        var to = new DateOnly(2023, 1, 3);
        var fileUri = WriteXmlToTempFile(TestFeedXml);
        var feedUrl = new FeedUrl(fileUri);
        var feedReader = new FeedReader(_loggerMock.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var act = async () => await feedReader.FetchPosts(feedUrl, from, to, cts.Token);
        act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void FetchFeedInfo_WhenCancelled_ThrowsOperationCanceledException()
    {
        var fileUri = WriteXmlToTempFile(TestFeedXml);
        var feedUrl = new FeedUrl(fileUri);
        var feedReader = new FeedReader(_loggerMock.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var act = async () => await feedReader.FetchFeedInfo(feedUrl, cts.Token);
        act.Should().ThrowAsync<OperationCanceledException>();
    }
}
