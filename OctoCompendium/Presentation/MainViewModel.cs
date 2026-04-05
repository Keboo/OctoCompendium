using OctoCompendium.Services.Collection;
using OctoCompendium.Services.Matching;

namespace OctoCompendium.Presentation;

public partial class CollectionViewModel : ObservableObject
{
    private readonly IStickerMatcher _matcher;
    private readonly ICollectionService _collection;
    private readonly IEmbeddingStore _embeddingStore;
    private readonly INavigator _navigator;

    [ObservableProperty]
    private string title = "Collection";

    [ObservableProperty]
    private List<StickerItemViewModel> stickers = [];

    [ObservableProperty]
    private int ownedCount;

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private string filterMode = "All";

    public CollectionViewModel(
        IStickerMatcher matcher,
        ICollectionService collection,
        IEmbeddingStore embeddingStore,
        INavigator navigator)
    {
        _matcher = matcher;
        _collection = collection;
        _embeddingStore = embeddingStore;
        _navigator = navigator;

        ScanSticker = new AsyncRelayCommand(OnScanSticker);
        RefreshCommand = new AsyncRelayCommand(LoadCollectionAsync);
    }

    public ICommand ScanSticker { get; }
    public ICommand RefreshCommand { get; }

    public async Task LoadCollectionAsync()
    {
        var entries = await _collection.GetAllEntriesAsync();
        var entryLookup = entries.ToDictionary(e => e.StickerId);

        var allStickers = _embeddingStore.Stickers;
        var items = allStickers.Select(s =>
        {
            var owned = entryLookup.TryGetValue(s.Id, out var entry) && entry.Owned;
            return new StickerItemViewModel
            {
                Id = s.Id,
                Name = s.Name,
                ImageFileName = s.ImageFileName,
                Category = s.Category,
                IsOwned = owned
            };
        }).ToList();

        Stickers = FilterMode switch
        {
            "Owned" => items.Where(s => s.IsOwned).ToList(),
            "Missing" => items.Where(s => !s.IsOwned).ToList(),
            _ => items
        };

        OwnedCount = items.Count(s => s.IsOwned);
        TotalCount = items.Count;
    }

    private async Task OnScanSticker()
    {
        await _navigator.NavigateViewModelAsync<CaptureViewModel>(this);
    }
}

public partial class StickerItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string id = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string imageFileName = string.Empty;

    [ObservableProperty]
    private string? category;

    [ObservableProperty]
    private bool isOwned;
}
