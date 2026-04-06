using OctoCompendium.Services.Collection;

namespace OctoCompendium.Presentation;

public partial class MatchResultViewModel : ObservableObject
{
    private readonly ICollectionService _collection;
    private readonly INavigator _navigator;

    [ObservableProperty]
    private IReadOnlyList<MatchResult> matches = [];

    [ObservableProperty]
    private MatchResult? selectedMatch;

    [ObservableProperty]
    private string? uploadedImagePath;

    public MatchResultViewModel(
        ICollectionService collection,
        INavigator navigator,
        MatchResultData data)
    {
        _collection = collection;
        _navigator = navigator;
        Matches = data.Matches;
        UploadedImagePath = data.UploadedImagePath;
        ConfirmMatchCommand = new AsyncRelayCommand(OnConfirmMatch, () => SelectedMatch is not null);
    }

    public ICommand ConfirmMatchCommand { get; }

    partial void OnSelectedMatchChanged(MatchResult? value)
    {
        ((AsyncRelayCommand)ConfirmMatchCommand).NotifyCanExecuteChanged();
    }

    private async Task OnConfirmMatch()
    {
        if (SelectedMatch is null) return;

        await _collection.MarkOwnedAsync(SelectedMatch.Sticker.Id);
        await _navigator.NavigateBackAsync(this);
    }
}
