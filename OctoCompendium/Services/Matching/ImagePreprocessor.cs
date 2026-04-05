using SkiaSharp;

namespace OctoCompendium.Services.Matching;

/// <summary>
/// Preprocesses images for CLIP inference: resize to 224x224, normalize with
/// ImageNet mean/std, and convert to a float tensor in NCHW format.
/// </summary>
public static class ImagePreprocessor
{
    private const int TargetSize = 224;

    // CLIP uses ImageNet normalization
    private static readonly float[] Mean = [0.48145466f, 0.4578275f, 0.40821073f];
    private static readonly float[] Std = [0.26862954f, 0.26130258f, 0.27577711f];

    /// <summary>
    /// Reads an image stream and produces a float[] tensor of shape [1, 3, 224, 224].
    /// </summary>
    public static float[] PreprocessImage(Stream imageStream)
    {
        using var bitmap = SKBitmap.Decode(imageStream);
        using var resized = bitmap.Resize(new SKImageInfo(TargetSize, TargetSize), SKSamplingOptions.Default);

        var tensor = new float[1 * 3 * TargetSize * TargetSize];

        for (int y = 0; y < TargetSize; y++)
        {
            for (int x = 0; x < TargetSize; x++)
            {
                var pixel = resized.GetPixel(x, y);

                int offset = y * TargetSize + x;
                // NCHW format: channel, height, width
                tensor[0 * TargetSize * TargetSize + offset] = (pixel.Red / 255f - Mean[0]) / Std[0];
                tensor[1 * TargetSize * TargetSize + offset] = (pixel.Green / 255f - Mean[1]) / Std[1];
                tensor[2 * TargetSize * TargetSize + offset] = (pixel.Blue / 255f - Mean[2]) / Std[2];
            }
        }

        return tensor;
    }
}
