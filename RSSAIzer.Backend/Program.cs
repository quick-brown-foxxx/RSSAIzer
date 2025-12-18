using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RSSAIzer.Backend.Db;
using RSSAIzer.Backend.Features;
using RSSAIzer.Backend.Features.DigestFromRssGeneration;
using RSSAIzer.Backend.Features.DigestParallelProcessing;
using RSSAIzer.Backend.Features.DigestSteps;
using RSSAIzer.Backend.Infrastructure;
using RSSAIzer.Backend.Models;
using RSSAIzer.Backend.Options;

namespace RSSAIzer.Backend;

public static class ServiceCollectionExtensions
{
    public static void AddBackendCustom(this IHostApplicationBuilder builder)
    {
        builder
            .Services.AddOptions<BackendDeploymentOptions>()
            .Bind(builder.Configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder
            .Services.AddOptions<AiOptions>()
            .Bind(builder.Configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder
            .Services.AddOptions<TgRssProvidersOptions>()
            .Bind(builder.Configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder
            .Services.AddOptions<SettingsOptions>()
            .Bind(builder.Configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Explicitly validate options immediately
        using (var tempSp = builder.Services.BuildServiceProvider())
        {
            _ = tempSp.GetRequiredService<IOptions<TgRssProvidersOptions>>().Value;
            _ = tempSp.GetRequiredService<IOptions<BackendDeploymentOptions>>().Value;
            _ = tempSp.GetRequiredService<IOptions<SettingsOptions>>().Value;
            _ = tempSp.GetRequiredService<IOptions<AiOptions>>().Value;
            tempSp.Dispose();
        }

        // Application services
        // Register SingleUserSeedingService first to ensure user exists before other services start
        builder.Services.AddHostedService<SingleUserSeedingService>();
        builder.Services.AddHostedService<DigestProcessor>();
        builder.Services.AddHostedService<SchedulerBackgroundService>();
        builder.Services.AddHostedService<DigestStepsProcessor>();

        // Scoped
        builder.Services.AddScoped<IFeedReader, FeedReader>();
        builder.Services.AddScoped<IFeedsService, FeedsService>();
        builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        builder.Services.AddScoped<IUserPersistenceService, UserPersistenceService>();
        builder.Services.AddScoped<IFeedsRepository, FeedsRepository>();
        builder.Services.AddScoped<IDigestService, DigestService>();
        builder.Services.AddScoped<IDigestService, DigestService>();
        builder.Services.AddScoped<IDigestRepository, DigestRepository>();
        builder.Services.AddScoped<IDigestStepsService, DigestStepsService>();
        builder.Services.AddScoped<IDigestStepsRepository, DigestStepsRepository>();
        builder.Services.AddScoped<IAiSummarizer, AiSummarizer>();
        builder.Services.AddScoped<IEmailSender, EmailSender>();
        builder.Services.AddScoped<ISettingsService, SettingsService>();
        builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
        builder.Services.AddScoped<IMainService, MainService>();
        builder.Services.AddScoped<IRssPublishingService, RssPublishingService>();
        builder.Services.AddScoped<IDigestProcessingOrchestrator, DigestProcessingOrchestrator>();
        builder.Services.AddScoped<IRssProvidersService, TgRssProvidersService>();
        builder.Services.AddScoped<UserPersistenceMiddleware>();

        // Singletons
        builder.Services.AddSingleton<IDigestStepsChannel, DigestStepsChannel>();
        builder.Services.AddSingleton<TaskManager<DigestId>>();
        builder.Services.AddSingleton<ITaskScheduler<DigestId>>(provider =>
            provider.GetRequiredService<TaskManager<DigestId>>()
        );
        builder.Services.AddSingleton<ITaskTracker<DigestId>>(sp =>
            sp.GetRequiredService<TaskManager<DigestId>>()
        );

        // Database
        builder.Services.AddDbContext<ApplicationDbContext>(
            (serviceProvider, options) =>
            {
                var deploymentOptions = serviceProvider
                    .GetRequiredService<IOptions<BackendDeploymentOptions>>()
                    .Value;
                var connectionString = deploymentOptions.GetConnectionString();
                options.UseSqlite(connectionString);
            }
        );

        // Logging
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddDebug();
        });
    }

    public static async Task UseBackendCustom(this IApplicationBuilder app)
    {
        app.UseMiddleware<UserPersistenceMiddleware>(); // TODO use service instead of middleware

        // Ensure runtime directory exists for SQLite database with fail-fast permission checks
        using var scope = app.ApplicationServices.CreateScope();
        var deploymentOptions = scope
            .ServiceProvider.GetRequiredService<IOptions<BackendDeploymentOptions>>()
            .Value;

        // Validate database permissions (fail-fast)
        SqliteDatabaseValidator.ValidateDatabasePermissions(deploymentOptions);

        // Database initial migration
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }
}
