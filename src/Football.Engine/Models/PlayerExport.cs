namespace DynastyManager.Core.Models;
/// <summary>Shared v1 interchange. Consumers translate ratings to their league scale.</summary>
public sealed record PlayerExport(int FormatVersion, string Source, int Year, List<DynastyPlayer> Players);
