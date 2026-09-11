using System.Net.Http.Json;
using System.Speech.Synthesis;
using System.Text.Json;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

public class ElevenLabsService
{
    private readonly HttpClient _http;
    private readonly AppSettings _settings;

    public ElevenLabsService(AppSettings settings)
    {
        _settings = settings;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
    }

    public async Task SynthesizeAsync(Scene scene, string outputPath, IProgress<string>? log, CancellationToken ct)
    {
        // No ElevenLabs key → go straight to Windows TTS
        if (string.IsNullOrWhiteSpace(_settings.ElevenLabsApiKey))
        {
            log?.Report($"Synthese Szene {scene.Order} (Windows TTS — kein ElevenLabs Key)...");
            SynthesizeWithWindowsTts(scene, outputPath);
            return;
        }

        log?.Report($"Synthese Szene {scene.Order} (ElevenLabs)...");

        var url = $"https://api.elevenlabs.io/v1/text-to-speech/{_settings.ElevenLabsVoiceId}";
        var body = new
        {
            text = scene.Narration,
            model_id = "eleven_multilingual_v2",
            voice_settings = new { stability = 0.5, similarity_boost = 0.75 }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Add("xi-api-key", _settings.ElevenLabsApiKey);
        req.Headers.Add("Accept", "audio/mpeg");

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);

            // Quota exceeded → fall back to Windows TTS silently
            bool isQuotaError = err.Contains("quota_exceeded") || (int)resp.StatusCode == 429;
            if (isQuotaError)
            {
                log?.Report($"  ElevenLabs Quota erschoepft → Windows TTS Fallback fuer Szene {scene.Order}.");
                SynthesizeWithWindowsTts(scene, outputPath);
                return;
            }

            throw new InvalidOperationException($"ElevenLabs Fehler {(int)resp.StatusCode}: {err}");
        }

        await using var fs = File.Create(outputPath);
        await resp.Content.CopyToAsync(fs, ct);
        scene.AudioPath = outputPath;
    }

    /// <summary>
    /// Windows built-in TTS. Outputs a WAV file; FFmpeg reads WAV just as well as MP3.
    /// The outputPath is adjusted to .wav and stored in scene.AudioPath.
    /// </summary>
    private static void SynthesizeWithWindowsTts(Scene scene, string outputPath)
    {
        // Use .wav extension — FFmpeg handles it natively
        var wavPath = Path.ChangeExtension(outputPath, ".wav");
        using var synth = new SpeechSynthesizer();

        // Pick the best available voice for the narration language
        var voices = synth.GetInstalledVoices();
        foreach (var v in voices)
        {
            // Prefer any installed voice with a known high-quality gender
            if (v.Enabled)
            {
                synth.SelectVoice(v.VoiceInfo.Name);
                break;
            }
        }

        synth.Rate = -1;  // Slightly slower → easier to follow
        synth.Volume = 100;
        synth.SetOutputToWaveFile(wavPath);
        synth.Speak(scene.Narration);
        synth.SetOutputToNull();

        scene.AudioPath = wavPath;
    }
}
