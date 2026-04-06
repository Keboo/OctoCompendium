using OctoCompendium.Services.Collection;
using OctoCompendium.Services.Matching;

namespace OctoCompendium.Presentation;

public partial class CaptureViewModel : ObservableObject
{
    private readonly IStickerMatcher _matcher;
    private readonly IModelDownloadService _modelDownloadService;
    private readonly INavigator _navigator;

    [ObservableProperty]
    private bool isProcessing;

    [ObservableProperty]
    private bool isModelMissing;

    [ObservableProperty]
    private bool isDownloading;

    [ObservableProperty]
    private double downloadProgress;

    [ObservableProperty]
    private string statusMessage = "Take a photo of a sticker to identify it.";

    public CaptureViewModel(IStickerMatcher matcher, IModelDownloadService modelDownloadService, INavigator navigator)
    {
        _matcher = matcher;
        _modelDownloadService = modelDownloadService;
        _navigator = navigator;
        PickPhotoCommand = new AsyncRelayCommand(OnPickPhoto);
        DownloadModelCommand = new AsyncRelayCommand(OnDownloadModel);
        IsModelMissing = !_matcher.IsReady && !_modelDownloadService.IsModelAvailable;
    }

    public ICommand PickPhotoCommand { get; }
    public ICommand DownloadModelCommand { get; }

    private async Task OnDownloadModel()
    {
        try
        {
            IsDownloading = true;
            DownloadProgress = 0;
            StatusMessage = "Downloading CLIP model...";

            var progress = new Progress<double>(p =>
            {
                DownloadProgress = p;
                StatusMessage = $"Downloading CLIP model... {p:P0}";
            });

            await _modelDownloadService.DownloadModelAsync(progress);

            StatusMessage = "Model downloaded. Initializing...";
            await _matcher.InitializeAsync();

            IsModelMissing = false;
            StatusMessage = _matcher.IsReady
                ? "Take a photo of a sticker to identify it."
                : "Model downloaded but failed to load. Check logs for details.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
        }
    }

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
                IsModelMissing = true;
                StatusMessage = "The CLIP model is required to scan stickers. Tap the button below to download it.";
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
