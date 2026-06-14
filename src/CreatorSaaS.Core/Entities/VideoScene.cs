namespace CreatorSaaS.Core.Entities;

public class VideoScene : BaseEntity
{
    public Guid VideoJobId { get; set; }
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Narration { get; set; } = string.Empty;
    public string? BRollQuery { get; set; }
    public string? BRollClipUrl { get; set; }
    public string? BRollLocalPath { get; set; }
    public int? DurationSeconds { get; set; }
    public string? AudioFilePath { get; set; }
    public bool AudioGenerated { get; set; } = false;
    public bool BRollFetched { get; set; } = false;

    // Navigation
    public VideoJob VideoJob { get; set; } = null!;
}
