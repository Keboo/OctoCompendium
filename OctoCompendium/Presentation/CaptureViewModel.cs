using OctoCompendium.Services.Collection;
using OctoCompendium.Services.Matching;

namespace OctoCompendium.Presentation;

public partial class CaptureViewModel : ObservableObject
{
    private readonly IStickerMatcher _matcher;
    private readonly INavigator _navigator;

    [ObservableProperty]
    private bool isProcessing;

    [ObservableProperty]
    private string statusMessage = "Take a photo of a sticker to identify it.";

    public CaptureViewModel(IStickerMatcher matcher, INavigator navigator)
    {
        _matcher = matcher;
        _navigator = navigator;
        PickPhotoCommand = new AsyncRelayCommand(OnPickPhoto);
    }

    public ICommand PickPhotoCommand { get; }

    private async Task OnPickPhoto()
    {
        try
        {
            if (!_matcher.IsReady)
            {
                StatusMessage = "Initializing matcher...";
                await _matcher.InitializeAsync();
            }

            if (!_matcher.IsReady)
            {
                StatusMessage = "Matcher is not available. Please ensure the CLIP model (clip-image-encoder.onnx) is placed in the Assets/Models folder.";
                return;
            }

            IsProcessing = true;
            StatusMessage = "Analyzing sticker...";

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                StatusMessage = "No photo selected.";
                return;
            }

            using var stream = await file.OpenStreamForReadAsync();
            var matches = await _matcher.MatchAsync(stream);

            await _navigator.NavigateViewModelAsync<MatchResultViewModel>(this, data: matches);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
