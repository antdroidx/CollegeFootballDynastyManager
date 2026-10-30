using System.Text.Json;
using DynastyManager.Core.Models;
namespace ProFootball.Core;

/// <summary>Frozen source-calibrated market values; existing signed contracts are never repriced.</summary>
public static class ProMarket
{
    sealed record Anchor(double Overall,long Annual);
    sealed record Curve(string Position,int Samples,long Ceiling,List<Anchor> Anchors);
    sealed record Pack(int Version,long BaseCap,int MinimumSamples,List<Curve> Curves);
    static readonly Lazy<Pack> Data=new(()=>
    {
        using var stream=typeof(ProMarket).Assembly.GetManifestResourceStream("ProFootball.Core.pro-market-curves.json")!;
        var pack=JsonSerializer.Deserialize<Pack>(stream,new JsonSerializerOptions{PropertyNameCaseInsensitive=true})??throw new InvalidDataException("Missing market calibration.");
        if(pack.Version!=1||pack.BaseCap!=ProFinance.Base2026||pack.Curves.Select(c=>c.Position).Distinct().Count()!=pack.Curves.Count||
            Enum.GetNames<Position>().Where(p=>p!="FB").Any(p=>!pack.Curves.Any(c=>c.Position==p)))throw new InvalidDataException("Incomplete market calibration.");
        foreach(var c in pack.Curves)
            if(c.Samples<pack.MinimumSamples||c.Anchors.Count<2||c.Anchors[0].Overall!=40||c.Anchors[^1].Overall!=99||c.Anchors.Any(a=>a.Annual<=0||a.Annual>c.Ceiling)||
                c.Anchors.Zip(c.Anchors.Skip(1)).Any(pair=>pair.First.Overall>=pair.Second.Overall||pair.First.Annual>pair.Second.Annual))throw new InvalidDataException("Invalid market curve.");
        return pack;
    });
    static readonly Lazy<Dictionary<string,long[]>> Prices=new(()=>Data.Value.Curves.ToDictionary(c=>c.Position,c=>Enumerable.Range(0,100).Select(r=>Interpolate(c,Math.Clamp(r,40,99))).ToArray()));
    static long Interpolate(Curve curve,int overall)
    {
        for(var i=1;i<curve.Anchors.Count;i++)
        {
            var right=curve.Anchors[i];if(overall>right.Overall)continue;
            var left=curve.Anchors[i-1];var fraction=(overall-left.Overall)/(right.Overall-left.Overall);
            return Math.Min(curve.Ceiling,(long)Math.Round(Math.Exp(Math.Log(left.Annual)+(Math.Log(right.Annual)-Math.Log(left.Annual))*fraction),MidpointRounding.AwayFromZero));
        }
        return curve.Ceiling;
    }
    public static long BaseSalary(ProPlayer player)
    {
        var key=player.Football.Position==Position.FB?"RB":player.Football.Position.ToString();var prices=Prices.Value;
        if(player.Football.Position==Position.K&&player.Football.NcaaPositionCode is int code&&prices.ContainsKey("K/"+code))key="K/"+code;
        return Math.Max(300_000,(long)(prices[key][Math.Clamp(player.Football.OverallRating,1,99)] * (player.Football.Position==Position.FB ? .4 : 1)));
    }
}
