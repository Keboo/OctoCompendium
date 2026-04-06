namespace OctoCompendium.Services.Matching;

/// <summary>
/// Downloads the CLIP ONNX model to local storage when it is not bundled with the app.
/// </summary>
public interface IModelDownloadService
{
    /// <summary>
    /// Whether the model file exists in either the app package or local storage.
    /// </summary>
    bool IsModelAvailable { get; }

    /// <summary>
    /// Returns the path to the model file, checking local storage first, then the app package.
    /// Returns null if the model is not available in either location.
    /// </summary>
    string? GetModelPath();

    /// <summary>
    /// Downloads the CLIP model to local storage.
    /// </summary>
    Task DownloadModelAsync(IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}
