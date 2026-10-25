namespace DynastyManager.Core.Models;

public sealed record SnapPersonnel
{
    public required string TeamName { get; init; }
    public IReadOnlyList<Guid> PlayerIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyList<Guid> SubstitutedPlayerIds { get; init; } = Array.Empty<Guid>();
}

public sealed class GameFatigueState
{
    private readonly Dictionary<Guid, double> _energy = new();

    public double Energy(Guid playerId) => _energy.TryGetValue(playerId, out var value) ? value : 100;
    public void Set(Guid playerId, double value) => _energy[playerId] = Math.Clamp(value, 0, 100);
}
