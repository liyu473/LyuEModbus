using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace EModbus.Converters;

public static class WindowStateConverters
{
    public static readonly IValueConverter IsFullScreen =
        new FuncValueConverter<WindowState, bool>(state => state == WindowState.FullScreen);

    public static readonly IValueConverter IsNotFullScreen =
        new FuncValueConverter<WindowState, bool>(state => state != WindowState.FullScreen);
}