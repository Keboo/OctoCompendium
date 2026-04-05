using Microsoft.UI.Xaml.Data;

namespace OctoCompendium.Presentation;

public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? 1.0 : 0.35;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

public class PercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is double d ? $"{d:P0}" : "0%";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
