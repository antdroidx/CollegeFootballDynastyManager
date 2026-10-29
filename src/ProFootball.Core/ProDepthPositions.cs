using DynastyManager.Core.Simulation;
namespace ProFootball.Core;
public static class ProDepthPositions
{
    static readonly string[] Order=DepthChartService.PositionNames.Select(p=>p switch{"HB"=>"RB","LT" or "RT"=>"OT","LG" or "RG"=>"OG","C"=>"OC","LE"=>"DE","RE"=>"DE","LOLB"=>"OLB","ROLB"=>"OLB",_=>p}).SelectMany(p=>p=="OC"?new[]{"OC","OL"}:new[]{p}).Distinct().ToArray();
    public static IReadOnlyList<string> Sort(IEnumerable<string> labels)=>labels.Distinct().OrderBy(p=>{var i=Array.IndexOf(Order,p);return i<0?int.MaxValue:i;}).ThenBy(p=>p,StringComparer.Ordinal).ToArray();
}
