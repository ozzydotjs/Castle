using Castle.Core.Interfaces;
using Castle.Core.Services;
using Castle.Data;
using Castle.Data.Repositories;
using Castle.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using InfiniLore.Lucide;

namespace Castle;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "castle.db");
        var database = new Database(dbPath);
        builder.Services.AddSingleton(database);
        builder.Services.AddSingleton<ISongRepository, SongRepository>();
        builder.Services.AddSingleton<IPlaylistRepository, PlaylistRepository>();
        builder.Services.AddSingleton<IBlacklistRepository, BlacklistRepository>();
        builder.Services.AddSingleton<IRecentlyPlayedRepository, RecentlyPlayedRepository>();
        var lastFmApiKey = Preferences.Get("lastfm_api_key", string.Empty);
        var lastFmApiSecret = Preferences.Get("lastfm_api_secret", string.Empty);
        builder.Services.AddSingleton(new LastFmService(lastFmApiKey));
        builder.Services.AddSingleton(new ScrobblerService(lastFmApiKey, lastFmApiSecret));
        builder.Services.AddSingleton<MetadataService>();
        builder.Services.AddSingleton<IAudioEngine, AudioEngine>();
        builder.Services.AddSingleton<ILibraryScanner, LibraryScanner>();
        builder.Services.AddSingleton<LyricsService>();
        builder.Services.AddSingleton<QueueService>();
        builder.Services.AddSingleton<PlaybackService>();
        builder.Services.AddSingleton<PlaylistService>();
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<SearchService>();
        builder.Services.AddSingleton<DownloadService>();
        builder.Services.AddSingleton<DownloadQueueService>();
        builder.Services.AddSingleton<RecommendationService>();
        builder.Services.AddSingleton<DiscoverService>();
        builder.Services.AddSingleton<PlaylistImportService>();
        builder.Services.AddSingleton(sp => new StreamSnipper(sp.GetRequiredService<PlaybackService>(), sp.GetRequiredService<QueueService>()));
        builder.Services.AddLucideIcons();
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<ThemeService>();
        builder.Services.AddSingleton<UpdateService>();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        var player = app.Services.GetRequiredService<PlaybackService>();
        var queue = app.Services.GetRequiredService<QueueService>();
        KeyboardHandler.Initialize(player, queue);

        return app;
    }
}