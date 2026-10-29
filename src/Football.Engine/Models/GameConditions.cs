namespace DynastyManager.Core.Models;

public enum GamePrecipitation { Clear, Rain, Snow }
public sealed record GameWeather
{
    public GamePrecipitation Precipitation { get; init; }
    public int TemperatureFahrenheit { get; init; } = 68;
    public int WindMph { get; init; }
    public int WindFieldDirection { get; init; } = 1;
    public bool Indoors { get; init; }
    public string Summary => Indoors ? "Indoors · 72°F · no wind" : $"{Precipitation} · {TemperatureFahrenheit}°F · wind {WindMph} mph {(WindFieldDirection > 0 ? "→" : WindFieldDirection < 0 ? "←" : "↕")}";
}

public sealed record PlayerGameAward
{
    public required string GameId { get; init; }
    public int SeasonYear { get; init; }
    public int Week { get; init; }
    public Guid PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required string TeamName { get; init; }
    public Position Position { get; init; }
    public required string Summary { get; init; }
}
