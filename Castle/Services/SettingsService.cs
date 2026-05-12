namespace Castle.Services;

public class SettingsService
{
    private const string MusicFolderKey = "music_folder_path";
    private const string HasScannedKey = "has_scanned";

    public string? MusicFolderPath
    {
        get => Preferences.Get(MusicFolderKey, null);
        set => Preferences.Set(MusicFolderKey, value);
    }

    public bool HasScanned
    {
        get => Preferences.Get(HasScannedKey, false);
        set => Preferences.Set(HasScannedKey, value);
    }

    public async Task<string?> PickMusicFolderAsync()
    {
        try
        {
            var fileResult = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select any music file in your main music folder",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".mp3", ".flac", ".wav", ".m4a", ".ogg" } }
                })
            });

            if (fileResult != null)
            {
                var folderPath = Path.GetDirectoryName(fileResult.FullPath);
                MusicFolderPath = folderPath;
                HasScanned = false;
                return folderPath;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Folder picker error: {ex.Message}");
        }
        return null;
    }
}