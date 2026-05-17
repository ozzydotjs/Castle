using Castle.Core.Interfaces;
using ManagedBass;
using ManagedBass.Fx;

namespace Castle.Core.Services;

public class AudioEngine : IAudioEngine, IDisposable
{
    private int _currentHandle;
    private bool _initialized;
    private SyncProcedure? _endSync;
    private float _cachedVolume = 1.0f;
    private int _eqHandle;
    private bool _eqEnabled;
    private float[] _eqBands = new float[10];
    private int _compressorHandle;
    private bool _compressorEnabled;
    private string? _nextFilePath;
    private readonly object _fftLock = new();

    public event Action? SongEnded;

    public bool Initialize()
    {
        if (_initialized)
            return true;

        _initialized = Bass.Init();

        if (_initialized)
        {
            Bass.GlobalStreamVolume = 10000;

            var volPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Castle", "volume.txt");
            if (File.Exists(volPath) && float.TryParse(File.ReadAllText(volPath), out var savedVol))
                _cachedVolume = savedVol;
            else
                _cachedVolume = 1.0f;
        }

        return _initialized;
    }

    public void Play(string filePath)
    {
        if (!_initialized)
            Initialize();

        float currentVol = Volume;
        bool wasEqEnabled = _eqEnabled;
        float[] savedBands = new float[10];
        Array.Copy(_eqBands, savedBands, 10);

        Stop();

        _currentHandle = Bass.CreateStream(filePath);

        if (_currentHandle != 0)
        {
            _endSync = new SyncProcedure(OnSongEnded);
            Bass.ChannelSetSync(_currentHandle, SyncFlags.End, 0, _endSync);
            Volume = currentVol;

            // Re-apply EQ if it was enabled before
            if (wasEqEnabled)
            {
                EnableEqualizerInternal();
                for (int i = 0; i < 10; i++)
                {
                    SetEqBandInternal(i, savedBands[i]);
                }
            }

            Bass.ChannelPlay(_currentHandle);
        }
    }

    public void PreloadNext(string filePath)
    {
        _nextFilePath = filePath;
    }

    private void OnSongEnded(int handle, int channel, int data, IntPtr user)
    {
        if (!string.IsNullOrWhiteSpace(_nextFilePath))
        {
            Stop();
            bool wasEqEnabled = _eqEnabled;
            float[] savedBands = new float[10];
            Array.Copy(_eqBands, savedBands, 10);

            _currentHandle = Bass.CreateStream(_nextFilePath);
            if (_currentHandle != 0)
            {
                _endSync = new SyncProcedure(OnSongEnded);
                Bass.ChannelSetSync(_currentHandle, SyncFlags.End, 0, _endSync);

                if (wasEqEnabled)
                {
                    EnableEqualizerInternal();
                    for (int i = 0; i < 10; i++)
                    {
                        SetEqBandInternal(i, savedBands[i]);
                    }
                }

                Bass.ChannelPlay(_currentHandle);
            }
            _nextFilePath = null;
        }

        SongEnded?.Invoke();
    }

    public void Stop()
    {
        _nextFilePath = null;

        if (_currentHandle != 0)
        {
            Bass.ChannelStop(_currentHandle);
            Bass.StreamFree(_currentHandle);
            _currentHandle = 0;
            _eqHandle = 0;
            _compressorEnabled = false;
        }
    }

    public void Pause()
    {
        if (_currentHandle != 0)
        {
            bool playing = Bass.ChannelIsActive(_currentHandle) == PlaybackState.Playing;
            if (playing) Bass.ChannelPause(_currentHandle);
            else Bass.ChannelPlay(_currentHandle);
        }
    }

    public bool IsPlaying => _currentHandle != 0 && Bass.ChannelIsActive(_currentHandle) == PlaybackState.Playing;

    public double Position
    {
        get
        {
            if (_currentHandle == 0) return 0;
            return Bass.ChannelBytes2Seconds(_currentHandle, Bass.ChannelGetPosition(_currentHandle));
        }
        set
        {
            if (_currentHandle == 0) return;
            Bass.ChannelSetPosition(_currentHandle, Bass.ChannelSeconds2Bytes(_currentHandle, value));
        }
    }

    public double Duration
    {
        get
        {
            if (_currentHandle == 0) return 0;
            return Bass.ChannelBytes2Seconds(_currentHandle, Bass.ChannelGetLength(_currentHandle));
        }
    }

    public float Volume
    {
        get
        {
            if (_currentHandle == 0) return _cachedVolume;
            float vol = 0f;
            Bass.ChannelGetAttribute(_currentHandle, ChannelAttribute.Volume, out vol);
            _cachedVolume = vol;
            return vol;
        }
        set
        {
            float clamped = Math.Clamp(value, 0f, 1f);
            _cachedVolume = clamped;
            if (_currentHandle != 0)
                Bass.ChannelSetAttribute(_currentHandle, ChannelAttribute.Volume, clamped);
            else
                Bass.GlobalStreamVolume = (int)(clamped * 10000);
        }
    }

    // ========== FFT DATA ==========
    public float[] GetFFTData(int bins = 64)
    {
        var output = new float[bins];
        try
        {
            if (_currentHandle == 0) return output;
            if (Bass.ChannelIsActive(_currentHandle) != PlaybackState.Playing) return output;

            var fftSize = 512;
            var data = new float[fftSize];
            var flags = unchecked((int)(0x40000000 | 0x80000004)); // BASS_DATA_FLOAT | BASS_DATA_FFT1024
            var result = Bass.ChannelGetData(_currentHandle, data, flags);
            if (result <= 0) return output;

            var ratio = (float)fftSize / bins;
            for (int i = 0; i < bins; i++)
            {
                var start = (int)(i * ratio);
                var end = Math.Min((int)((i + 1) * ratio), fftSize);
                var sum = 0f;
                var count = 0;
                for (int j = start; j < end; j++)
                {
                    sum += Math.Abs(data[j]);
                    count++;
                }
                output[i] = count > 0 ? sum / count : 0f;
            }
        }
        catch { }
        return output;
    }

    // ========== EQUALIZER ==========
    public void EnableEqualizer()
    {
        EnableEqualizerInternal();
    }

    private void EnableEqualizerInternal()
    {
        if (_currentHandle == 0 || _eqEnabled) return;
        _eqHandle = Bass.ChannelSetFX(_currentHandle, EffectType.PeakEQ, 1);
        _eqEnabled = true;
    }

    public void SetEqBand(int band, float gain)
    {
        if (band < 0 || band >= 10) return;
        _eqBands[band] = gain;
        SetEqBandInternal(band, gain);
    }

    private void SetEqBandInternal(int band, float gain)
    {
        if (!_eqEnabled || band < 0 || band >= 10) return;
        var peq = new PeakEQParameters
        {
            fBandwidth = 1.0f,
            fCenter = GetCenterFrequency(band),
            fGain = gain,
            lChannel = 0
        };
        Bass.FXSetParameters(_eqHandle, peq);
    }

    private float GetCenterFrequency(int band) => band switch
    {
        0 => 32f,
        1 => 64f,
        2 => 125f,
        3 => 250f,
        4 => 500f,
        5 => 1000f,
        6 => 2000f,
        7 => 4000f,
        8 => 8000f,
        9 => 16000f,
        _ => 1000f
    };

    // ========== COMPRESSOR ==========
    public void EnableCompressor()
    {
        if (_currentHandle == 0 || _compressorEnabled) return;
        _compressorHandle = Bass.ChannelSetFX(_currentHandle, EffectType.Compressor, 1);
        var comp = new CompressorParameters
        {
            fThreshold = -20f,
            fRatio = 4f,
            fAttack = 10f,
            fRelease = 200f,
            fGain = 6f
        };
        Bass.FXSetParameters(_compressorHandle, comp);
        _compressorEnabled = true;
    }

    public void Dispose()
    {
        Stop();
        Bass.Free();
    }
}