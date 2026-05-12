using Castle.Core.Interfaces;
using Castle.Core.Models;

namespace Castle.Core.Services;

public class PlaylistService
{
    private readonly IPlaylistRepository _playlistRepo;
    private readonly ISongRepository _songRepo;

    public Playlist? CurrentPlaylist { get; private set; }
    public List<Song> CurrentSongs { get; private set; } = new();

    public event Action? PlaylistChanged;

    public PlaylistService(IPlaylistRepository playlistRepo, ISongRepository songRepo)
    {
        _playlistRepo = playlistRepo;
        _songRepo = songRepo;
    }

    public Playlist CreatePlaylist(string name)
    {
        var cleanName = NormalizePlaylistName(name);

        if (string.IsNullOrWhiteSpace(cleanName))
        {
            cleanName = "New Playlist";
        }

        cleanName = GetUniquePlaylistName(cleanName);

        var playlist = new Playlist
        {
            Id = Guid.NewGuid().ToString(),
            Name = cleanName,
            SongIds = new List<string>(),
            CreatedAt = DateTime.Now
        };

        _playlistRepo.Insert(playlist);

        PlaylistChanged?.Invoke();
        return playlist;
    }

    public void DeletePlaylist(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        _playlistRepo.Delete(id);

        if (CurrentPlaylist != null &&
            string.Equals(CurrentPlaylist.Id, id, StringComparison.OrdinalIgnoreCase))
        {
            CurrentPlaylist = null;
            CurrentSongs = new List<Song>();
        }

        PlaylistChanged?.Invoke();
    }

    public List<Playlist> GetAllPlaylists()
    {
        return _playlistRepo
            .GetAll()
            .OrderBy(p => p.CreatedAt)
            .ToList();
    }

    public Playlist? GetPlaylistById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return _playlistRepo.GetById(id);
    }

    public void LoadPlaylist(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var playlist = _playlistRepo.GetById(id);

        if (playlist == null)
        {
            return;
        }

        CurrentPlaylist = playlist;
        CurrentSongs = BuildSongListFromPlaylist(playlist, cleanMissingSongs: true);

        PlaylistChanged?.Invoke();
    }

    public void AddToPlaylist(string playlistId, string songId)
    {
        if (string.IsNullOrWhiteSpace(playlistId) || string.IsNullOrWhiteSpace(songId))
        {
            return;
        }

        var playlist = _playlistRepo.GetById(playlistId);

        if (playlist == null)
        {
            return;
        }

        var song = _songRepo.GetById(songId);

        if (song == null)
        {
            return;
        }

        var alreadyExists = playlist.SongIds.Any(id =>
            string.Equals(id, songId, StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
        {
            return;
        }

        playlist.SongIds.Add(songId);
        _playlistRepo.Update(playlist);

        RefreshCurrentPlaylistIfNeeded(playlistId);

        PlaylistChanged?.Invoke();
    }

    public void RemoveFromPlaylist(string playlistId, string songId)
    {
        if (string.IsNullOrWhiteSpace(playlistId) || string.IsNullOrWhiteSpace(songId))
        {
            return;
        }

        var playlist = _playlistRepo.GetById(playlistId);

        if (playlist == null)
        {
            return;
        }

        playlist.SongIds.RemoveAll(id =>
            string.Equals(id, songId, StringComparison.OrdinalIgnoreCase));

        _playlistRepo.Update(playlist);

        RefreshCurrentPlaylistIfNeeded(playlistId);

        PlaylistChanged?.Invoke();
    }

    public bool PlaylistContainsSong(string playlistId, string songId)
    {
        if (string.IsNullOrWhiteSpace(playlistId) || string.IsNullOrWhiteSpace(songId))
        {
            return false;
        }

        var playlist = _playlistRepo.GetById(playlistId);

        if (playlist == null)
        {
            return false;
        }

        return playlist.SongIds.Any(id =>
            string.Equals(id, songId, StringComparison.OrdinalIgnoreCase));
    }

    public void RenamePlaylist(string playlistId, string newName)
    {
        if (string.IsNullOrWhiteSpace(playlistId))
        {
            return;
        }

        var playlist = _playlistRepo.GetById(playlistId);

        if (playlist == null)
        {
            return;
        }

        var cleanName = NormalizePlaylistName(newName);

        if (string.IsNullOrWhiteSpace(cleanName))
        {
            return;
        }

        cleanName = GetUniquePlaylistName(cleanName, playlistId);

        playlist.Name = cleanName;
        _playlistRepo.Update(playlist);

        if (CurrentPlaylist != null &&
            string.Equals(CurrentPlaylist.Id, playlistId, StringComparison.OrdinalIgnoreCase))
        {
            CurrentPlaylist = playlist;
        }

        PlaylistChanged?.Invoke();
    }

    public void ClearCurrentPlaylist()
    {
        CurrentPlaylist = null;
        CurrentSongs = new List<Song>();

        PlaylistChanged?.Invoke();
    }

    private List<Song> BuildSongListFromPlaylist(Playlist playlist, bool cleanMissingSongs)
    {
        var songs = new List<Song>();
        var validSongIds = new List<string>();

        foreach (var songId in playlist.SongIds)
        {
            if (string.IsNullOrWhiteSpace(songId))
            {
                continue;
            }

            var song = _songRepo.GetById(songId);

            if (song == null)
            {
                continue;
            }

            songs.Add(song);
            validSongIds.Add(songId);
        }

        if (cleanMissingSongs && validSongIds.Count != playlist.SongIds.Count)
        {
            playlist.SongIds = validSongIds;
            _playlistRepo.Update(playlist);
        }

        return songs;
    }

    private void RefreshCurrentPlaylistIfNeeded(string playlistId)
    {
        if (CurrentPlaylist == null)
        {
            return;
        }

        if (!string.Equals(CurrentPlaylist.Id, playlistId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var updatedPlaylist = _playlistRepo.GetById(playlistId);

        if (updatedPlaylist == null)
        {
            CurrentPlaylist = null;
            CurrentSongs = new List<Song>();
            return;
        }

        CurrentPlaylist = updatedPlaylist;
        CurrentSongs = BuildSongListFromPlaylist(updatedPlaylist, cleanMissingSongs: true);
    }

    private string NormalizePlaylistName(string name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? string.Empty
            : name.Trim();
    }

    private string GetUniquePlaylistName(string baseName, string? ignorePlaylistId = null)
    {
        var playlists = _playlistRepo.GetAll();

        bool NameExists(string name)
        {
            return playlists.Any(p =>
                !string.Equals(p.Id, ignorePlaylistId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        if (!NameExists(baseName))
        {
            return baseName;
        }

        var counter = 2;
        var newName = $"{baseName} {counter}";

        while (NameExists(newName))
        {
            counter++;
            newName = $"{baseName} {counter}";
        }

        return newName;
    }
}