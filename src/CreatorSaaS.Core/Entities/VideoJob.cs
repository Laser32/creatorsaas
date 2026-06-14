using CreatorSaaS.Core.Enums;

namespace CreatorSaaS.Core.Entities;

public class VideoJob : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid CreatedByUserId { get; set; }

    // Input
    public string Topic { get; set; } = string.Empty;
    public string? Keywords { get; set; }
    public string Language { get; set; } = "en";
    public string Style { get; set; } = "documentary";      // documentary | vlog | tutorial | shorts
    public int TargetDurationSeconds { get; set; } = 300;   // 5 min default
    public string? VoiceId { get; set; }

    // Pipeline state
    public VideoJobStatus Status { get; set; } = VideoJobStatus.Pending;
    public int CurrentStep { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? HangfireJobId { get; set; }

    // Script output
    public string? ScriptTitle { get; set; }
    public string? ScriptContent { get; set; }    // JSON with scenes array
    public string? GeneratedTitle { get; set; }
    public string? GeneratedDescription { get; set; }
    public string? Tags { get; set; }             // comma-separated

    // Media assets
    public string? AudioFilePath { get; set; }
    public string? VideoFilePath { get; set; }
    public string? ThumbnailFilePath { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? DurationSeconds { get; set; }
    public long? FileSizeBytes { get; set; }

    // YouTube
    public string? YouTubeVideoId { get; set; }
    public string? YouTubeUrl { get; set; }
    public DateTime? PublishedAt { get; set; }

    // Analytics (fetched from YouTube)
    public long? Views { get; set; }
    public long? Likes { get; set; }
    public long? Comments { get; set; }
    public double? ClickThroughRate { get; set; }
    public double? AverageViewDurationSeconds { get; set; }
    public DateTime? AnalyticsLastFetchedAt { get; set; }

    // A/B Testing
    public bool IsVariant { get; set; } = false;
    public Guid? ParentJobId { get; set; }
    public string? VariantLabel { get; set; }     // "A" | "B" | "C"

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public Channel? Channel { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<VideoScene> Scenes { get; set; } = new List<VideoScene>();
    public ICollection<VideoJob> Variants { get; set; } = new List<VideoJob>();
}
