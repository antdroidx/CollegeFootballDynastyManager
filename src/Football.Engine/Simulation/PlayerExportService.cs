using System.Text.Json;
using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;
public static class PlayerExportService
{
    public const int MaxBytes=16_000_000;
    static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true,MaxDepth=32};
    public static void Validate(PlayerExport e)
    {
        if(e.FormatVersion!=1||string.IsNullOrWhiteSpace(e.Source)||e.Source.Length>200||e.Year<1900||e.Year>9999||e.Players is null||e.Players.Count is <1 or >10000)throw new InvalidDataException("Choose a supported, non-empty draft-class file (format 1).");
        if(e.Players.Any(p=>p is null||p.PlayerId==Guid.Empty||string.IsNullOrWhiteSpace(p.FullName)||p.FullName.Length>120||string.IsNullOrWhiteSpace(p.TeamName)||p.TeamName.Length>160||!Enum.IsDefined(p.Position))||e.Players.Select(p=>p.PlayerId).Distinct().Count()!=e.Players.Count)throw new InvalidDataException("The class contains invalid or duplicate players.");
        foreach(var p in e.Players)
        {
            var values=new[]{p.OverallRating,p.PotentialRating,p.SpeedRating,p.StrengthRating,p.AgilityRating,p.AwarenessRating};
            if(values.Any(x=>x is <1 or >99)||p.ClassYear is <0 or >6)throw new InvalidDataException("Player ratings or eligibility are outside the supported range.");
            if(p.Skills is {} k && new[]{k.PassingPower,k.PassingAccuracy,k.Catching,k.BreakTackle,k.Tackling,k.RunBlocking,k.PassBlocking,k.BallCarrying,k.Acceleration,k.Leadership,k.Discipline,k.Jumping,k.KickPower,k.KickAccuracy}.Any(x=>x is <0 or >99))throw new InvalidDataException("Player attributes are outside the supported range.");
        }
    }
    public static string Serialize(PlayerExport e){Validate(e);return JsonSerializer.Serialize(e,Json);}
    public static PlayerExport Parse(string text)
    {
        if(System.Text.Encoding.UTF8.GetByteCount(text)>MaxBytes)throw new InvalidDataException("Draft-class files must be under 16 MB.");
        try{var e=JsonSerializer.Deserialize<PlayerExport>(text,Json)??throw new InvalidDataException("Empty draft-class file.");Validate(e);return e;}
        catch(JsonException){throw new InvalidDataException("This is not a valid draft-class JSON file.");}
    }
    public static PlayerExport Latest(DynastyState s)
    {
        var result=s.LatestProDraftClass;
        if(s.Phase==SeasonPhase.TransferPortal&&s.TransferPortalEntryWindowSeasonYear!=s.SeasonYear)result=OffseasonPlayerLifecycleService.EnterTransferPortal(s).LatestProDraftClass;
        if(result is null||result.Players.Count==0)throw new InvalidOperationException("A full draft class becomes available when postseason departures are processed. Older summary-only departures cannot be exported with full attributes.");
        Validate(result);return result;
    }
}
