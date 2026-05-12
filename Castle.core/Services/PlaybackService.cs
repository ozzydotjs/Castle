using Castle.Core.Interfaces;
using Castle.Core.Models;

namespace Castle.Core.Services;

public class PlaybackService
{
    private readonly IAudioEngine _audio;
    private readonly QueueService _queue;
    private readonly ISongRepository _repository;
    private readonly LyricsService _lyricsService;
    private readonly IRecentlyPlayedRepository _recentlyPlayedRepo;
    private readonly ScrobblerService? _scrobblerService;

    private CancellationTokenSource? _lyricsCts;
    private float _savedVolume = 1.0f;
    private System.Timers.Timer? _sleepTimer;
    private DateTime _sleepEndTime;

    public PlaybackService(
        IAudioEngine audio,
        QueueService queue,
        ISongRepository repository,
        LyricsService lyricsService,
        IRecentlyPlayedRepository recentlyPlayedRepo,
        ScrobblerService? scrobblerService = null)
    {
        _audio = audio;
        _queue = queue;
        _repository = repository;
        _lyricsService = lyricsService;
        _recentlyPlayedRepo = recentlyPlayedRepo;
        _scrobblerService = scrobblerService;

        _audio.SongEnded += OnSongEnded;
    }

    public QueueService Queue => _queue;
    public bool IsPlaying => _audio.IsPlaying;

    public event Action? StateChanged;

    public List<LyricLine>? CurrentLyrics { get; private set; }
    public event Action? LyricsLoaded;

    public bool SleepTimerActive => _sleepTimer != null;
    public TimeSpan SleepTimeRemaining => SleepTimerActive ? _sleepEndTime - DateTime.Now : TimeSpan.Zero;

    public event Action? SleepTimerChanged;
    public event Action? SleepTimerEnded;

    public void PlaySong(Song song)
    {
        var currentQueue = _queue.GetAll();
        var index = FindSongIndex(currentQueue, song);

        if (index >= 0)
        {
            var selected = _queue.JumpTo(index);
            if (selected != null)
            {
                PlaySelectedSong(selected);
                return;
            }
        }

        var allSongs = _repository.GetAll();
        var libraryIndex = FindSongIndex(allSongs, song);

        if (libraryIndex >= 0)
        {
            PlayQueue(allSongs, libraryIndex);
            return;
        }

        _queue.SetQueue(new List<Song> { song });
        _queue.JumpTo(0);
        PlaySelectedSong(song);
    }

    public void PlayQueue(List<Song> songs, int startIndex = 0)
    {
        if (songs.Count == 0)
        {
            Stop();
            return;
        }

        if (startIndex < 0)
        {
            startIndex = 0;
        }

        if (startIndex >= songs.Count)
        {
            startIndex = songs.Count - 1;
        }

        _queue.SetQueue(songs);

        var song = _queue.JumpTo(startIndex);

        if (song != null)
        {
            PlaySelectedSong(song);
        }
    }

    public void PlayPause()
    {
        if (_audio.IsPlaying)
        {
            _audio.Pause();
            StateChanged?.Invoke();
            return;
        }

        if (_queue.CurrentSong != null)
        {
            PlaySelectedSong(_queue.CurrentSong);
            return;
        }

        var songs = _repository.GetAll();

        if (songs.Count > 0)
        {
            PlayQueue(songs, 0);
        }
    }

    public void Stop()
    {
        _audio.Stop();
        StateChanged?.Invoke();
    }

    public void Next()
    {
        var nextSong = _queue.Next();

        if (nextSong != null)
        {
            PlaySelectedSong(nextSong);
        }
        else
        {
            Stop();
        }
    }

    public void Previous()
    {
        if (_audio.Position > 3)
        {
            _audio.Position = 0;
            StateChanged?.Invoke();
            return;
        }

        var previousSong = _queue.Previous();

        if (previousSong != null)
        {
            PlaySelectedSong(previousSong);
        }
    }

    public void ToggleFavorite()
    {
        var song = _queue.CurrentSong;

        if (song == null)
        {
            return;
        }

        var dbSong = _repository.GetByFilePath(song.FilePath);

        if (dbSong != null)
        {
            dbSong.IsFavorite = !dbSong.IsFavorite;
            _repository.Update(dbSong);
            StateChanged?.Invoke();
        }
    }

    public bool IsCurrentSongFavorite()
    {
        var song = _queue.CurrentSong;

        if (song == null)
        {
            return false;
        }

        return _repository.GetByFilePath(song.FilePath)?.IsFavorite ?? false;
    }

    public void Seek(double seconds)
    {
        if (seconds < 0)
        {
            seconds = 0;
        }

        var duration = _audio.Duration;

        if (duration > 0 && seconds > duration)
        {
            seconds = duration;
        }

        _audio.Position = seconds;
        StateChanged?.Invoke();
    }

    public double GetPosition()
    {
        return _audio.Position;
    }

    public double GetDuration()
    {
        return _audio.Duration;
    }

    public void SetVolume(float volume)
    {
        volume = Math.Clamp(volume, 0f, 1f);

        _audio.Volume = volume;
        _savedVolume = volume;

        StateChanged?.Invoke();
    }

    public float GetVolume()
    {
        return _audio.Volume;
    }

    public void SaveVolume()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Castle",
            "volume.txt");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, _savedVolume.ToString("F2"));
    }

    public void EnableEQ()
    {
        if (_audio is AudioEngine ae)
        {
            ae.EnableEqualizer();
        }
    }

    public void SetEqBand(int band, float gain)
    {
        if (_audio is AudioEngine ae)
        {
            ae.SetEqBand(band, gain);
        }
    }

    public void EnableCompressor()
    {
        if (_audio is AudioEngine ae)
        {
            ae.EnableCompressor();
        }
    }

    public void SetSleepTimer(int minutes)
    {
        _sleepTimer?.Stop();
        _sleepTimer?.Dispose();

        _sleepTimer = new System.Timers.Timer(1000);
        _sleepEndTime = DateTime.Now.AddMinutes(minutes);

        _sleepTimer.Elapsed += (_, _) =>
        {
            if (DateTime.Now >= _sleepEndTime)
            {
                _sleepTimer.Stop();
                _sleepTimer.Dispose();
                _sleepTimer = null;

                Stop();
                SleepTimerEnded?.Invoke();
            }

            SleepTimerChanged?.Invoke();
        };

        _sleepTimer.AutoReset = true;
        _sleepTimer.Start();

        SleepTimerChanged?.Invoke();
    }

    public void CancelSleepTimer()
    {
        _sleepTimer?.Stop();
        _sleepTimer?.Dispose();
        _sleepTimer = null;

        SleepTimerChanged?.Invoke();
    }

    private void PlaySelectedSong(Song song)
    {
        _audio.Initialize();
        _audio.Play(song.FilePath);

        PreloadNextTrack();
        TrackPlay(song);
        ScrobbleUpdate(song);

        StateChanged?.Invoke();

        PreloadLyrics(song.Title, song.Artist);
    }

    private int FindSongIndex(List<Song> songs, Song target)
    {
        return songs.FindIndex(song =>
            song.Id == target.Id ||
            string.Equals(song.FilePath, target.FilePath, StringComparison.OrdinalIgnoreCase));
    }

    private void TrackPlay(Song song)
    {
        try
        {
            _recentlyPlayedRepo.Add(new RecentlyPlayedEntry
            {
                SongId = song.Id,
                Title = song.Title,
                Artist = song.Artist,
                FilePath = song.FilePath,
                CoverArtPath = song.CoverArtPath,
                Duration = song.Duration
            });
        }
        catch
        {
        }
    }

    private void ScrobbleUpdate(Song song)
    {
        try
        {
            _scrobblerService?.UpdateNowPlayingAsync(song.Artist, song.Title);
        }
        catch
        {
        }
    }

    private void PreloadNextTrack()
    {
        var allSongs = _queue.GetAll();
        var currentIndex = _queue.CurrentIndex;

        if (currentIndex >= 0 &&
            currentIndex < allSongs.Count - 1 &&
            _audio is AudioEngine ae)
        {
            ae.PreloadNext(allSongs[currentIndex + 1].FilePath);
        }
    }

    private void OnSongEnded()
    {
        try
        {
            var nextSong = _queue.Next();

            if (nextSong != null)
            {
                PlaySelectedSong(nextSong);
            }
            else
            {
                Stop();
            }
        }
        catch
        {
        }
    }

    private void PreloadLyrics(string title, string artist)
    {
        _lyricsCts?.Cancel();
        _lyricsCts = new CancellationTokenSource();

        var token = _lyricsCts.Token;

        CurrentLyrics = null;

        Task.Run(async () =>
        {
            try
            {
                var lyrics = await _lyricsService.GetLyricsAsync(title, artist);

                if (!token.IsCancellationRequested)
                {
                    CurrentLyrics = lyrics;
                    LyricsLoaded?.Invoke();
                }
            }
            catch
            {
            }
        }, token);
    }
}