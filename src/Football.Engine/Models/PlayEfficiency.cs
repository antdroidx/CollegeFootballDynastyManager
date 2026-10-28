namespace DynastyManager.Core.Models;

/// <summary>Compact totals from all scrimmage events, never selected highlights. Optional for old saves.</summary>
public sealed record PlayEfficiency(int Plays, int Yards, int Successful, int ThirdDowns, int ThirdDownConversions, int SackYardsLost);
