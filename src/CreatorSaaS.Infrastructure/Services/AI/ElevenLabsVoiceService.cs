using CreatorSaaS.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreatorSaaS.Infrastructure.Services.AI;

public class ElevenLabsVoiceService : IVoiceSynthesisService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ElevenLabsVoiceService> _logger;

    public ElevenLabsVoiceService(HttpClient httpClient, IConfiguration config, ILogger<ElevenLabsVoiceService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<string> SynthesizeAsync(string text, string voiceId, string outputPath, CancellationToken ct = default)
    {
        var apiKey = _config["ElevenLabs:ApiKey"] ?? throw new InvalidOperationException("ElevenLabs:ApiKey not configured");
        var model = _config["ElevenLabs:Model"] ?? "eleven_multilingual_v2";

        var url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}?optimize_streaming_latency=0";

        var request = new
        {
            text = text,
            model_id = model,
            voice_settings = new
            {
                stability = 0.5,
                similarity_boost = 0.75
            }
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        _httpClient.DefaultRequestHeaders.Add("xi-api-key", apiKey);

        try
        {
            var response = await _httpClient.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();

            var audioStream = await response.Content.ReadAsStreamAsync(ct);
            var directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using (var fileStream = File.Create(outputPath))
                await audioStream.CopyToAsync(fileStream, ct);

            _logger.LogInformation("Audio synthesized: {Path}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ElevenLabs synthesis failed");
            throw new ExternalServiceException("ElevenLabs", ex.Message);
        }
    }
}
