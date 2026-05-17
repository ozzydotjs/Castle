using System.Text.Json;

namespace Castle.Services;

public class ThemeDefinition
{
    public string Name { get; set; } = "Castle Dark";
    public string Description { get; set; } = "";
    public string? Wallpaper { get; set; }

    public string BgPrimary { get; set; } = "#0D0D0D";
    public string BgSecondary { get; set; } = "#141414";
    public string BgSurface { get; set; } = "#1A1A1A";
    public string Border { get; set; } = "#2A2A2A";
    public string TextPrimary { get; set; } = "#FFFFFF";
    public string TextSecondary { get; set; } = "#999999";
    public string Accent { get; set; } = "#CC0000";
    public string AccentHover { get; set; } = "#FF0000";
    public string AccentSubtle { get; set; } = "#1A0000";
    public string AccentActive { get; set; } = "#FF0000";
    public string IconDefault { get; set; } = "#BBBBBB";
    public string ProgressFill { get; set; } = "#CC0000";
    public string FavoriteColor { get; set; } = "#FF0000";
    public string PlayerBarBg { get; set; } = "#141414";
    public string SidebarBg { get; set; } = "#0A0A0A";

    public bool IsRgb { get; set; }
    public double RgbSpeed { get; set; } = 3;
    public double RgbBrightness { get; set; } = 1.0;
    public double RgbSaturation { get; set; } = 1.0;
    public string RgbPattern { get; set; } = "wave";
}

public class ThemeService
{
    private const string ThemeKey = "castle_theme";

    private static readonly JsonSerializerOptions ThemeJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public ThemeDefinition CurrentTheme { get; private set; }
    public List<ThemeDefinition> BuiltInThemes { get; }

    public event Action? ThemeChanged;

    public ThemeService()
    {
        BuiltInThemes = new List<ThemeDefinition>
        {
            // Sierra
            new()
            {
                Name = "Sierra",
                Description = "i dedicate this to you, my love ^^",
                BgPrimary = "#2E2B28",
                BgSecondary = "#3A3632",
                BgSurface = "#4A4A3C",
                Border = "#DCD1C0",
                TextPrimary = "#F5EFE2",
                TextSecondary = "#C1A179",
                Accent = "#8B5A3C",
                AccentHover = "#A06E4A",
                AccentSubtle = "#3A3528",
                AccentActive = "#7A8B5E",
                IconDefault = "#C1A179",
                ProgressFill = "#8B5A3C",
                FavoriteColor = "#7A8B5E",
                PlayerBarBg = "#3A3632",
                SidebarBg = "#25221D"
            },

            // Castle Dark
            new()
            {
                Name = "Castle Dark",
                Description = "In certain extreme situations, the law is inadequate.",
                Wallpaper = "warzone-bg.png",

                BgPrimary = "rgba(13,13,13,0.72)",
                BgSecondary = "rgba(20,20,20,0.80)",
                BgSurface = "rgba(26,26,26,0.84)",
                Border = "rgba(255,255,255,0.10)",

                TextPrimary = "#FFFFFF",
                TextSecondary = "#999999",

                Accent = "#CC0000",
                AccentHover = "#FF0000",
                AccentSubtle = "rgba(26,0,0,0.72)",
                AccentActive = "#FF0000",

                IconDefault = "#BBBBBB",
                ProgressFill = "#CC0000",
                FavoriteColor = "#FF0000",

                PlayerBarBg = "rgba(20,20,20,0.88)",
                SidebarBg = "rgba(10,10,10,0.86)"
            },

            // WinterZ
            new()
            {
                Name = "WinterZ",
                Description = "my twin till i die fr.",
                BgPrimary = "#071D33",
                BgSecondary = "#0E2847",
                BgSurface = "#163A5C",
                Border = "#4E5A6A",
                TextPrimary = "#D4DADD",
                TextSecondary = "#B8C3C5",
                Accent = "#7489A7",
                AccentHover = "#8FA3BD",
                AccentSubtle = "#0E2847",
                AccentActive = "#D4DADD",
                IconDefault = "#B8C3C5",
                ProgressFill = "#7489A7",
                FavoriteColor = "#D4DADD",
                PlayerBarBg = "#0A2440",
                SidebarBg = "#040F1F"
            },

            // PinkMeth
            new()
            {
                Name = "PinkMeth",
                Description = "ang \"S\" sa \"SHANE\" ay \"Shabu\"",

                BgPrimary = "#FFCAD8",
                BgSecondary = "#FFB3C8",
                BgSurface = "#FF8FB3",

                Border = "#FF3F8E",

                TextPrimary = "#2A0714",
                TextSecondary = "#7A1438",

                Accent = "#FF007F",
                AccentHover = "#FF3399",
                AccentSubtle = "#FFD6E8",
                AccentActive = "#B000FF",

                IconDefault = "#C2185B",
                ProgressFill = "#FF007F",
                FavoriteColor = "#B000FF",

                PlayerBarBg = "#FF9DBC",
                SidebarBg = "#FF7AAD"
            },

            // Landog (updated - darker/slate)
            new()
            {
                Name = "Landog",
                Description = "cant handle a shade lighter than black",
                BgPrimary = "#1E1C26",
                BgSecondary = "#24222E",
                BgSurface = "#2E2C3A",
                Border = "#3D3B4A",
                TextPrimary = "#E8E9ED",
                TextSecondary = "#8E8C9A",
                Accent = "#5E5A70",
                AccentHover = "#736E85",
                AccentSubtle = "#2E2C3A",
                AccentActive = "#9491A5",
                IconDefault = "#8E8C9A",
                ProgressFill = "#5E5A70",
                FavoriteColor = "#736E85",
                PlayerBarBg = "#24222E",
                SidebarBg = "#15131C"
            },

            // Teshido
            new()
            {
                Name = "Teshido",
                Description = "NATHAN DESTROYER THEME",
                BgPrimary = "#2F2E2F",
                BgSecondary = "#353436",
                BgSurface = "#454346",
                Border = "#555456",
                TextPrimary = "#F7F6F9",
                TextSecondary = "#A1A0A3",
                Accent = "#6F5CFF",
                AccentHover = "#8A7BFF",
                AccentSubtle = "#1A1530",
                AccentActive = "#9D5153",
                IconDefault = "#A1A0A3",
                ProgressFill = "#6F5CFF",
                FavoriteColor = "#9D5153",
                PlayerBarBg = "#353436",
                SidebarBg = "#252425"
            },

            // Ally
            new()
            {
                Name = "Ally",
                Description = "always on your side.",
                Wallpaper = "cat-bg.png",

                BgPrimary = "rgba(255,245,238,0.72)",
                BgSecondary = "rgba(255,212,184,0.78)",
                BgSurface = "rgba(255,226,204,0.82)",
                Border = "rgba(247,164,107,0.65)",

                TextPrimary = "#472114",
                TextSecondary = "#8F5B40",

                Accent = "#FF6600",
                AccentHover = "#FF8126",
                AccentSubtle = "rgba(255,231,214,0.75)",
                AccentActive = "#FFB56B",

                IconDefault = "#BF7750",
                ProgressFill = "#FF6600",
                FavoriteColor = "#FFA14D",

                PlayerBarBg = "rgba(255,216,195,0.86)",
                SidebarBg = "rgba(255,198,167,0.84)"
            },

            // Mixed Berries
            new()
            {
                Name = "Mixed-Berries",
                Description = "Sige na, hipaki na primo",

                BgPrimary = "#FFF8FD",
                BgSecondary = "#FFE5F7",
                BgSurface = "#EAF3FF",

                Border = "#BFD6F4",

                TextPrimary = "#302632",
                TextSecondary = "#766C80",

                Accent = "#F5B7E9",
                AccentHover = "#FFD2F4",
                AccentSubtle = "#FFF0FA",
                AccentActive = "#B8D6FA",

                IconDefault = "#8AAEDC",
                ProgressFill = "#B8D6FA",
                FavoriteColor = "#F5B7E9",

                PlayerBarBg = "#F8ECFF",
                SidebarBg = "#FFD4F2"
            },

            // Aero
            new()
            {
                Name = "Aero",
                Description = "Windows Vista called — it wants its glass back",
                Wallpaper = "aero-bgm.png",

                BgPrimary = "rgba(13,27,42,0.25)",
                BgSecondary = "rgba(20,40,70,0.35)",
                BgSurface = "rgba(25,50,85,0.35)",
                Border = "rgba(255,255,255,0.18)",

                TextPrimary = "#F2F7FF",
                TextSecondary = "#B8D4E8",

                Accent = "#1A6EC4",
                AccentHover = "#35D6FF",
                AccentSubtle = "rgba(26,110,196,0.20)",
                AccentActive = "#35D6FF",

                IconDefault = "#AFC7D9",
                ProgressFill = "#A8E63A",
                FavoriteColor = "#35D6FF",

                PlayerBarBg = "rgba(20,40,70,0.40)",
                SidebarBg = "rgba(10,21,32,0.45)"
            },

            // Astonishing
            new()
            {
                Name = "Astonishing",
                Description = "i do prefer yellow spandex",

                BgPrimary = "#061A3A",
                BgSecondary = "#082652",
                BgSurface = "#103B78",

                Border = "#FFD21F",

                TextPrimary = "#FFF3B0",
                TextSecondary = "#D8C46A",

                Accent = "#FFD21F",
                AccentHover = "#FFE45C",
                AccentSubtle = "#132E63",
                AccentActive = "#FFB800",

                IconDefault = "#FFD21F",
                ProgressFill = "#FFD21F",
                FavoriteColor = "#FFB800",

                PlayerBarBg = "#04152F",
                SidebarBg = "#020B1A"
            },

            // Yombots Prime
            new()
            {
                Name = "Yombots Prime",
                Description = "Tomboy^2",

                BgPrimary = "#180000",
                BgSecondary = "#250000",
                BgSurface = "#3A0505",

                Border = "#C79A00",

                TextPrimary = "#FFF2B8",
                TextSecondary = "#FFC857",

                Accent = "#E10600",
                AccentHover = "#FF2A1F",
                AccentSubtle = "#4A0906",
                AccentActive = "#FFD700",

                IconDefault = "#FFD700",
                ProgressFill = "#FFD700",
                FavoriteColor = "#FFD700",

                PlayerBarBg = "#120000",
                SidebarBg = "#050505"
            },
            // _Ayranic
            new()
            {
                Name = "_Ran",
                Description = "ECHUUUU",
                Wallpaper = "ayranic-bgm.png",

                BgPrimary = "rgba(8,18,36,0.28)",
                BgSecondary = "rgba(11,20,40,0.35)",
                BgSurface = "rgba(14,25,48,0.40)",
                Border = "rgba(160,210,255,0.18)",

                TextPrimary = "#F4F7FB",
                TextSecondary = "#B8C7D9",

                Accent = "#4DD7FF",
                AccentHover = "#7DE4FF",
                AccentSubtle = "rgba(77,215,255,0.12)",
                AccentActive = "#57F2C3",

                IconDefault = "#B8C7D9",
                ProgressFill = "#57F2C3",
                FavoriteColor = "#F7D77A",

                PlayerBarBg = "rgba(11,20,40,0.42)",
                SidebarBg = "rgba(6,12,24,0.50)"
            },
                // Ultra
                new()
                {
                    Name = "Ultra",
                    Description = "Some people are impossible to please... ",
                    Wallpaper = "monster-ultra-bg.png",

                    BgPrimary = "rgba(31,38,40,0.25)",
                    BgSecondary = "rgba(38,46,48,0.35)",
                    BgSurface = "rgba(45,54,56,0.35)",
                    Border = "rgba(199,208,211,0.15)",

                    TextPrimary = "#FFFFFF",
                    TextSecondary = "#E6EAEB",

                    Accent = "#00C8D7",
                    AccentHover = "#72E6F2",
                    AccentSubtle = "rgba(0,200,215,0.15)",
                    AccentActive = "#72E6F2",

                    IconDefault = "#E6EAEB",
                    ProgressFill = "#A6FF3D",
                    FavoriteColor = "#A6FF3D",

                    PlayerBarBg = "rgba(38,46,48,0.40)",
                    SidebarBg = "rgba(10,13,14,0.90)"
                },
            // ========== SPECIAL THEMES ==========

            // RGB Gamer
            new()
            {
                Name = "RGB Gamer",
                Description = "Customizable RGB — cycle speed, brightness, pattern",

                BgPrimary = "#0A0A0A",
                BgSecondary = "#0F0F0F",
                BgSurface = "#161616",

                Border = "#2A2A2A",

                TextPrimary = "#FFFFFF",
                TextSecondary = "#AAAAAA",

                Accent = "#FF0000",
                AccentHover = "#FF4444",
                AccentSubtle = "#1A0000",
                AccentActive = "#FF8888",

                IconDefault = "#BBBBBB",
                ProgressFill = "#FF0000",
                FavoriteColor = "#FF0000",

                PlayerBarBg = "#0F0F0F",
                SidebarBg = "#050505",

                IsRgb = true,
                RgbSpeed = 3,
                RgbBrightness = 1.0,
                RgbSaturation = 1.0,
                RgbPattern = "wave"
            },

            // RGB Neon
            new()
            {
                Name = "RGB Neon",
                Description = "Brighter, faster, electric",

                BgPrimary = "#000000",
                BgSecondary = "#050505",
                BgSurface = "#0D0D0D",

                Border = "#333333",

                TextPrimary = "#FFFFFF",
                TextSecondary = "#CCCCCC",

                Accent = "#00FF00",
                AccentHover = "#44FF44",
                AccentSubtle = "#001A00",
                AccentActive = "#88FF88",

                IconDefault = "#CCCCCC",
                ProgressFill = "#00FF00",
                FavoriteColor = "#00FF00",

                PlayerBarBg = "#050505",
                SidebarBg = "#000000",

                IsRgb = true,
                RgbSpeed = 1.5,
                RgbBrightness = 1.0,
                RgbSaturation = 1.0,
                RgbPattern = "pulse"
            },

            // RGB Pastel
            new()
            {
                Name = "RGB Pastel",
                Description = "Soft, slow, dreamy",

                BgPrimary = "#1A1A1A",
                BgSecondary = "#222222",
                BgSurface = "#2A2A2A",

                Border = "#3A3A3A",

                TextPrimary = "#F0F0F0",
                TextSecondary = "#BBBBBB",

                Accent = "#FFB3BA",
                AccentHover = "#FFC4CC",
                AccentSubtle = "#1A1516",
                AccentActive = "#FFD1D6",

                IconDefault = "#CCCCCC",
                ProgressFill = "#FFB3BA",
                FavoriteColor = "#FFB3BA",

                PlayerBarBg = "#222222",
                SidebarBg = "#111111",

                IsRgb = true,
                RgbSpeed = 5,
                RgbBrightness = 0.6,
                RgbSaturation = 0.4,
                RgbPattern = "breathing"
            },

            // RGB Vaporwave
            new()
            {
                Name = "RGB Vaporwave",
                Description = "Purple/pink/cyan retro vibes",

                BgPrimary = "#0D0A1A",
                BgSecondary = "#120F24",
                BgSurface = "#1A1530",

                Border = "#2A2040",

                TextPrimary = "#FF88CC",
                TextSecondary = "#88CCFF",

                Accent = "#FF00FF",
                AccentHover = "#FF44FF",
                AccentSubtle = "#1A0020",
                AccentActive = "#00FFFF",

                IconDefault = "#CC88FF",
                ProgressFill = "#FF00FF",
                FavoriteColor = "#00FFFF",

                PlayerBarBg = "#120F24",
                SidebarBg = "#080510",

                IsRgb = true,
                RgbSpeed = 4,
                RgbBrightness = 0.8,
                RgbSaturation = 1.0,
                RgbPattern = "wave"
            }
        };

        var savedTheme = Preferences.Get(ThemeKey, "Sierra");
        CurrentTheme = BuiltInThemes.FirstOrDefault(t => t.Name == savedTheme) ?? BuiltInThemes[0];
    }

    public void ApplyTheme(string themeName)
    {
        var theme = BuiltInThemes.FirstOrDefault(t => t.Name == themeName);

        if (theme == null)
        {
            return;
        }

        CurrentTheme = theme;
        Preferences.Set(ThemeKey, themeName);
        ThemeChanged?.Invoke();
    }

    public void ApplyTheme(ThemeDefinition theme)
    {
        CurrentTheme = theme;
        Preferences.Set(ThemeKey, theme.Name);
        ThemeChanged?.Invoke();
    }

    public string GetThemeJson()
    {
        return JsonSerializer.Serialize(CurrentTheme, ThemeJsonOptions);
    }

    public void ImportTheme(string json)
    {
        try
        {
            var theme = JsonSerializer.Deserialize<ThemeDefinition>(json, ThemeJsonOptions);

            if (theme != null)
            {
                ApplyTheme(theme);
            }
        }
        catch
        {
        }
    }
}