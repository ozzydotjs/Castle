using Castle.Core.Interfaces;
using Castle.Core.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Castle.Core.Services;

public class StreamSnipper
{
    private readonly PlaybackService? _playback;
    private readonly QueueService? _queue;
    private readonly string _ytdlpPath;
    private readonly string _coversPath;

    public StreamSnipper(PlaybackService? playback = null, QueueService? queue = null)
    {
        _playback = playback;
        _queue = queue;
        _ytdlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe");
        _coversPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Castle", "covers");
        Directory.CreateDirectory(_coversPath);
        _ = CleanOldTempFilesAsync();
    }

    public async Task PlayPreviewAsync(string videoId, IAudioEngine audioEngine, int seconds = 30)
    {
        await PlayStreamAsync(videoId, null, null, null, audioEngine, seconds);
    }

    public async Task PlayStreamAsync(string videoId, string title, string artist, string? thumbnailUrl, IAudioEngine audioEngine, int seconds = 0)
    {
        var song = await DownloadToTempAsync(videoId, title, artist, thumbnailUrl);
        if (song == null) return;

        if (_playback != null)
        {
            _queue?.Clear();
            _queue?.Add(song);
            _playback.PlaySong(song);
        }
        else
        {
            audioEngine.Initialize();
            audioEngine.Play(song.FilePath);
        }
    }

    public async Task<Song?> DownloadToTempAsync(string videoId, string title, string artist, string? thumbnailUrl)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"castle_stream_{videoId}");
        var coverPath = Path.Combine(_coversPath, $"stream_{videoId}.jpg");

        try
        {
            if (!string.IsNullOrEmpty(thumbnailUrl) && !File.Exists(coverPath))
            {
                _ = DownloadThumbnailAsync(thumbnailUrl, coverPath);
            }

            var args = $"-f bestaudio --extract-audio --audio-format mp3 --audio-quality 0 " +
                       $"--no-playlist -o \"{tempPath}.%(ext)s\" " +
                       $"--socket-timeout 10 --retries 2 " +
                       $"\"https://youtube.com/watch?v={videoId}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ytdlpPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            var outputBuilder = new System.Text.StringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    outputBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            await process.WaitForExitAsync();

            var finalPath = tempPath + ".mp3";

            if (File.Exists(finalPath))
            {
                var duration = ParseDuration(outputBuilder.ToString());

                return new Song
                {
                    Id = $"stream_{videoId}",
                    Title = title ?? "Streaming Track",
                    Artist = artist ?? "Unknown",
                    FilePath = finalPath,
                    Duration = duration,
                    CoverArtPath = File.Exists(coverPath) ? coverPath : null
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StreamSnipper] DownloadToTemp error: {ex.Message}");
        }

        return null;
    }

    private static TimeSpan ParseDuration(string ytdlpOutput)
    {
        try
        {
            var match = Regex.Match(ytdlpOutput, @"Duration[:\s]+(\d+):(\d+):(\d+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return new TimeSpan(
                    int.Parse(match.Groups[1].Value),
                    int.Parse(match.Groups[2].Value),
                    int.Parse(match.Groups[3].Value));
            }

            match = Regex.Match(ytdlpOutput, @"(\d+):(\d+):(\d+)[\.\s]", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return new TimeSpan(
                    int.Parse(match.Groups[1].Value),
                    int.Parse(match.Groups[2].Value),
                    int.Parse(match.Groups[3].Value));
            }
        }
        catch { }
        return TimeSpan.Zero;
    }

    private static async Task DownloadThumbnailAsync(string url, string outputPath)
    {
        try
        {
            using var client = new HttpClient();
            var bytes = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(outputPath, bytes);
        }
        catch { }
    }

    private static async Task CleanOldTempFilesAsync()
    {
        try
        {
            var tempDir = Path.GetTempPath();
            var files = Directory.GetFiles(tempDir, "castle_stream_*.mp3");
            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < DateTime.Now.AddHours(-1))
                        File.Delete(file);
                }
                catch { }
            }
        }
        catch { }
    }
}