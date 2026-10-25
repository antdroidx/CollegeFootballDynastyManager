namespace DynastyManager.Core.Models;

/// <summary>A playing assignment; the athlete's natural position and attributes are retained.</summary>
public sealed record DepthChartAssignment(string TeamName, Guid PlayerId, int PositionCode, int Order);
