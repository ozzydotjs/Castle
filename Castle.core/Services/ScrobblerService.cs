using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace Castle.Core.Services;

public class ScrobblerService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private string? _sessionKey;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_sessionKey);

    public ScrobblerService(string apiKey, string apiSecret)
    {
        _apiKey = apiKey;
        _apiSecret = apiSecret;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent", "Castle/1.0");
    }

    public async Task ScrobbleAsync(string artist, string track, string album = "")
    {
        if (!IsAuthenticated) return;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var sig = GetSignature("track.scrobble", artist, track, timestamp);

        var parameters = new Dictionary<string, string>
        {
            ["method"] = "track.scrobble",
            ["artist"] = artist,
            ["track"] = track,
            ["timestamp"] = timestamp,
            ["api_key"] = _apiKey,
            ["sk"] = _sessionKey,
            ["api_sig"] = sig
        };

        await _http.PostAsync("https://ws.audioscrobbler.com/2.0/", new FormUrlEncodedContent(parameters));
    }

    public async Task UpdateNowPlayingAsync(string artist, string track)
    {
        if (!IsAuthenticated) return;

        var sig = GetSignature("track.updateNowPlaying", artist, track, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

        var parameters = new Dictionary<string, string>
        {
            ["method"] = "track.updateNowPlaying",
            ["artist"] = artist,
            ["track"] = track,
            ["api_key"] = _apiKey,
            ["sk"] = _sessionKey,
            ["api_sig"] = sig
        };

        await _http.PostAsync("https://ws.audioscrobbler.com/2.0/", new FormUrlEncodedContent(parameters));
    }

    private string GetSignature(string method, string artist, string track, string timestamp)
    {
        var raw = $"api_key{_apiKey}artist{artist}method{method}sk{_sessionKey}timestamp{timestamp}track{track}{_apiSecret}";
        var md5 = MD5.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(md5).ToLower();
    }
}