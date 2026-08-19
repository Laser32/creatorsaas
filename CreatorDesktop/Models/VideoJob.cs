namespace CreatorDesktop.Models;

public class Scene
{
    public int Order { get; set; }
    public string Title { get; set; } = "";
    public string Narration { get; set; } = "";
    public string BRollQuery { get; set; } = "";
    public int DurationSeconds { get; set; } = 10;
    public string? AudioPath { get; set; }
    public string? BRollPath { get; set; }
    public string? SegmentPath { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? SourceVideoId { get; set; }
    public string? SourceUploader { get; set; }
    public string? SourceUploaderUrl { get; set; }
    public string? SourceLicense { get; set; }
}

public class VideoJob
{
    public string Topic { get; set; } = "";
    public string Style { get; set; } = "documentary";
    public string Language { get; set; } = "de";
    public int TargetDurationSeconds { get; set; } = 60;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public List<Scene> Scenes { get; set; } = new();
    public string WorkDir { get; set; } = "";
    public string? OutputVideoPath { get; set; }
    public string? ThumbnailPath { get; set; }
}
