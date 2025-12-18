using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RSSAIzer.Backend.Db;
using RSSAIzer.Backend.Infrastructure;
using RSSAIzer.Backend.Models;

namespace RSSAIzer.Backend.Tests.IntegrationTests;

[TestFixture]
public sealed class UserIsolationTests
{
    private static ApplicationDbContext CreateDbContext(string dbName) =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName).Options
        );

    private static DigestRepository CreateDigestRepo(ApplicationDbContext ctx, Guid userId)
    {
        var userContext = Mock.Of<ICurrentUserContext>(x => x.UserId == userId);
        var logger = Mock.Of<ILogger<DigestRepository>>();
        return new(ctx, logger, userContext);
    }

    private static FeedsRepository CreateFeedsRepo(ApplicationDbContext ctx, Guid userId)
    {
        var userContext = Mock.Of<ICurrentUserContext>(x => x.UserId == userId);
        var logger = Mock.Of<ILogger<FeedsRepository>>();
        return new(ctx, logger, userContext);
    }

    private static SettingsRepository CreateSettingsRepo(ApplicationDbContext ctx, Guid userId)
    {
        var userContext = Mock.Of<ICurrentUserContext>(x => x.UserId == userId);
        var logger = Mock.Of<ILogger<SettingsRepository>>();
        return new(ctx, logger, userContext);
    }

    [Test]
    public async Task DigestRepository_UserIsolation_Works()
    {
        var dbName = Guid.NewGuid().ToString();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // User A saves a digest
        await using (var ctx = CreateDbContext(dbName))
        {
            var repoA = CreateDigestRepo(ctx, userA);
            var digestIdA = new DigestId(Guid.NewGuid());
            var digestA = new DigestModel(digestIdA, [], MakeSummary(digestIdA, "titleA"), new());
            await repoA.SaveDigestForCurrentUser(digestA, CancellationToken.None);
        }
        // User B saves a digest
        await using (var ctx = CreateDbContext(dbName))
        {
            var repoB = CreateDigestRepo(ctx, userB);
            var digestIdB = new DigestId(Guid.NewGuid());
            var digestB = new DigestModel(digestIdB, [], MakeSummary(digestIdB, "titleB"), new());
            await repoB.SaveDigestForCurrentUser(digestB, CancellationToken.None);
        }
        // User A only sees their digest
        await using (var ctx = CreateDbContext(dbName))
        {
            var repoA = CreateDigestRepo(ctx, userA);
            var result = await repoA.LoadAllDigestsForCurrentUser(CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            var digests = result.Value;
            digests.Should().ContainSingle();
            digests.Single().DigestSummary.Title.Should().Be("titleA");
        }
        // User B only sees their digest
        await using (var ctx = CreateDbContext(dbName))
        {
            var repoB = CreateDigestRepo(ctx, userB);
            var result = await repoB.LoadAllDigestsForCurrentUser(CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            var digests = result.Value;
            digests.Should().ContainSingle();
            digests.Single().DigestSummary.Title.Should().Be("titleB");
        }

        return;

        // Helper for summary
        DigestSummaryModel MakeSummary(DigestId id, string title) =>
            new(id, title, "posts summary", 1, 5.0, now, now, now);
    }

    [Test]
    public async Task FeedsRepository_UserIsolation_Works()
    {
        var dbName = Guid.NewGuid().ToString();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        // User A saves a feed
        await using (var ctx = CreateDbContext(dbName))
        {
            var repoA = CreateFeedsRepo(ctx, userA);
            var feedA = new FeedModel(
                new("https://a.com/rss"),
                "descA",
                "titleA",
                new("https://a.com/img.png")
            );
            await repoA.SaveFeed(feedA, CancellationToken.None);
        }
        // User B saves a feed
        await using (var ctx = CreateDbContext(dbName))
        {
            var repoB = CreateFeedsRepo(ctx, userB);
            var feedB = new FeedModel(
                new("https://b.com/rss"),
                "descB",
                "titleB",
                new("https://b.com/img.png")
            );
            await repoB.SaveFeed(feedB, CancellationToken.None);
        }
        // User A only sees their feed
        await using (var ctx = CreateDbContext(dbName))
        {
            var repoA = CreateFeedsRepo(ctx, userA);
            var result = await repoA.LoadFeeds(CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            var feeds = result.Value;
            feeds.Should().ContainSingle();
            feeds.Single().Title.Should().Be("titleA");
        }
        // User B only sees their feed
        await using (var ctx = CreateDbContext(dbName))
        {
            var repoB = CreateFeedsRepo(ctx, userB);
            var result = await repoB.LoadFeeds(CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            var feeds = result.Value;
            feeds.Should().ContainSingle();
            feeds.Single().Title.Should().Be("titleB");
        }
    }

    [Test]
    public async Task SettingsRepository_UserIsolation_Works()
    {
        var dbName = Guid.NewGuid().ToString();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var smtpA = new SmtpSettingsModel(new("smtp.a.com"), 25, "userA", "passA", false);
        var smtpB = new SmtpSettingsModel(new("smtp.b.com"), 25, "userB", "passB", false);
        var openAi = new OpenAiSettingsModel("apikey", "gpt-3", new("https://openai.com"));
        var promptsA = new PromptSettingsModel(
            new("A {Content}"),
            new("A {Content}"),
            new("A {Content}")
        );
        var promptsB = new PromptSettingsModel(
            new("B {Content}"),
            new("B {Content}"),
            new("B {Content}")
        );
        var digestTime = new TimeUtc(TimeOnly.FromDateTime(DateTime.UtcNow));

        // User A saves settings
        await using (var ctx = CreateDbContext(dbName))
        {
            var repoA = CreateSettingsRepo(ctx, userA);
            var settingsA = new SettingsModel("a@example.com", digestTime, smtpA, openAi, promptsA);
            await repoA.SaveSettings(settingsA, CancellationToken.None);
        }
        // User B saves settings
        await using (var ctx = CreateDbContext(dbName))
        {
            var repoB = CreateSettingsRepo(ctx, userB);
            var settingsB = new SettingsModel("b@example.com", digestTime, smtpB, openAi, promptsB);
            await repoB.SaveSettings(settingsB, CancellationToken.None);
        }
        // User A only sees their settings
        await using (var ctx = CreateDbContext(dbName))
        {
            var repoA = CreateSettingsRepo(ctx, userA);
            var resultA = await repoA.LoadSettings(CancellationToken.None);
            resultA.IsSuccess.Should().BeTrue();
            resultA.Value.Should().NotBeNull();
            resultA.Value!.EmailRecipient.Should().Be("a@example.com");
        }
        // User B only sees their settings
        await using (var ctx = CreateDbContext(dbName))
        {
            var repoB = CreateSettingsRepo(ctx, userB);
            var resultB = await repoB.LoadSettings(CancellationToken.None);
            resultB.IsSuccess.Should().BeTrue();
            resultB.Value.Should().NotBeNull();
            resultB.Value!.EmailRecipient.Should().Be("b@example.com");
        }
    }
}
