using CreatorSaaS.Core.Entities;
using CreatorSaaS.Core.Interfaces;
using FFMpegCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreatorSaaS.Infrastructure.Services.AI;

public class FFMpegVideoRenderService : IVideoRenderService
{
    private readonly IConfiguration _config;
    private readonly ILogger<FFMpegVideoRenderService> _logger;

    public FFMpegVideoRenderService(IConfiguration config, ILogger<FFMpegVideoRenderService> logger)
    {
        _config = config;
        _logger = logger;

        // Configure FFmpeg path
        var ffmpegPath = config["FFmpeg:Path"];
        if (!string.IsNullOrEmpty(ffmpegPath))
            FFMpegOptions.Configure(new FFMpegOptions { RootDirectory = Path.GetDirectoryName(ffmpegPath) });
    }

    public async Task<RenderResult> RenderAsync(VideoJob job, List<VideoScene> scenes, CancellationToken ct = default)
    {
        try
        {
            // Create concat demux file for scene concatenation
            var workDir = Path.Combine(Path.GetTempPath(), $"video_{job.Id}");
            Directory.CreateDirectory(workDir);

            var concatFile = Path.Combine(workDir, "concat.txt");
            var videoSegments = new List<string>();

            // Render each scene with audio and B-Roll
            foreach (var scene in scenes.OrderBy(s => s.Order))
            {
                var sceneOutput = await RenderSceneAsync(job, scene, workDir, ct);
                videoSegments.Add(sceneOutput);
            }

            // Concatenate all scene videos
            await File.WriteAllLinesAsync(concatFile,
                videoSegments.Select(f => $"file '{Path.GetFileName(f)}'"), ct);

            var outputPath = _config["Storage:VideoOutputPath"] ?? Path.Combine(workDir, "output.mp4");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? workDir);

            var concatenated = await FFMpegArguments
                .FromConcatInput(concatFile)
                .OutputToFile(outputPath, true, options => options
                    .WithVideoCodec("libx264")
                    .WithAudioCodec("aac")
                    .WithFramerate(30)
                    .WithResolution(1920, 1080)
                    .WithVariableBitrate(5))
                .ProcessAsynchronously(true, ct);

            if (!concatenated)
                throw new InvalidOperationException("FFmpeg concatenation failed");

            var fileInfo = new FileInfo(outputPath);
            var duration = GetVideoDuration(outputPath);

            _logger.LogInformation("Video rendered: {Path} ({Size} bytes, {Duration}s)",
                outputPath, fileInfo.Length, duration);

            // Cleanup
            foreach (var segment in videoSegments)
                if (File.Exists(segment))
                    File.Delete(segment);
            File.Delete(concatFile);

            return new RenderResult(outputPath, (int)duration, fileInfo.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Video render failed");
            throw new ExternalServiceException("FFmpeg", ex.Message);
        }
    }

    private async Task<string> RenderSceneAsync(VideoJob job, VideoScene scene, string workDir, CancellationToken ct)
    {
        var sceneOutput = Path.Combine(workDir, $"scene_{scene.Order}.mp4");

        // Build FFmpeg filter for overlaying audio and video
        var inputs = new List<string>();

        if (!string.IsNullOrEmpty(scene.BRollLocalPath) && File.Exists(scene.BRollLocalPath))
            inputs.Add(scene.BRollLocalPath);

        if (!string.IsNullOrEmpty(scene.AudioFilePath) && File.Exists(scene.AudioFilePath))
            inputs.Add(scene.AudioFilePath);

        if (!inputs.Any())
            throw new InvalidOperationException($"No valid inputs for scene {scene.Order}");

        var builder = FFMpegArguments.FromFileInput(inputs[0]);

        for (int i = 1; i < inputs.Count; i++)
            builder = builder.AddFileInput(inputs[i]);

        var success = await builder
            .OutputToFile(sceneOutput, true, options => options
                .WithVideoCodec("libx264")
                .WithAudioCodec("aac")
                .WithFramerate(30)
                .WithResolution(1920, 1080)
                .WithDuration(TimeSpan.FromSeconds(scene.DurationSeconds ?? 10)))
            .ProcessAsynchronously(true, ct);

        if (!success)
            throw new InvalidOperationException($"Scene {scene.Order} rendering failed");

        return sceneOutput;
    }

    private static double GetVideoDuration(string filePath)
    {
        try
        {
            var mediaInfo = FFProbe.Analyse(filePath);
            return mediaInfo.Duration.TotalSeconds;
        }
        catch
        {
            return 0;
        }
    }
}
