namespace OctoCompendium.Services.Matching;

/// <summary>
/// Runs CLIP inference on-device to match captured images against known stickers.
/// </summary>
public interface IStickerMatcher
{
    /// <summary>
    /// Initializes the ONNX model and loads embeddings. Call once at startup.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Matches an image against the known sticker set.
    /// Returns the top N matches ranked by confidence (cosine similarity).
    /// </summary>
    /// <param name="imageStream">The captured image as a stream.</param>
    /// <param name="topN">Number of top results to return.</param>
    Task<IReadOnlyList<MatchResult>> MatchAsync(Stream imageStream, int topN = 5);

    /// <summary>
    /// Whether the matcher has been initialized and is ready for inference.
    /// </summary>
    bool IsReady { get; }
}
