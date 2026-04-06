namespace OctoCompendium.Models;

public class MatchResult
{
    public required Sticker Sticker { get; init; }
    public double Confidence { get; init; }

    /// <summary>
    /// URI for the sticker image bundled in the app package.
    /// </summary>
    public string StickerImageUri => $"ms-appx:///Assets/Stickers/{Sticker.ImageFileName}";
}
