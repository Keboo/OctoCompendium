using SQLite;

namespace OctoCompendium.Services.Collection;

[Table("collection_entries")]
public class CollectionEntryEntity
{
    [PrimaryKey]
    public string StickerId { get; set; } = string.Empty;
    public bool Owned { get; set; }
    public DateTime? DateAcquired { get; set; }
    public string? PhotoPath { get; set; }
}

/// <summary>
/// SQLite-backed collection tracking service.
/// </summary>
public class CollectionService : ICollectionService
{
    private readonly ILogger<CollectionService> _logger;
    private SQLiteAsyncConnection? _db;

    public CollectionService(ILogger<CollectionService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OctoCompendium", "collection.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        _db = new SQLiteAsyncConnection(dbPath);
        await _db.CreateTableAsync<CollectionEntryEntity>();
        _logger.LogInformation("Collection database initialized at {Path}", dbPath);
    }

    public async Task<IReadOnlyList<CollectionEntry>> GetAllEntriesAsync()
    {
        var entities = await _db!.Table<CollectionEntryEntity>().ToListAsync();
        return entities.Select(e => new CollectionEntry
        {
            StickerId = e.StickerId,
            Owned = e.Owned,
            DateAcquired = e.DateAcquired,
            PhotoPath = e.PhotoPath
        }).ToList();
    }

    public async Task<CollectionEntry?> GetEntryAsync(string stickerId)
    {
        var entity = await _db!.Table<CollectionEntryEntity>()
            .FirstOrDefaultAsync(e => e.StickerId == stickerId);

        if (entity is null) return null;
        return new CollectionEntry
        {
            StickerId = entity.StickerId,
            Owned = entity.Owned,
            DateAcquired = entity.DateAcquired,
            PhotoPath = entity.PhotoPath
        };
    }

    public async Task MarkOwnedAsync(string stickerId, string? photoPath = null)
    {
        var entity = await _db!.Table<CollectionEntryEntity>()
            .FirstOrDefaultAsync(e => e.StickerId == stickerId);

        if (entity is not null)
        {
            entity.Owned = true;
            entity.DateAcquired = DateTime.UtcNow;
            entity.PhotoPath = photoPath ?? entity.PhotoPath;
            await _db.UpdateAsync(entity);
        }
        else
        {
            await _db.InsertAsync(new CollectionEntryEntity
            {
                StickerId = stickerId,
                Owned = true,
                DateAcquired = DateTime.UtcNow,
                PhotoPath = photoPath
            });
        }
    }

    public async Task MarkNotOwnedAsync(string stickerId)
    {
        var entity = await _db!.Table<CollectionEntryEntity>()
            .FirstOrDefaultAsync(e => e.StickerId == stickerId);

        if (entity is not null)
        {
            entity.Owned = false;
            entity.DateAcquired = null;
            await _db.UpdateAsync(entity);
        }
    }

    public async Task<int> OwnedCountAsync()
        => await _db!.Table<CollectionEntryEntity>().CountAsync(e => e.Owned);

    public async Task<int> TotalCountAsync()
        => await _db!.Table<CollectionEntryEntity>().CountAsync();
}
