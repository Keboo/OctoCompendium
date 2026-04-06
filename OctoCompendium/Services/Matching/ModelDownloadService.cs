namespace OctoCompendium.Services.Matching;

public class ModelDownloadService : IModelDownloadService
{
    private const string ModelFileName = "clip-image-encoder.onnx";
    private const string ModelSubFolder = "Models";
    private const string ModelUrl = "https://huggingface.co/Qdrant/clip-ViT-B-32-vision/resolve/main/model.onnx";

    private readonly ILogger<ModelDownloadService> _logger;
    private readonly HttpClient _httpClient;

    public ModelDownloadService(ILogger<ModelDownloadService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public bool IsModelAvailable => GetModelPath() is not null;

    public string? GetModelPath()
    {
        // Check local storage first (downloaded model)
        var localPath = GetLocalModelPath();
        if (File.Exists(localPath))
            return localPath;

        // Fall back to bundled asset
        var assetPath = Path.Combine(
            Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
            "Assets", ModelSubFolder, ModelFileName);
        if (File.Exists(assetPath))
            return assetPath;

        return null;
    }

    public async Task DownloadModelAsync(IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var localPath = GetLocalModelPath();
        var directory = Path.GetDirectoryName(localPath)!;
        Directory.CreateDirectory(directory);

        var tempPath = localPath + ".tmp";

        try
        {
            using var response = await _httpClient.GetAsync(ModelUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920);

            var buffer = new byte[81920];
            long bytesRead = 0;
            int read;

            while ((read = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                bytesRead += read;

                if (totalBytes > 0)
                {
                    progress?.Report((double)bytesRead / totalBytes.Value);
                }
            }

            await fileStream.FlushAsync(cancellationToken);
            fileStream.Close();

            // Atomic move from temp to final
            File.Move(tempPath, localPath, overwrite: true);

            _logger.LogInformation("CLIP model downloaded successfully to {Path} ({Bytes} bytes).", localPath, bytesRead);
        }
        catch
        {
            // Clean up partial download
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    private static string GetLocalModelPath()
    {
        var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        return Path.Combine(localFolder, ModelSubFolder, ModelFileName);
    }
}
