namespace OctoCompendium.Models;

public class MatchResult
{
    public required Sticker Sticker { get; init; }
    public double Confidence { get; init; }
}
