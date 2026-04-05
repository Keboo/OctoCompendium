namespace OctoCompendium.Services.Collection;

/// <summary>
/// Manages the user's sticker collection state (which stickers they own).
/// </summary>
public interface ICollectionService
{
    Task InitializeAsync();
    Task<IReadOnlyList<CollectionEntry>> GetAllEntriesAsync();
    Task<CollectionEntry?> GetEntryAsync(string stickerId);
    Task MarkOwnedAsync(string stickerId, string? photoPath = null);
    Task MarkNotOwnedAsync(string stickerId);
    Task<int> OwnedCountAsync();
    Task<int> TotalCountAsync();
}
