namespace OctoCompendium.Models;

public record Entity(string Name);

public class Sticker
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ImageFileName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int EmbeddingIndex { get; set; }
}

public class CollectionEntry
{
    public string StickerId { get; set; } = string.Empty;
    public bool Owned { get; set; }
    public DateTime? DateAcquired { get; set; }
    public string? PhotoPath { get; set; }
}
