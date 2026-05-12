using Castle.Core.Interfaces;
using Castle.Core.Models;
using TagLib;
using System.IO;

namespace Castle.Core.Services;

public class LibraryScanner : ILibraryScanner
{
    private static readonly string[] SupportedExtensions =
        { ".mp3", ".flac", ".wav", ".ogg", ".m4a", ".wma", ".aac" };

    private readonly LastFmService _lastFm;
    private readonly string _coversFolder;

    public LibraryScanner(LastFmService lastFm)
    {
        _lastFm = lastFm;

        _coversFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Castle",
            "covers"
        );

        Directory.CreateDirectory(_coversFolder);
    }

    public async Task<List<Song>> ScanFolderAsync(string folderPath, IProgress<int>? progress = null)
    {
        var songs = new List<Song>();

        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return songs;
        }

        Directory.CreateDirectory(_coversFolder);

        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();

        int total = files.Count;
        int processed = 0;

        foreach (var file in files)
        {
            try
            {
                string title = Path.GetFileNameWithoutExtension(file);
                string artist = "Unknown Artist";
                string album = "Unknown Album";
                string genre = "";
                TimeSpan duration = TimeSpan.Zero;
                uint trackNumber = 0;
                uint year = 0;
                bool hasCoverArt = false;
                string? coverPath = null;

                try
                {
                    using var tagFile = TagLib.File.Create(file);
                    var tag = tagFile.Tag;

                    title = !string.IsNullOrWhiteSpace(tag.Title) ? tag.Title : title;
                    artist = !string.IsNullOrWhiteSpace(tag.FirstPerformer) ? tag.FirstPerformer : artist;
                    album = !string.IsNullOrWhiteSpace(tag.Album) ? tag.Album : album;
                    duration = tagFile.Properties.Duration;
                    trackNumber = tag.Track;
                    genre = tag.FirstGenre ?? "";
                    year = tag.Year;

                    if (tag.Pictures.Length > 0)
                    {
                        try
                        {
                            var cover = tag.Pictures[0];
                            var extension = GetImageExtensionFromMimeType(cover.MimeType);
                            var coverFileName = $"cover_{Guid.NewGuid():N}{extension}";
                            coverPath = Path.Combine(_coversFolder, coverFileName);

                            System.IO.File.WriteAllBytes(coverPath, cover.Data.Data);
                            hasCoverArt = true;
                        }
                        catch
                        {
                            coverPath = null;
                            hasCoverArt = false;
                        }
                    }
                }
                catch
                {
                }

                if (!hasCoverArt)
                {
                    var baseName = Path.GetFileNameWithoutExtension(file);
                    var directory = Path.GetDirectoryName(file) ?? folderPath;

                    var imageFiles = Directory.GetFiles(directory, $"{baseName}.*")
                        .Where(IsSupportedImageFile)
                        .ToList();

                    if (imageFiles.Count > 0)
                    {
                        try
                        {
                            var sourceFile = imageFiles.First();
                            var coverFileName = $"cover_{Guid.NewGuid():N}{Path.GetExtension(sourceFile).ToLowerInvariant()}";
                            coverPath = Path.Combine(_coversFolder, coverFileName);

                            System.IO.File.Copy(sourceFile, coverPath, true);
                            hasCoverArt = true;
                        }
                        catch
                        {
                            coverPath = null;
                            hasCoverArt = false;
                        }
                    }
                }

                if (artist == "Unknown Artist")
                {
                    var extractedArtist = ExtractArtistFromFilename(Path.GetFileNameWithoutExtension(file));

                    if (!string.IsNullOrWhiteSpace(extractedArtist))
                    {
                        var genres = await _lastFm.GetArtistGenresAsync(extractedArtist);

                        if (genres.Count > 0)
                        {
                            artist = extractedArtist;

                            if (string.IsNullOrWhiteSpace(genre))
                            {
                                genre = string.Join(", ", genres.Take(3));
                            }
                        }
                    }
                }

                if (artist != "Unknown Artist" && string.IsNullOrWhiteSpace(genre))
                {
                    var genres = await _lastFm.GetArtistGenresAsync(artist);

                    if (genres.Count > 0)
                    {
                        genre = string.Join(", ", genres.Take(3));
                    }
                }

                songs.Add(new Song
                {
                    FilePath = file,
                    Title = CleanTitle(title),
                    Artist = artist,
                    Album = album,
                    Duration = duration,
                    TrackNumber = trackNumber,
                    Genre = genre,
                    Year = (int)year,
                    HasCoverArt = hasCoverArt,
                    CoverArtPath = coverPath,
                    IsFavorite = false
                });
            }
            catch
            {
            }

            processed++;

            if (total > 0)
            {
                progress?.Report((int)((float)processed / total * 100));
            }
        }

        return songs;
    }

    private static bool IsSupportedImageFile(string path)
    {
        var extension = Path.GetExtension(path);

        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetImageExtensionFromMimeType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            return ".jpg";
        }

        return mimeType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            _ => ".jpg"
        };
    }

    private string? ExtractArtistFromFilename(string filename)
    {
        var dashIndex = filename.IndexOf(" - ", StringComparison.Ordinal);

        if (dashIndex > 0)
        {
            var potentialArtist = filename[..dashIndex].Trim();
            potentialArtist = RemoveCommonSuffixes(potentialArtist);

            if (potentialArtist.Length > 1 && potentialArtist.Length < 100)
            {
                return potentialArtist;
            }
        }

        return null;
    }

    private string RemoveCommonSuffixes(string artist)
    {
        var suffixes = new[]
        {
            "VEVO",
            " - Topic",
            "Official",
            "Official Video",
            "Official Music Video",
            "Lyrics",
            "HD",
            "4K"
        };

        foreach (var suffix in suffixes)
        {
            if (artist.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                artist = artist[..^suffix.Length].Trim();
            }
        }

        return artist.Trim();
    }

    private string CleanTitle(string title)
    {
        var patterns = new[]
        {
            "(Official Music Video)",
            "(Official Video)",
            "[Official Music Video]",
            "[Official Video]",
            "(Lyrics)",
            "(HD)",
            "(4K)"
        };

        foreach (var pattern in patterns)
        {
            title = title.Replace(pattern, "", StringComparison.OrdinalIgnoreCase);
        }

        return title.Trim();
    }
}