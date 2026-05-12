using Castle.Core.Services;
using Microsoft.JSInterop;

namespace Castle;

public static class KeyboardHandler
{
    private static PlaybackService? _player;
    private static QueueService? _queue;
    private static float _lastVolume = 0.5f;

    public static void Initialize(PlaybackService player, QueueService queue)
    {
        _player = player;
        _queue = queue;
    }

    [JSInvokable]
    public static void HandleKey(string action)
    {
        if (_player == null || _queue == null) return;

        switch (action)
        {
            case "PlayPause":
                _player.PlayPause();
                break;
            case "SeekBack":
                _player.Seek(_player.GetPosition() - 5);
                break;
            case "SeekForward":
                _player.Seek(_player.GetPosition() + 5);
                break;
            case "Previous":
                _player.Previous();
                break;
            case "Next":
                _player.Next();
                break;
            case "VolumeUp":
                _player.SetVolume(Math.Min(1, _player.GetVolume() + 0.05f));
                break;
            case "VolumeDown":
                _player.SetVolume(Math.Max(0, _player.GetVolume() - 0.05f));
                break;
            case "Mute":
                if (_player.GetVolume() > 0)
                {
                    _lastVolume = _player.GetVolume();
                    _player.SetVolume(0);
                }
                else
                {
                    _player.SetVolume(_lastVolume);
                }
                break;
            case "Shuffle":
                _queue.ToggleShuffle();
                break;
            case "Repeat":
                _queue.CycleRepeat();
                break;
        }
    }
}