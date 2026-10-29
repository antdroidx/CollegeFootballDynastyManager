using System.Reflection;
using System.Text.Json;
using DynastyManager.Core.Models;
namespace ProFootball.Core;
public sealed record ProAppearanceMatch(string ExternalId,int SkinTone,string Source,string ObservedColor,double Confidence);
public static class ProAppearanceData
{
    static readonly Lazy<Dictionary<string,ProAppearanceMatch>> Matches=new(()=>{
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ProFootball.Core.nfl-appearances.json")!;
        return JsonSerializer.Deserialize<List<ProAppearanceMatch>>(stream)!.Where(x=>x.Confidence>=.65&&x.SkinTone is >=0 and <=6&&PortraitAppearance.ValidSkinColor(x.ObservedColor)is not null).DistinctBy(x=>x.ExternalId).ToDictionary(x=>x.ExternalId);
    });
    public static ProFranchise Apply(ProFranchise s)=>s with{Players=s.Players.Select(p=>{
        if(p.AppearanceOrigin=="custom"||p.AppearanceOrigin is null&&p.Football.Appearance?.SkinTone is not null||!Matches.Value.TryGetValue(p.ExternalId,out var match))return p;
        return p with{AppearanceOrigin=match.Source,Football=p.Football with{Appearance=(p.Football.Appearance??new NcaaPlayerAppearance()) with{SkinTone=match.SkinTone,SkinColorHex=match.ObservedColor}}};
    }).ToList()};
}
