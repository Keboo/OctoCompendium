using System.Text.Json;

namespace OctoCompendium.Services.Matching;

/// <summary>
/// Loads pre-computed CLIP embeddings and sticker metadata from bundled app assets.
/// 
/// Expected assets:
///   - Assets/Stickers/manifest.json — array of sticker metadata objects
///   - Assets/Stickers/embeddings.bin — contiguous float32 vectors, 512 floats per sticker
/// </summary>
public class EmbeddingStore : IEmbeddingStore
{
    private const int EmbeddingDimension = 512;

    private List<Sticker> _stickers = [];
    private float[] _embeddings = [];

    public IReadOnlyList<Sticker> Stickers => _stickers;
    public int Count => _stickers.Count;

    public async Task LoadAsync()
    {
        // Load manifest
        var manifestPath = Path.Combine(
            Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
            "Assets", "Stickers", "manifest.json");

        if (!File.Exists(manifestPath))
        {
            // Fallback: empty set for development
            _stickers = [];
            _embeddings = [];
            return;
        }

        var manifestJson = await File.ReadAllTextAsync(manifestPath);
        _stickers = JsonSerializer.Deserialize<List<Sticker>>(manifestJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        // Load embeddings binary
        var embeddingsPath = Path.Combine(
            Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
            "Assets", "Stickers", "embeddings.bin");

        if (File.Exists(embeddingsPath))
        {
            var bytes = await File.ReadAllBytesAsync(embeddingsPath);
            _embeddings = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, _embeddings, 0, bytes.Length);
        }
    }

    public ReadOnlySpan<float> GetEmbedding(int embeddingIndex)
    {
        int offset = embeddingIndex * EmbeddingDimension;
        return _embeddings.AsSpan(offset, EmbeddingDimension);
    }
}
