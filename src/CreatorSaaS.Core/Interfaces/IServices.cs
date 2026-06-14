using CreatorSaaS.Core.Entities;

namespace CreatorSaaS.Core.Interfaces;

public interface IAIScriptService
{
    Task<ScriptResult> GenerateScriptAsync(string topic, string style, string language, 
        string? keywords, int targetDurationSeconds, CancellationToken ct = default);
    Task<TitleResult> GenerateTitleAndDescriptionAsync(string scriptContent, string topic, 
        CancellationToken ct = default);
}

public interface IVoiceSynthesisService
{
    Task<string> SynthesizeAsync(string text, string voiceId, string outputPath, 
        CancellationToken ct = default);
}

public interface IBRollService
{
    Task<string?> FetchClipAsync(string query, string outputPath, int durationHint = 10, 
        CancellationToken ct = default);
}

public interface IVideoRenderService
{
    Task<RenderResult> RenderAsync(VideoJob job, List<VideoScene> scenes, 
        CancellationToken ct = default);
}

public interface IThumbnailService
{
    Task<string> GenerateThumbnailAsync(string title, string videoFilePath, 
        string outputPath, CancellationToken ct = default);
}

public interface IYouTubeUploadService
{
    Task<string> UploadAsync(VideoJob job, string videoFilePath, string thumbnailPath, 
        string accessToken, CancellationToken ct = default);
    Task<string> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct = default);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}

public interface IStorageService
{
    Task<string> SaveAsync(Stream stream, string fileName, string folder, CancellationToken ct = default);
    Task<Stream> ReadAsync(string path, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
    string GetPublicUrl(string path);
}

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string name, string token, CancellationToken ct = default);
    Task SendPasswordResetEmailAsync(string email, string name, string token, CancellationToken ct = default);
    Task SendVideoCompleteEmailAsync(string email, string name, string videoTitle, string youtubeUrl, CancellationToken ct = default);
}

// DTOs returned by service interfaces
public record ScriptResult(
    string Title,
    string Content,        // JSON with scenes array
    List<SceneData> Scenes
);

public record SceneData(
    int Order,
    string Title,
    string Narration,
    string BRollQuery,
    int DurationSeconds
);

public record TitleResult(
    string Title,
    string Description,
    List<string> Tags
);

public record RenderResult(
    string OutputPath,
    int DurationSeconds,
    long FileSizeBytes
);
