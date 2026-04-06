using Microsoft.UI.Xaml.Media.Imaging;

namespace OctoCompendium.Presentation;

public sealed partial class MatchResultPage : Page
{
    public MatchResultPage()
    {
        this.InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is MatchResultViewModel vm && vm.UploadedImagePath is string path)
        {
            var bitmap = new BitmapImage(new Uri(path));
            UploadedImage.Source = bitmap;
        }
    }
}
