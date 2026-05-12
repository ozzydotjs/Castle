namespace Castle;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
#if WINDOWS
        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("bg", (handler, _) =>
        {
            if (handler.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 webView)
            {
                webView.DefaultBackgroundColor = Windows.UI.Color.FromArgb(0, 26, 29, 38);
            }
        });
#endif
    }
}