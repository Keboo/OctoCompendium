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
    private const string EmbeddingsFileName = "embeddings.bin";
    private const string StickersSubFolder = "Stickers";

    private List<Sticker> _stickers = [];
    private float[] _embeddings = [];

    public IReadOnlyList<Sticker> Stickers => _stickers;
    public int Count => _stickers.Count;
    public bool HasEmbeddings => _embeddings.Length > 0;

    public async Task LoadAsync()
    {
        // Load manifest
        var manifestPath = Path.Combine(
            Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
            "Assets", StickersSubFolder, "manifest.json");

        if (!File.Exists(manifestPath))
        {
            _stickers = [];
            _embeddings = [];
            return;
        }

        var manifestJson = await File.ReadAllTextAsync(manifestPath);
        _stickers = JsonSerializer.Deserialize<List<Sticker>>(manifestJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        // Try local storage first (generated embeddings), then bundled asset
        var localPath = GetLocalEmbeddingsPath();
        var assetPath = Path.Combine(
            Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
            "Assets", StickersSubFolder, EmbeddingsFileName);

        var embeddingsPath = File.Exists(localPath) ? localPath
                           : File.Exists(assetPath) ? assetPath
                           : null;

        if (embeddingsPath is not null)
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

    public async Task SaveEmbeddingsAsync(float[] embeddings)
    {
        _embeddings = embeddings;

        var localPath = GetLocalEmbeddingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

        var bytes = new byte[embeddings.Length * sizeof(float)];
        Buffer.BlockCopy(embeddings, 0, bytes, 0, bytes.Length);
        await File.WriteAllBytesAsync(localPath, bytes);
    }

    private static string GetLocalEmbeddingsPath()
    {
        var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        return Path.Combine(localFolder, StickersSubFolder, EmbeddingsFileName);
    }
}
