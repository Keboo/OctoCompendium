namespace OctoCompendium.Models;

/// <summary>
/// Data passed from the Capture page to the Match Result page,
/// containing the uploaded image path and the match results.
/// </summary>
public class MatchResultData
{
    public required string UploadedImagePath { get; init; }
    public required IReadOnlyList<MatchResult> Matches { get; init; }
}
