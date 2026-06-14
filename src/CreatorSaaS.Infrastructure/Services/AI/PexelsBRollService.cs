using System.Text.Json;
using CreatorSaaS.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreatorSaaS.Infrastructure.Services.AI;

public class PexelsBRollService : IBRollService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<PexelsBRollService> _logger;

    public PexelsBRollService(HttpClient httpClient, IConfiguration config, ILogger<PexelsBRollService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<string?> FetchClipAsync(string query, string outputPath, int durationHint = 10, CancellationToken ct = default)
    {
        var apiKey = _config["Pexels:ApiKey"] ?? throw new InvalidOperationException("Pexels:ApiKey not configured");

        var url = $"https://api.pexels.com/videos/search?query={Uri.EscapeDataString(query)}&per_page=1&min_duration={durationHint}&page=1";

        _httpClient.DefaultRequestHeaders.Add("Authorization", apiKey);

        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            var videos = json.GetProperty("videos").EnumerateArray();

            var video = videos.FirstOrDefault();
            if (video.ValueKind == JsonValueKind.Undefined)
            {
                _logger.LogWarning("No videos found for query: {Query}", query);
                return null;
            }

            // Get HD video file
            var videoFiles = video.GetProperty("video_files").EnumerateArray();
            var file = videoFiles.FirstOrDefault(f =>
                f.GetProperty("quality").GetString() == "hd"
            ) ?? videoFiles.FirstOrDefault();

            if (file.ValueKind == JsonValueKind.Undefined)
                return null;

            var videoUrl = file.GetProperty("link").GetString();
            if (string.IsNullOrEmpty(videoUrl))
                return null;

            // Download video
            var videoResponse = await _httpClient.GetAsync(videoUrl, ct);
            videoResponse.EnsureSuccessStatusCode();

            var directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using (var fileStream = File.Create(outputPath))
                await videoResponse.Content.CopyToAsync(fileStream, ct);

            _logger.LogInformation("B-Roll downloaded: {Path} for query {Query}", outputPath, query);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pexels B-Roll fetch failed for query: {Query}", query);
            throw new ExternalServiceException("Pexels", ex.Message);
        }
    }
}
