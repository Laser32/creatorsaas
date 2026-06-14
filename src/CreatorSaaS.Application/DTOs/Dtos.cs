namespace CreatorSaaS.Application.DTOs;

// ─── Auth ─────────────────────────────────────────────────────────────────────

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User,
    TenantDto Tenant
);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid TenantId,
    string? AvatarUrl
);

public record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    string Plan,
    int MonthlyVideoQuota,
    int VideosCreatedThisMonth
);

// ─── Project ──────────────────────────────────────────────────────────────────

public record CreateProjectDto(
    string Name,
    string? Description,
    string? DefaultLanguage,
    string? DefaultVoiceId,
    string? DefaultStyle
);

public record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    string? DefaultLanguage,
    string? DefaultVoiceId,
    string? DefaultStyle,
    DateTime CreatedAt
);

// ─── Channel ───────────────────────────────────────────────────────────────────

public record CreateChannelDto(
    Guid ProjectId,
    string Name
);

public record ChannelDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    bool IsConnected,
    string? YouTubeChannelId,
    long TotalViews,
    long TotalVideos
);

public record ConnectYouTubeDto(
    string AuthorizationCode,
    string RedirectUri
);

// ─── VideoJob ─────────────────────────────────────────────────────────────────

public record CreateVideoJobDto(
    Guid ProjectId,
    Guid? ChannelId,
    string Topic,
    string? Keywords,
    string Language = "en",
    string Style = "documentary",
    int TargetDurationSeconds = 300,
    string? VoiceId = null
);

public record VideoJobListDto(
    Guid Id,
    string Topic,
    string? ScriptTitle,
    int Status,
    int CurrentStep,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? YouTubeUrl,
    long? Views
);

public record VideoJobDetailDto(
    Guid Id,
    Guid ProjectId,
    Guid? ChannelId,
    string Topic,
    string? Keywords,
    string Language,
    string Style,
    int TargetDurationSeconds,
    string? VoiceId,
    int Status,
    int CurrentStep,
    string? ErrorMessage,
    int RetryCount,
    string? ScriptTitle,
    string? ScriptContent,
    string? GeneratedTitle,
    string? GeneratedDescription,
    string? Tags,
    string? YouTubeVideoId,
    string? YouTubeUrl,
    long? Views,
    long? Likes,
    long? Comments,
    double? ClickThroughRate,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    List<VideoSceneDto> Scenes
);

public record VideoSceneDto(
    Guid Id,
    int Order,
    string Title,
    string Narration,
    string? BRollQuery,
    int? DurationSeconds,
    bool AudioGenerated,
    bool BRollFetched
);

public record GenerateVideoVariantDto(
    Guid ParentVideoJobId,
    string VariantLabel,
    string? TitleOverride = null,
    string? DescriptionOverride = null
);

// ─── Pagination ───────────────────────────────────────────────────────────────

public record PaginatedResponse<T>(
    List<T> Data,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
)
{
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public record PaginationQuery(
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortOrder = "desc"
);

// ─── Billing ──────────────────────────────────────────────────────────────────

public record BillingPortalUrlDto(
    string Url
);

public record SubscriptionDto(
    Guid Id,
    string Plan,
    int Status,
    DateTime? CurrentPeriodStart,
    DateTime? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    int MonthlyVideoQuota,
    decimal PriceMonthly
);

public record CreateCheckoutSessionDto(
    string PriceId
);

public record CheckoutSessionDto(
    string SessionId,
    string Url
);

// ─── Analytics ────────────────────────────────────────────────────────────────

public record VideoAnalyticsDto(
    Guid VideoJobId,
    long? Views,
    long? Likes,
    long? Comments,
    double? ClickThroughRate,
    double? AverageViewDurationSeconds,
    DateTime? FetchedAt
);

public record ProjectAnalyticsSummaryDto(
    int TotalVideos,
    long TotalViews,
    long TotalLikes,
    double AverageClickThroughRate,
    double AverageViewDuration,
    List<VideoAnalyticsDto> TopVideos
);
