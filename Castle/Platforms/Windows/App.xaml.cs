using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Castle.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        var window = Application.Windows[0].Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (window != null)
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            appWindow.TitleBar.BackgroundColor = Windows.UI.Color.FromArgb(255, 26, 29, 38);
            appWindow.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(255, 26, 29, 38);
            appWindow.TitleBar.InactiveBackgroundColor = Windows.UI.Color.FromArgb(255, 26, 29, 38);
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        }
    }
}