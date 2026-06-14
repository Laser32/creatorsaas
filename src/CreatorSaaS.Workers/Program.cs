using CreatorSaaS.Application.Commands.Videos;
using CreatorSaaS.Application.Services;
using CreatorSaaS.Core.Interfaces;
using CreatorSaaS.Infrastructure.Cache;
using CreatorSaaS.Infrastructure.Data;
using CreatorSaaS.Infrastructure.Repositories;
using CreatorSaaS.Infrastructure.Services;
using CreatorSaaS.Infrastructure.Services.AI;
using CreatorSaaS.Infrastructure.Services.Storage;
using CreatorSaaS.Workers.Analytics;
using CreatorSaaS.Workers.VideoProcessing;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/workers-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting CreatorSaaS Workers...");

    var builder = Host.CreateApplicationBuilder(args);
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();

    var config = builder.Configuration;
    var connStr = config.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' not configured");
    var redisConn = config["Redis:Connection"] ?? "localhost:6379";

    builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(connStr));

    var redis = ConnectionMultiplexer.Connect(redisConn);
    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IJwtService, JwtService>();
    builder.Services.AddScoped<ICacheService, RedisCacheService>();
    builder.Services.AddScoped<IStorageService, LocalStorageService>();
    builder.Services.AddScoped<IEmailService, SendGridEmailService>();

    builder.Services.AddHttpClient<IAIScriptService, AIScriptService>();
    builder.Services.AddHttpClient<IVoiceSynthesisService, ElevenLabsVoiceService>();
    builder.Services.AddHttpClient<IBRollService, PexelsBRollService>();
    builder.Services.AddScoped<IVideoRenderService, FFMpegVideoRenderService>();
    builder.Services.AddScoped<IThumbnailService, ThumbnailService>();
    builder.Services.AddHttpClient<IYouTubeUploadService, YouTubeUploadService>();

    builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();
    builder.Services.AddHttpClient<IAnalyticsService, AnalyticsService>();
    builder.Services.AddScoped<AnalyticsScheduler>();
    builder.Services.AddScoped<QuotaResetScheduler>();

    builder.Services.AddHangfire(cfg =>
        cfg.UsePostgreSqlStorage(opts => opts.UseConnection(connStr),
            new PostgreSqlStorageOptions
            {
                PrepareSchemaIfNecessary = true,
                QueuePollInterval = TimeSpan.FromSeconds(5)
            })
    );

    builder.Services.AddHangfireServer(opts =>
    {
        opts.ServerName = $"CreatorSaaS-Worker-{Environment.MachineName}";
        opts.WorkerCount = Environment.ProcessorCount * 2;
        opts.Queues = new[] { "video-processing", "analytics", "default" };
        opts.SchedulePollingInterval = TimeSpan.FromSeconds(10);
    });

    var host = builder.Build();

    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied");
    }

    RecurringJobRegistration.RegisterRecurringJobs();
    Log.Information("Recurring jobs registered. Workers running. Press Ctrl+C to stop.");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Workers terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
