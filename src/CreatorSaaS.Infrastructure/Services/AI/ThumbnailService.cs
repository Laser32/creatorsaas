using CreatorSaaS.Core.Entities;
using CreatorSaaS.Core.Interfaces;
using FFMpegCore;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Text;

namespace CreatorSaaS.Infrastructure.Services.AI;

public class ThumbnailService : IThumbnailService
{
    private readonly ILogger<ThumbnailService> _logger;

    public ThumbnailService(ILogger<ThumbnailService> logger) => _logger = logger;

    public async Task<string> GenerateThumbnailAsync(string title, string videoFilePath, string outputPath,
        CancellationToken ct = default)
    {
        try
        {
            // Extract frame from video at 3 seconds
            var tempFrame = Path.Combine(Path.GetTempPath(), $"frame_{Guid.NewGuid()}.png");

            await FFMpegArguments
                .FromFileInput(videoFilePath)
                .OutputToFile(tempFrame, true, options => options
                    .Seek(TimeSpan.FromSeconds(3))
                    .WithFrameOutputCount(1))
                .ProcessAsynchronously(true, ct);

            // Add text overlay to frame
            AddTextOverlay(tempFrame, title, outputPath);

            File.Delete(tempFrame);

            _logger.LogInformation("Thumbnail generated: {Path}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Thumbnail generation failed");
            throw new ExternalServiceException("ThumbnailService", ex.Message);
        }
    }

    private void AddTextOverlay(string inputImage, string text, string outputPath)
    {
        using (var image = Image.FromFile(inputImage))
        using (var graphics = Graphics.FromImage(image))
        {
            // YouTube thumbnail dimensions: 1280x720
            var thumbnail = new Bitmap(1280, 720);
            using (var g = Graphics.FromImage(thumbnail))
            {
                // Scale and center the image
                var scaled = ScaleImage(image, 1280, 720);
                g.DrawImage(scaled, 0, 0);

                // Add dark overlay
                using (var brush = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
                    g.FillRectangle(brush, 0, 580, 1280, 140);

                // Add text
                var font = new Font("Arial", 48, FontStyle.Bold);
                var textBrush = new SolidBrush(Color.White);
                var textSize = g.MeasureString(text, font);

                // Center text
                var x = (1280 - textSize.Width) / 2;
                var y = 600;

                g.DrawString(text, font, textBrush, x, y);

                font.Dispose();
                textBrush.Dispose();
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "");
            thumbnail.Save(outputPath);
            thumbnail.Dispose();
        }
    }

    private static Image ScaleImage(Image image, int maxWidth, int maxHeight)
    {
        var ratio = Math.Min((double)maxWidth / image.Width, (double)maxHeight / image.Height);
        var newWidth = (int)(image.Width * ratio);
        var newHeight = (int)(image.Height * ratio);

        var newImage = new Bitmap(newWidth, newHeight);
        using (var g = Graphics.FromImage(newImage))
            g.DrawImage(image, 0, 0, newWidth, newHeight);

        return newImage;
    }
}
