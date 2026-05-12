namespace Castle.Core.Services;

public static class CoverArtService
{
    public static string GetCoverUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            if (path.Contains("wwwroot", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(path);
                return $"covers/{fileName}";
            }

            if (!File.Exists(path))
            {
                return string.Empty;
            }

            var extension = Path.GetExtension(path).ToLowerInvariant();

            var mimeType = extension switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".jpeg" => "image/jpeg",
                ".jpg" => "image/jpeg",
                _ => "image/jpeg"
            };

            var bytes = File.ReadAllBytes(path);
            var base64 = Convert.ToBase64String(bytes);

            return $"data:{mimeType};base64,{base64}";
        }
        catch
        {
            return string.Empty;
        }
    }
}