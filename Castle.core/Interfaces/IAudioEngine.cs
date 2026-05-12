namespace Castle.Core.Interfaces;

public interface IAudioEngine
{
    bool Initialize();
    void Play(string filePath);
    void Stop();
    void Pause();
    bool IsPlaying { get; }
    double Position { get; set; }
    double Duration { get; }
    float Volume { get; set; }
    event Action? SongEnded;
}