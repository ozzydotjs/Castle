using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace Castle.Core.Services;

public static class CoverArtService
{
    private static readonly string CoversFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Castle",
        "covers"
    );

    private static readonly long MaxCoversFolderSize = 500 * 1024 * 1024; // 500MB max
    private static readonly int MaxCoverFiles = 5000; // Max 5000 cover files
    private static bool _cleanupRun = false;

    static CoverArtService()
    {
        Directory.CreateDirectory(CoversFolder);
        CleanupCoversFolder();
    }

    public static void CleanupCoversFolder()
    {
        if (_cleanupRun) return;
        _cleanupRun = true;

        try
        {
            if (!Directory.Exists(CoversFolder)) return;

            var files = Directory.GetFiles(CoversFolder)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();

            if (files.Count > MaxCoverFiles)
            {
                foreach (var file in files.Skip(MaxCoverFiles))
                {
                    try { file.Delete(); } catch { }
                }
            }

            long totalSize = files.Where(f => f.Exists).Sum(f => { try { return f.Length; } catch { return 0; } });
            var remaining = files.Where(f => f.Exists).ToList();

            while (totalSize > MaxCoversFolderSize && remaining.Count > 0)
            {
                var oldest = remaining.Last();
                var size = oldest.Length;
                totalSize -= size;
                try { oldest.Delete(); } catch { }
                remaining.RemoveAt(remaining.Count - 1);
            }
        }
        catch { }
    }

    public static string GetCoverUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            // Already a remote URL or data URI - pass through
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            // Serve as file URL instead of base64 to avoid massive memory bloat
            if (File.Exists(path))
            {
                var fileName = Path.GetFileName(path);
                return $"covers/{fileName}";
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    // ========== PLAYLIST COVER COLLAGE ==========

    public static string GeneratePlaylistCover(string playlistId, List<string> songCoverPaths)
    {
        Directory.CreateDirectory(CoversFolder);
        var outputPath = Path.Combine(CoversFolder, $"playlist_{playlistId}.jpg");

        try
        {
            var validPaths = songCoverPaths
                .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
                .Take(4)
                .ToList();

            if (validPaths.Count == 0)
                return string.Empty;

            const int tileSize = 150;
            const int outputSize = 300;

            using var bitmap = new Bitmap(outputSize, outputSize);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.Clear(Color.FromArgb(26, 26, 26));

            switch (validPaths.Count)
            {
                case 1:
                    DrawTile(graphics, validPaths[0], 0, 0, outputSize, outputSize);
                    break;

                case 2:
                    DrawTile(graphics, validPaths[0], 0, 0, outputSize / 2, outputSize);
                    DrawTile(graphics, validPaths[1], outputSize / 2, 0, outputSize / 2, outputSize);
                    break;

                case 3:
                    DrawTile(graphics, validPaths[0], 0, 0, outputSize / 2, outputSize / 2);
                    DrawTile(graphics, validPaths[1], outputSize / 2, 0, outputSize / 2, outputSize / 2);
                    DrawTile(graphics, validPaths[2], outputSize / 4, outputSize / 2, outputSize / 2, outputSize / 2);
                    break;

                case 4:
                    DrawTile(graphics, validPaths[0], 0, 0, tileSize, tileSize);
                    DrawTile(graphics, validPaths[1], tileSize, 0, tileSize, tileSize);
                    DrawTile(graphics, validPaths[2], 0, tileSize, tileSize, tileSize);
                    DrawTile(graphics, validPaths[3], tileSize, tileSize, tileSize, tileSize);
                    break;
            }

            bitmap.Save(outputPath, ImageFormat.Jpeg);
            return outputPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CoverArt] Collage failed: {ex.Message}");
            return string.Empty;
        }
    }

    private static void DrawTile(Graphics graphics, string imagePath, int x, int y, int width, int height)
    {
        try
        {
            using var image = Image.FromFile(imagePath);
            var srcRect = GetFillCropRect(image.Width, image.Height, width, height);
            graphics.DrawImage(image, new Rectangle(x, y, width, height), srcRect, GraphicsUnit.Pixel);
        }
        catch { }
    }

    private static Rectangle GetFillCropRect(int imgWidth, int imgHeight, int targetWidth, int targetHeight)
    {
        var targetRatio = (float)targetWidth / targetHeight;
        var imgRatio = (float)imgWidth / imgHeight;

        if (imgRatio > targetRatio)
        {
            var newWidth = (int)(imgHeight * targetRatio);
            var offsetX = (imgWidth - newWidth) / 2;
            return new Rectangle(offsetX, 0, newWidth, imgHeight);
        }
        else
        {
            var newHeight = (int)(imgWidth / targetRatio);
            var offsetY = (imgHeight - newHeight) / 2;
            return new Rectangle(0, offsetY, imgWidth, newHeight);
        }
    }

    public static void SetCustomPlaylistCover(string playlistId, string sourceImagePath)
    {
        Directory.CreateDirectory(CoversFolder);
        var outputPath = Path.Combine(CoversFolder, $"playlist_{playlistId}.jpg");

        try
        {
            const int outputSize = 300;

            using var bitmap = new Bitmap(outputSize, outputSize);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            using var image = Image.FromFile(sourceImagePath);
            var srcRect = GetFillCropRect(image.Width, image.Height, outputSize, outputSize);
            graphics.DrawImage(image, new Rectangle(0, 0, outputSize, outputSize), srcRect, GraphicsUnit.Pixel);

            bitmap.Save(outputPath, ImageFormat.Jpeg);
        }
        catch { }
    }

    public static string GetPlaylistCoverUrl(string playlistId)
    {
        var path = Path.Combine(CoversFolder, $"playlist_{playlistId}.jpg");
        if (File.Exists(path))
        {
            return GetCoverUrl(path);
        }
        return string.Empty;
    }

    public static string GetPlaylistCoverPath(string playlistId)
    {
        var path = Path.Combine(CoversFolder, $"playlist_{playlistId}.jpg");
        return File.Exists(path) ? path : string.Empty;
    }
}