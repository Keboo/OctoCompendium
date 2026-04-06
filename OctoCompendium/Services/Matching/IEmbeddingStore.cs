namespace OctoCompendium.Services.Matching;

/// <summary>
/// Manages pre-computed CLIP embeddings for the known sticker set.
/// </summary>
public interface IEmbeddingStore
{
    /// <summary>
    /// Loads sticker metadata and their pre-computed embeddings.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Returns all known stickers.
    /// </summary>
    IReadOnlyList<Sticker> Stickers { get; }

    /// <summary>
    /// Returns the embedding vector for a given sticker by its embedding index.
    /// </summary>
    ReadOnlySpan<float> GetEmbedding(int embeddingIndex);

    /// <summary>
    /// Total number of loaded stickers.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Whether embeddings have been loaded or generated.
    /// </summary>
    bool HasEmbeddings { get; }

    /// <summary>
    /// Sets the embedding data from externally-generated vectors and persists to local storage.
    /// </summary>
    Task SaveEmbeddingsAsync(float[] embeddings);
}
