using Castle.Core.Models;

namespace Castle.Core.Services;

public enum RepeatMode { Off, RepeatAll, RepeatOne }

public class QueueService
{
    private List<Song> _queue = new();
    private readonly Random _random = new();
    private List<Song> _originalOrder = new();

    public RepeatMode Repeat { get; set; } = RepeatMode.Off;
    public bool IsShuffled { get; private set; }
    public int Count => _queue.Count;

    public Song? CurrentSong { get; private set; }
    public event Action? QueueChanged;

    public void SetQueue(List<Song> songs, int startIndex = 0)
    {
        if (songs == null || songs.Count == 0)
        {
            Clear();
            return;
        }

        _originalOrder = new List<Song>(songs);

        if (IsShuffled)
        {
            _queue = songs.OrderBy(_ => _random.Next()).ToList();
        }
        else
        {
            _queue = new List<Song>(songs);
        }

        // Extract the starting song and remove everything before it
        if (startIndex >= 0 && startIndex < _queue.Count)
        {
            CurrentSong = _queue[startIndex];
            _queue.RemoveRange(0, startIndex + 1);
        }
        else if (_queue.Count > 0)
        {
            CurrentSong = _queue[0];
            _queue.RemoveAt(0);
        }
        else
        {
            CurrentSong = null;
        }

        QueueChanged?.Invoke();
    }

    public Song? Next()
    {
        if (Repeat == RepeatMode.RepeatOne && CurrentSong != null)
        {
            QueueChanged?.Invoke();
            return CurrentSong;
        }

        if (_queue.Count > 0)
        {
            CurrentSong = _queue[0];
            _queue.RemoveAt(0);
            QueueChanged?.Invoke();
            return CurrentSong;
        }

        if (Repeat == RepeatMode.RepeatAll && _originalOrder.Count > 0)
        {
            RefillFromOriginal();

            if (_queue.Count > 0)
            {
                CurrentSong = _queue[0];
                _queue.RemoveAt(0);
                QueueChanged?.Invoke();
                return CurrentSong;
            }
        }

        CurrentSong = null;
        QueueChanged?.Invoke();
        return null;
    }

    public Song? Previous()
    {
        if (CurrentSong != null)
        {
            QueueChanged?.Invoke();
            return CurrentSong;
        }

        return null;
    }

    public Song? SkipTo(int index)
    {
        if (index < 0 || index >= _queue.Count)
            return null;

        var song = _queue[index];
        _queue.RemoveAt(index);
        CurrentSong = song;
        QueueChanged?.Invoke();
        return song;
    }

    public Song? JumpToSong(Song song)
    {
        var index = _queue.FindIndex(s => IsSameSong(s, song));
        if (index >= 0)
            return SkipTo(index);

        // Song not in queue, play it directly
        CurrentSong = song;
        QueueChanged?.Invoke();
        return song;
    }

    public void Add(Song song)
    {
        if (song == null) return;

        _queue.Add(song);
        _originalOrder.Add(song);

        if (CurrentSong == null && _queue.Count > 0)
        {
            CurrentSong = _queue[0];
            _queue.RemoveAt(0);
        }

        QueueChanged?.Invoke();
    }

    public void AddRange(List<Song> songs)
    {
        if (songs == null || songs.Count == 0) return;

        _queue.AddRange(songs);
        _originalOrder.AddRange(songs);

        if (CurrentSong == null && _queue.Count > 0)
        {
            CurrentSong = _queue[0];
            _queue.RemoveAt(0);
        }

        QueueChanged?.Invoke();
    }

    public void PlayNext(Song song)
    {
        if (song == null) return;

        _queue.Insert(0, song);

        var currentIndex = _originalOrder.FindIndex(s => IsSameSong(s, CurrentSong));
        if (currentIndex >= 0)
            _originalOrder.Insert(currentIndex + 1, song);
        else
            _originalOrder.Add(song);

        QueueChanged?.Invoke();
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _queue.Count) return;

        var song = _queue[index];
        _queue.RemoveAt(index);
        _originalOrder.RemoveAll(s => IsSameSong(s, song));

        QueueChanged?.Invoke();
    }

    public void Clear()
    {
        _queue.Clear();
        _originalOrder.Clear();
        CurrentSong = null;
        QueueChanged?.Invoke();
    }

    public void ToggleShuffle()
    {
        IsShuffled = !IsShuffled;

        if (IsShuffled)
        {
            _queue = _queue.OrderBy(_ => _random.Next()).ToList();
        }
        else
        {
            var remaining = new HashSet<string>(_queue.Select(s => s.FilePath ?? ""));
            _queue = _originalOrder.Where(s => remaining.Contains(s.FilePath ?? "")).ToList();
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

    public List<Song> GetUpcoming(int count = 50)
    {
        return _queue.Take(count).ToList();
    }

    public List<Song> GetAll()
    {
        return new List<Song>(_queue);
    }

    public bool HasNext()
    {
        return _queue.Count > 0 ||
               (Repeat == RepeatMode.RepeatAll && _originalOrder.Count > 0) ||
               Repeat == RepeatMode.RepeatOne;
    }

    private void RefillFromOriginal()
    {
        if (IsShuffled)
        {
            _queue = _originalOrder.OrderBy(_ => _random.Next()).ToList();
        }
        else
        {
            _queue = new List<Song>(_originalOrder);
        }
    }

    private bool IsSameSong(Song? a, Song? b)
    {
        if (a == null || b == null) return false;
        return a.Id == b.Id ||
               string.Equals(a.FilePath, b.FilePath, StringComparison.OrdinalIgnoreCase);
    }
}