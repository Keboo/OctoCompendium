using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace OctoCompendium.Services.Matching;

/// <summary>
/// On-device sticker matching using CLIP ViT-B/32 via ONNX Runtime.
/// Embeds a captured image and finds the closest matches from pre-computed sticker embeddings.
/// </summary>
public class StickerMatcher : IStickerMatcher, IDisposable
{
    private const int EmbeddingDimension = 512;

    private readonly IEmbeddingStore _embeddingStore;
    private readonly IModelDownloadService _modelDownloadService;
    private readonly ILogger<StickerMatcher> _logger;
    private InferenceSession? _session;

    public bool IsReady => _session is not null;

    public StickerMatcher(IEmbeddingStore embeddingStore, IModelDownloadService modelDownloadService, ILogger<StickerMatcher> logger)
    {
        _embeddingStore = embeddingStore;
        _modelDownloadService = modelDownloadService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _embeddingStore.LoadAsync();

            var modelPath = _modelDownloadService.GetModelPath();

            if (modelPath is not null)
            {
                var options = new SessionOptions();
                _session = new InferenceSession(modelPath, options);

                if (!_embeddingStore.HasEmbeddings && _embeddingStore.Count > 0)
                {
                    _logger.LogInformation("Embeddings not found. Generating from {Count} sticker images...", _embeddingStore.Count);
                    await GenerateEmbeddingsAsync();
                }

                _logger.LogInformation("CLIP model loaded successfully. {Count} stickers indexed.", _embeddingStore.Count);
            }
            else
            {
                _logger.LogWarning("CLIP model not found. Matcher will not be available until the model is downloaded.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize sticker matcher.");
        }
    }

    private async Task GenerateEmbeddingsAsync()
    {
        var stickers = _embeddingStore.Stickers;
        var allEmbeddings = new float[stickers.Count * EmbeddingDimension];
        var assetsPath = Path.Combine(
            Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
            "Assets", "Stickers");

        for (int i = 0; i < stickers.Count; i++)
        {
            var imagePath = Path.Combine(assetsPath, stickers[i].ImageFileName);
            if (!File.Exists(imagePath))
            {
                _logger.LogWarning("Sticker image not found: {Path}", imagePath);
                continue;
            }

            using var stream = File.OpenRead(imagePath);
            var inputTensor = ImagePreprocessor.PreprocessImage(stream);
            var tensor = new DenseTensor<float>(inputTensor, [1, 3, 224, 224]);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("pixel_values", tensor)
            };

            using var results = _session!.Run(inputs);
            var output = results.First().AsEnumerable<float>().ToArray();
            Normalize(output);

            var offset = stickers[i].EmbeddingIndex * EmbeddingDimension;
            Array.Copy(output, 0, allEmbeddings, offset, EmbeddingDimension);
        }

        await _embeddingStore.SaveEmbeddingsAsync(allEmbeddings);
        _logger.LogInformation("Generated and saved embeddings for {Count} stickers.", stickers.Count);
    }

    public Task<IReadOnlyList<MatchResult>> MatchAsync(Stream imageStream, int topN = 5)
    {
        if (_session is null)
            throw new InvalidOperationException("Matcher not initialized. Call InitializeAsync first.");

        // Preprocess image to tensor
        var inputTensor = ImagePreprocessor.PreprocessImage(imageStream);
        var tensor = new DenseTensor<float>(inputTensor, [1, 3, 224, 224]);

        // Run inference
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("pixel_values", tensor)
        };

        using var results = _session.Run(inputs);
        var outputTensor = results.First().AsEnumerable<float>().ToArray();

        // Normalize the output embedding
        Normalize(outputTensor);

        // Compare against all known embeddings via cosine similarity
        var matches = new List<MatchResult>();
        for (int i = 0; i < _embeddingStore.Count; i++)
        {
            var sticker = _embeddingStore.Stickers[i];
            var knownEmbedding = _embeddingStore.GetEmbedding(sticker.EmbeddingIndex);
            var similarity = CosineSimilarity(outputTensor, knownEmbedding);

            matches.Add(new MatchResult
            {
                Sticker = sticker,
                Confidence = similarity
            });
        }

        IReadOnlyList<MatchResult> topMatches = matches
            .OrderByDescending(m => m.Confidence)
            .Take(topN)
            .ToList();

        return Task.FromResult(topMatches);
    }

    private static double CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private static void Normalize(float[] vector)
    {
        double norm = 0;
        for (int i = 0; i < vector.Length; i++)
            norm += vector[i] * vector[i];
        norm = Math.Sqrt(norm);

        if (norm > 0)
        {
            for (int i = 0; i < vector.Length; i++)
                vector[i] /= (float)norm;
        }
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
