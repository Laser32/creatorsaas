using CreatorSaaS.Application.Services;
using CreatorSaaS.Core.Interfaces;
using CreatorSaaS.Infrastructure.Cache;
using CreatorSaaS.Infrastructure.Data;
using CreatorSaaS.Infrastructure.Repositories;
using CreatorSaaS.Infrastructure.Services;
using CreatorSaaS.Infrastructure.Services.AI;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Logging ──────────────────────────────────────────────────────────────────

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.MinimumLevel.Information()
       .WriteTo.Console()
       .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
);

// ─── Database ─────────────────────────────────────────────────────────────────

var connStr = builder.Configuration.GetConnectionString("Default") ??
    throw new InvalidOperationException("Connection string 'Default' not found");

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(connStr, x => x.MigrationsHistoryTable("_ef_migrations_history"))
);

// ─── Redis ────────────────────────────────────────────────────────────────────

var redisConn = builder.Configuration["Redis:Connection"] ?? "localhost:6379";
var redis = ConnectionMultiplexer.Connect(redisConn);
builder.Services.AddSingleton(redis);

// ─── Repositories & UnitOfWork ────────────────────────────────────────────────

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ─── Application Services ─────────────────────────────────────────────────────

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<IEmailService, SendGridEmailService>();

// ─── AI & External Services ───────────────────────────────────────────────────

builder.Services.AddHttpClient<IAIScriptService, AIScriptService>();
builder.Services.AddHttpClient<IVoiceSynthesisService, ElevenLabsVoiceService>();
builder.Services.AddHttpClient<IBRollService, PexelsBRollService>();
builder.Services.AddScoped<IVideoRenderService, FFMpegVideoRenderService>();
builder.Services.AddScoped<IThumbnailService, ThumbnailService>();
builder.Services.AddHttpClient<IYouTubeUploadService, YouTubeUploadService>();

// ─── MediatR ──────────────────────────────────────────────────────────────────

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(
        typeof(Program).Assembly,
        typeof(CreatorSaaS.Application.ApplicationAssembly).Assembly
    )
);

// ─── Hangfire ─────────────────────────────────────────────────────────────────

builder.Services.AddHangfire(cfg =>
    cfg.UsePostgreSqlStorage(opts =>
        opts.UseConnection(connStr),
        new PostgreSqlStorageOptions
        {
            PrepareSchemaIfNecessary = true
        }
    )
);
builder.Services.AddHangfireServer(opts =>
{
    opts.ServerName = $"CreatorSaaS-{Environment.MachineName}";
    opts.WorkerCount = Environment.ProcessorCount;
});

// ─── Authentication (JWT) ─────────────────────────────────────────────────────

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT:Secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CreatorSaaS";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CreatorSaaS-Clients";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// ─── CORS ─────────────────────────────────────────────────────────────────────

builder.Services.AddCors(opts =>
    opts.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? Array.Empty<string>())
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
    )
);

// ─── Controllers & Swagger ────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT token"
    });
    opts.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// ─── Build ────────────────────────────────────────────────────────────────────

var app = builder.Build();

// ─── Middleware ───────────────────────────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// ─── Hangfire Dashboard ───────────────────────────────────────────────────────

var dashboardUser = app.Configuration["Hangfire:DashboardUser"] ?? "admin";
var dashboardPassword = app.Configuration["Hangfire:DashboardPassword"] ?? "password";

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter(dashboardUser, dashboardPassword) }
});

// ─── Run Migrations ───────────────────────────────────────────────────────────

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ─── Routes ───────────────────────────────────────────────────────────────────

app.MapControllers();
app.MapHangfireDashboard();

app.Run();

// ─── Hangfire Authorization Filter ────────────────────────────────────────────

public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    private readonly string _user;
    private readonly string _password;

    public HangfireAuthorizationFilter(string user, string password)
    {
        _user = user;
        _password = password;
    }

    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var auth = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(
            httpContext.Request.Headers["Authorization"]
        );
        if (auth.Scheme != "Basic")
            return false;

        var credentialBytes = Convert.FromBase64String(auth.Parameter ?? "");
        var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);

        return credentials[0] == _user && credentials[1] == _password;
    }
}

// ─── AssemblyMarker ───────────────────────────────────────────────────────────

namespace CreatorSaaS.Application
{
    public static class ApplicationAssembly { }
}
