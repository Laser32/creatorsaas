namespace CreatorSaaS.Core.Entities;

public class Channel : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? YouTubeChannelId { get; set; }
    public string? YouTubeAccessToken { get; set; }
    public string? YouTubeRefreshToken { get; set; }
    public DateTime? YouTubeTokenExpiry { get; set; }
    public bool IsConnected { get; set; } = false;
    public string? ThumbnailUrl { get; set; }
    public long TotalViews { get; set; } = 0;
    public long TotalVideos { get; set; } = 0;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public ICollection<VideoJob> VideoJobs { get; set; } = new List<VideoJob>();
}
