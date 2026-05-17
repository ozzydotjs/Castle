using System.Diagnostics;
using Castle.Core.Interfaces;

namespace Castle.Core.Services;

public class DownloadService
{
    private readonly ISongRepository _songRepo;
    private readonly ILibraryScanner _scanner;
    private readonly MetadataService _metadataService;
    private readonly string _downloadPath;
    private readonly string _coversPath;

    public event Action<string>? StatusChanged;
    public event Action<int>? ProgressChanged;
    public event Action? DownloadComplete;

    public DownloadService(ISongRepository songRepo, ILibraryScanner scanner, MetadataService metadataService)
    {
        _songRepo = songRepo;
        _scanner = scanner;
        _metadataService = metadataService;

        _downloadPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            "Castle"
        );

        _coversPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Castle",
            "covers"
        );

        Directory.CreateDirectory(_downloadPath);
        Directory.CreateDirectory(_coversPath);
    }

    public ILibraryScanner? GetScanner() => _scanner;

    public string DownloadPath => _downloadPath;

    public async Task DownloadAsync(string videoUrl, string title, string artist)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
        {
            StatusChanged?.Invoke("Error: Missing video URL.");
            return;
        }

        title = string.IsNullOrWhiteSpace(title) ? "Unknown Title" : title.Trim();
        artist = string.IsNullOrWhiteSpace(artist) ? "Unknown Artist" : artist.Trim();

        StatusChanged?.Invoke($"Downloading: {title}...");

        var safeBaseName = SanitizeFileName($"{artist} - {title}");
        var finalFile = GetUniqueFilePath(_downloadPath, safeBaseName, ".mp3");
        var outputBase = Path.Combine(_downloadPath, Path.GetFileNameWithoutExtension(finalFile));
        var outputTemplate = $"{outputBase}.%(ext)s";

        var thumbnailBase = Path.Combine(_coversPath, Path.GetFileNameWithoutExtension(finalFile));
        var thumbnailTemplate = $"{thumbnailBase}.%(ext)s";

        var ytdlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe");
        var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");

        if (!File.Exists(ytdlpPath))
        {
            StatusChanged?.Invoke("Error: yt-dlp.exe not found.");
            return;
        }

        if (!File.Exists(ffmpegPath))
        {
            StatusChanged?.Invoke("Error: ffmpeg.exe not found.");
            return;
        }

        try
        {
            var thumbArgs =
                $"--write-thumbnail --skip-download " +
                $"--convert-thumbnails webp " +
                $"-o \"{thumbnailTemplate}\" " +
                $"\"{videoUrl}\"";

            var thumbProcess = Process.Start(new ProcessStartInfo
            {
                FileName = ytdlpPath,
                Arguments = thumbArgs,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (thumbProcess != null)
            {
                await thumbProcess.WaitForExitAsync();
            }

            var args =
                $"--extractor-args \"youtube:client=mweb\" " +
                $"-f bestaudio --extract-audio --audio-format mp3 --audio-quality 0 " +
                $"--embed-thumbnail --embed-metadata " +
                $"--ffmpeg-location \"{ffmpegPath.Replace("\\", "/")}\" " +
                $"-N 4 --no-part --socket-timeout 10 --retries 3 " +
                $"--sleep-requests 1 --sleep-interval 1 --max-sleep-interval 3 " +
                $"--no-check-certificate --prefer-insecure " +
                $"-o \"{outputTemplate}\" --no-playlist \"{videoUrl}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ytdlpPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            var errorOutput = new System.Text.StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    Debug.WriteLine($"[yt-dlp] {e.Data}");

                    if (TryParseProgress(e.Data, out var percent))
                    {
                        ProgressChanged?.Invoke(percent);
                    }
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    errorOutput.AppendLine(e.Data);
                    Debug.WriteLine($"[yt-dlp error] {e.Data}");

                    if (TryParseProgress(e.Data, out var percent))
                    {
                        ProgressChanged?.Invoke(percent);
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                var downloadedFile = ResolveDownloadedFile(finalFile, outputBase);

                if (string.IsNullOrWhiteSpace(downloadedFile) || !File.Exists(downloadedFile))
                {
                    StatusChanged?.Invoke("Download finished, but the output file was not found.");
                    return;
                }

                _metadataService.WriteMetadata(downloadedFile, title, artist);

                StatusChanged?.Invoke("Download complete!");
                DownloadComplete?.Invoke();
            }
            else
            {
                var err = errorOutput.ToString();
                StatusChanged?.Invoke("Download failed.");
                Debug.WriteLine($"yt-dlp error: {err}");
            }
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"Error: {ex.Message}");
        }
    }

    private static bool TryParseProgress(string text, out int percent)
    {
        percent = 0;

        var percentIndex = text.IndexOf('%');

        if (percentIndex <= 0)
        {
            return false;
        }

        var beforePercent = text[..percentIndex].Trim();
        var lastSpace = beforePercent.LastIndexOf(' ');
        var rawPercent = lastSpace >= 0 ? beforePercent[(lastSpace + 1)..] : beforePercent;

        if (double.TryParse(rawPercent, out var parsed))
        {
            percent = Math.Clamp((int)Math.Round(parsed), 0, 100);
            return true;
        }

        return false;
    }

    private static string ResolveDownloadedFile(string expectedMp3Path, string outputBase)
    {
        if (File.Exists(expectedMp3Path))
        {
            return expectedMp3Path;
        }

        var folder = Path.GetDirectoryName(expectedMp3Path);

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return expectedMp3Path;
        }

        var baseName = Path.GetFileNameWithoutExtension(outputBase);

        var newestMatch = Directory.GetFiles(folder, $"{baseName}.*")
            .Where(path => Path.GetExtension(path).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(path => new FileInfo(path).LastWriteTimeUtc)
            .FirstOrDefault();

        return newestMatch ?? expectedMp3Path;
    }

    public static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Untitled";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());

        sanitized = sanitized.Trim();

        while (sanitized.Contains("  ", StringComparison.Ordinal))
        {
            sanitized = sanitized.Replace("  ", " ");
        }

        sanitized = sanitized.Trim('.', ' ');

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "Untitled";
        }

        return sanitized.Length > 100 ? sanitized[..100].Trim() : sanitized;
    }

    public static string GetUniqueFilePath(string folder, string baseName, string extension)
    {
        Directory.CreateDirectory(folder);

        baseName = SanitizeFileName(baseName);

        if (!extension.StartsWith('.'))
        {
            extension = "." + extension;
        }

        var path = Path.Combine(folder, $"{baseName}{extension}");

        if (!File.Exists(path))
        {
            return path;
        }

        var counter = 1;

        while (true)
        {
            var candidate = Path.Combine(folder, $"{baseName} ({counter}){extension}");

            if (!File.Exists(candidate))
            {
                return candidate;
            }

            counter++;
        }
    }
}