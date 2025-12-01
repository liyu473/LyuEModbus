using Avalonia.Controls;
using Avalonia.Interactivity;
using Window = ShadUI.Window;

namespace EModbus.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void FullScreen_OnClick(object? sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.FullScreen)
        {
            ExitFullScreen();
            ToolTip.SetTip(FullscreenButton, "Fullscreen");
        }
        else
        {
            WindowState = WindowState.FullScreen;
            ToolTip.SetTip(FullscreenButton, "Exit Fullscreen");
        }
    }
}