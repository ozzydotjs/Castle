using Castle.Core.Models;

namespace Castle.Core.Services;

public enum RepeatMode { Off, RepeatAll, RepeatOne }

public class QueueService
{
    private List<Song> _originalQueue = new();
    private List<Song> _shuffledQueue = new();
    private int _currentIndex = -1;
    private readonly Random _random = new();

    public RepeatMode Repeat { get; set; } = RepeatMode.Off;
    public bool IsShuffled { get; private set; }
    public int Count => _shuffledQueue.Count;
    public int CurrentIndex => _currentIndex;

    public Song? CurrentSong =>
        _currentIndex >= 0 && _currentIndex < _shuffledQueue.Count
            ? _shuffledQueue[_currentIndex]
            : null;

    public event Action? QueueChanged;

    public void SetQueue(List<Song> songs)
    {
        _originalQueue = new List<Song>(songs);
        _shuffledQueue = new List<Song>(songs);
        _currentIndex = songs.Count > 0 ? 0 : -1;

        QueueChanged?.Invoke();
    }

    public Song? Next()
    {
        if (_shuffledQueue.Count == 0)
        {
            return null;
        }

        if (Repeat == RepeatMode.RepeatOne && CurrentSong != null)
        {
            QueueChanged?.Invoke();
            return CurrentSong;
        }

        _currentIndex++;

        if (_currentIndex >= _shuffledQueue.Count)
        {
            if (Repeat == RepeatMode.RepeatAll)
            {
                _currentIndex = 0;
            }
            else
            {
                _currentIndex = _shuffledQueue.Count - 1;
                QueueChanged?.Invoke();
                return null;
            }
        }

        QueueChanged?.Invoke();
        return CurrentSong;
    }

    public Song? Previous()
    {
        if (_shuffledQueue.Count == 0)
        {
            return null;
        }

        _currentIndex--;

        if (_currentIndex < 0)
        {
            _currentIndex = Repeat == RepeatMode.RepeatAll
                ? _shuffledQueue.Count - 1
                : 0;
        }

        QueueChanged?.Invoke();
        return CurrentSong;
    }

    public Song? JumpTo(int index)
    {
        if (index < 0 || index >= _shuffledQueue.Count)
        {
            return null;
        }

        _currentIndex = index;

        QueueChanged?.Invoke();
        return CurrentSong;
    }

    public Song? JumpToSong(Song song)
    {
        var index = FindSongIndex(_shuffledQueue, song);
        return JumpTo(index);
    }

    public List<Song> GetUpcoming(int count = 5)
    {
        if (_currentIndex < 0 || _currentIndex >= _shuffledQueue.Count - 1)
        {
            return new List<Song>();
        }

        return _shuffledQueue
            .Skip(_currentIndex + 1)
            .Take(count)
            .ToList();
    }

    public void Add(Song song)
    {
        _originalQueue.Add(song);

        if (IsShuffled)
        {
            _shuffledQueue.Add(song);
        }
        else
        {
            _shuffledQueue = new List<Song>(_originalQueue);
            _currentIndex = FindSongIndex(_shuffledQueue, CurrentSong);
        }

        if (_currentIndex < 0 && _shuffledQueue.Count > 0)
        {
            _currentIndex = 0;
        }

        QueueChanged?.Invoke();
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _shuffledQueue.Count)
        {
            return;
        }

        var currentSong = CurrentSong;
        var songToRemove = _shuffledQueue[index];

        _shuffledQueue.RemoveAt(index);

        _originalQueue.RemoveAll(song =>
            IsSameSong(song, songToRemove));

        if (_shuffledQueue.Count == 0)
        {
            _currentIndex = -1;
        }
        else if (currentSong != null)
        {
            var newIndex = FindSongIndex(_shuffledQueue, currentSong);

            if (newIndex >= 0)
            {
                _currentIndex = newIndex;
            }
            else
            {
                _currentIndex = Math.Min(index, _shuffledQueue.Count - 1);
            }
        }
        else
        {
            _currentIndex = Math.Min(index, _shuffledQueue.Count - 1);
        }

        QueueChanged?.Invoke();
    }

    public void ToggleShuffle()
    {
        IsShuffled = !IsShuffled;

        var current = CurrentSong;

        _shuffledQueue = IsShuffled
            ? _originalQueue.OrderBy(_ => _random.Next()).ToList()
            : new List<Song>(_originalQueue);

        if (current != null)
        {
            _currentIndex = FindSongIndex(_shuffledQueue, current);
        }

        if (_currentIndex < 0 && _shuffledQueue.Count > 0)
        {
            _currentIndex = 0;
        }

        if (_shuffledQueue.Count == 0)
        {
            _currentIndex = -1;
        }

        QueueChanged?.Invoke();
    }

    public RepeatMode CycleRepeat()
    {
        Repeat = Repeat switch
        {
            RepeatMode.Off => RepeatMode.RepeatAll,
            RepeatMode.RepeatAll => RepeatMode.RepeatOne,
            RepeatMode.RepeatOne => RepeatMode.Off,
            _ => RepeatMode.Off
        };

        QueueChanged?.Invoke();
        return Repeat;
    }

    public List<Song> GetAll()
    {
        return new List<Song>(_shuffledQueue);
    }

    private int FindSongIndex(List<Song> songs, Song? target)
    {
        if (target == null)
        {
            return -1;
        }

        return songs.FindIndex(song => IsSameSong(song, target));
    }

    private bool IsSameSong(Song a, Song b)
    {
        return a.Id == b.Id ||
               string.Equals(a.FilePath, b.FilePath, StringComparison.OrdinalIgnoreCase);
    }
}