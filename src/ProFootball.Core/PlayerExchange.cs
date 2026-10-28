using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Core;
public static class ProPlayerImport
{
    public static List<ProPlayer> Convert(PlayerExport export,int seed)
    {
        PlayerExportService.Validate(export);
        return export.Players.Select(original=>{
            var p=PlayerSkillService.EnsureInitialized(original);
            var random=new Random(BitConverter.ToInt32(p.PlayerId.ToByteArray(),0)^seed);
            int Scale(int n)=>Math.Clamp((int)Math.Round(35+n*.45)+random.Next(-4,5),30,95);
            var k=p.Skills!;var overall=Scale(p.OverallRating);
            var football=p with{TeamName="Draft class",OverallRating=overall,SeasonStartOverall=overall,PotentialRating=Math.Clamp(Math.Max(overall,Scale(p.PotentialRating)+5),30,99),SpeedRating=Scale(p.SpeedRating),StrengthRating=Scale(p.StrengthRating),AgilityRating=Scale(p.AgilityRating),AwarenessRating=Scale(p.AwarenessRating),DepthChartOrder=99,PlayingPositionCode=null,ClassYear=0,IsRedshirted=false,HasRedshirted=false,IsWalkOn=false,
                Skills=k with{PassingPower=Scale(k.PassingPower),PassingAccuracy=Scale(k.PassingAccuracy),Catching=Scale(k.Catching),BreakTackle=Scale(k.BreakTackle),Tackling=Scale(k.Tackling),RunBlocking=Scale(k.RunBlocking),PassBlocking=Scale(k.PassBlocking),BallCarrying=Scale(k.BallCarrying),Acceleration=Scale(k.Acceleration),Leadership=Scale(k.Leadership),Discipline=Scale(k.Discipline),Jumping=Scale(k.Jumping),KickPower=Scale(k.KickPower),KickAccuracy=Scale(k.KickAccuracy)}};
            return ProPlayerRatingService.Refresh(new(football,Math.Clamp(18+original.ClassYear+(original.HasRedshirted?1:0),20,24),0,false){RatingSource="College draft class",ExternalId="college-"+p.PlayerId},true,true);
        }).ToList();
    }
}
public sealed record ProImportedClass(int Year,int SourceYear,string Source,string Fingerprint,int Players,int Fillers);
public static class ProDraftClassImport
{
    public static void RequireOpen(ProFranchise s)
    {
        if(s.Phase!=ProPhase.Offseason||s.OffseasonStep is not (OffseasonStep.FreeAgency or OffseasonStep.Draft)||s.DraftIndex!=0||s.DraftHistory.Any(d=>d.Year==s.Year))throw new InvalidOperationException("Import during offseason free agency, before the first draft selection.");
    }
    public static (List<ProPlayer> Imported,List<ProPlayer> Fillers,string Fingerprint) Preview(ProFranchise s,PlayerExport file)
    {
        RequireOpen(s);var imported=ProPlayerImport.Convert(file,s.Year);
        var used=s.Players.Select(p=>p.Football.PlayerId).Concat(s.DraftHistory.Select(d=>d.Player.Football.PlayerId)).ToHashSet();
        if(imported.Any(p=>used.Contains(p.Football.PlayerId)))throw new InvalidDataException("This class contains players already in this franchise or its draft history.");
        var fingerprint=System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(PlayerExportService.Serialize(file with{Players=file.Players.OrderBy(p=>p.PlayerId).ToList()}))));
        if(s.ImportedDraftClass is {} current&&current.Year==s.Year&&current.Fingerprint==fingerprint)throw new InvalidOperationException("This class is already imported for this draft.");
        var pool=s.Prospects.Where(p=>p.RatingSource!="College draft class").ToList();if(pool.Count<Math.Max(0,320-imported.Count))pool=ProDraft.Generate(s).Prospects;
        var ids=imported.Select(p=>p.Football.PlayerId).Concat(used).ToHashSet();
        var fillers=pool.Where(p=>!ids.Contains(p.Football.PlayerId)).Take(Math.Max(0,320-imported.Count)).ToList();
        return(imported,fillers,fingerprint);
    }
    public static ProFranchise Apply(ProFranchise s,PlayerExport file)
    {
        var p=Preview(s,file);var combined=p.Imported.Concat(p.Fillers).Select(x=>x with{RookieYear=s.Year}).ToList();
        var next=s with{Prospects=combined,Scouting=[],DraftBoard=[],ImportedDraftClass=new(s.Year,file.Year,file.Source,p.Fingerprint,p.Imported.Count,p.Fillers.Count)};
        return ProDraft.AutoScout(ProEconomy.Log(next,$"Imported {p.Imported.Count} college prospects for the {s.Year} draft; {p.Fillers.Count} generated prospects complete the pool."));
    }
}
