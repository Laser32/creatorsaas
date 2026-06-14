using System.Text.Json;
using CreatorSaaS.Core.Entities;
using CreatorSaaS.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreatorSaaS.Infrastructure.Services.AI;

public class YouTubeUploadService : IYouTubeUploadService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<YouTubeUploadService> _logger;

    public YouTubeUploadService(HttpClient httpClient, IConfiguration config, ILogger<YouTubeUploadService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<string> UploadAsync(VideoJob job, string videoFilePath, string thumbnailPath,
        string accessToken, CancellationToken ct = default)
    {
        try
        {
            // Create video metadata
            var metadata = new
            {
                snippet = new
                {
                    title = job.GeneratedTitle ?? job.ScriptTitle ?? "Untitled",
                    description = job.GeneratedDescription ?? "",
                    tags = job.Tags?.Split(',').ToArray() ?? Array.Empty<string>(),
                    categoryId = "22"  // Videos category
                },
                status = new
                {
                    privacyStatus = "public",
                    publishAt = DateTime.UtcNow.AddMinutes(1)
                }
            };

            var url = "https://www.googleapis.com/upload/youtube/v3/videos?uploadType=multipart&part=snippet,status";

            using (var formContent = new MultipartFormDataContent())
            {
                // Add metadata
                var metadataContent = new StringContent(JsonSerializer.Serialize(metadata));
                formContent.Add(metadataContent, "metadata");

                // Add video file
                using (var videoStream = File.OpenRead(videoFilePath))
                {
                    var videoContent = new StreamContent(videoStream);
                    formContent.Add(videoContent, "file", Path.GetFileName(videoFilePath));

                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    var response = await _httpClient.PostAsync(url, formContent, ct);
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync(ct);
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var videoId = jsonResponse.GetProperty("id").GetString();

                    // Upload thumbnail
                    if (File.Exists(thumbnailPath))
                        await UploadThumbnailAsync(videoId, thumbnailPath, accessToken, ct);

                    _logger.LogInformation("Video uploaded to YouTube: {VideoId}", videoId);
                    return videoId ?? throw new InvalidOperationException("No video ID returned");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YouTube upload failed");
            throw new ExternalServiceException("YouTube", ex.Message);
        }
    }

    public async Task<string> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        try
        {
            var clientId = _config["YouTube:ClientId"];
            var clientSecret = _config["YouTube:ClientSecret"];

            var request = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" }
            };

            var content = new FormUrlEncodedContent(request);
            var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content, ct);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var accessToken = jsonResponse.GetProperty("access_token").GetString();

            return accessToken ?? throw new InvalidOperationException("No access token returned");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            throw new ExternalServiceException("YouTube", ex.Message);
        }
    }

    private async Task UploadThumbnailAsync(string videoId, string thumbnailPath, string accessToken, CancellationToken ct)
    {
        try
        {
            var url = $"https://www.googleapis.com/upload/youtube/v3/thumbnails/set?videoId={videoId}";

            using (var fileStream = File.OpenRead(thumbnailPath))
            {
                var content = new StreamContent(fileStream);
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.PostAsync(url, content, ct);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Thumbnail uploaded for video: {VideoId}", videoId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Thumbnail upload failed (non-critical): {VideoId}", videoId);
        }
    }
}
