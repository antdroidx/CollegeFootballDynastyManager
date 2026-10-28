using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class PlayerExchangeTests
{
    static ProFranchise New()=>ProDraft.Generate(ProLeagueService.Create("Seattle Seahawks") with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency});
    static PlayerExport File(ProFranchise s)=>new(1,"College Football Dynasty Manager",2027,s.Prospects.Take(3).Select((p,i)=>p.Football with{PlayerId=Guid.NewGuid(),FullName="College Prospect "+i,TeamName="College",ClassYear=4,OverallRating=90,PotentialRating=95,Skills=new(){PassingPower=95,PassingAccuracy=95,Catching=95,Tackling=95,RunBlocking=95,PassBlocking=95,BreakTackle=95,BallCarrying=95,Acceleration=95,Leadership=95,Discipline=95,Jumping=95,KickPower=95,KickAccuracy=95}}).ToList());
    [Fact] public void FileRoundTripTranslatesAllAttributesAndPreservesIdentityAppearance()
    {
        var s=New();var file=File(s);var parsed=PlayerExportService.Parse(PlayerExportService.Serialize(file));var players=ProPlayerImport.Convert(parsed,2027);var reversed=ProPlayerImport.Convert(parsed with{Players=parsed.Players.AsEnumerable().Reverse().ToList()},2027);
        foreach(var p in players){var original=file.Players.Single(x=>x.PlayerId==p.Football.PlayerId);Assert.Equal(original.FullName,p.Football.FullName);Assert.Equal(original.Appearance,p.Football.Appearance);Assert.Equal(p,reversed.Single(x=>x.Football.PlayerId==p.Football.PlayerId));Assert.NotEqual(original.OverallRating,p.Football.OverallRating);Assert.Equal(PlayerRatingService.CalculateOverall(p.Football),p.Football.OverallRating);Assert.InRange(p.Football.Skills!.KickAccuracy,30,95);Assert.NotEqual(95,p.Football.Skills.Leadership);Assert.Equal(22,p.Age);}
    }
    [Fact] public void ImportReplacesOnlyProspectsAndPreservesFinancialsPicksAndCareer()
    {
        var s=New();var next=ProDraftClassImport.Apply(s,File(s));Assert.Equal(320,next.Prospects.Count);Assert.Equal(3,next.ImportedDraftClass!.Players);Assert.Equal(317,next.ImportedDraftClass.Fillers);Assert.Equal(2027,next.ImportedDraftClass.SourceYear);Assert.Equal(s.Year,next.ImportedDraftClass.Year);
        Assert.Equal(s.Players,next.Players);Assert.Equal(s.Contracts,next.Contracts);Assert.Equal(s.Picks,next.Picks);Assert.Equal(s.DeadCap,next.DeadCap);Assert.Equal(s.Career,next.Career);Assert.Equal(s.DraftHistory,next.DraftHistory);Assert.Empty(next.DraftBoard);
    }
    [Fact] public void StartedDraftDuplicatePlayersAndDuplicateFilesAreRejectedAtomically()
    {
        var s=New();var file=File(s);var next=ProDraftClassImport.Apply(s,file);
        Assert.Throws<InvalidOperationException>(()=>ProDraftClassImport.Apply(next,file));Assert.Throws<InvalidOperationException>(()=>ProDraftClassImport.Apply(s with{DraftIndex=1},file));Assert.Throws<InvalidOperationException>(()=>ProDraftClassImport.Apply(s with{Phase=ProPhase.RegularSeason},file));
        Assert.Throws<InvalidDataException>(()=>ProDraftClassImport.Apply(s,file with{Players=[s.Players[0].Football]}));Assert.Throws<InvalidDataException>(()=>PlayerExportService.Parse("{}"));Assert.Throws<InvalidDataException>(()=>PlayerExportService.Parse("not json"));Assert.Throws<InvalidDataException>(()=>PlayerExportService.Serialize(file with{FormatVersion=2}));Assert.Throws<InvalidDataException>(()=>PlayerExportService.Serialize(file with{Players=[file.Players[0],file.Players[0]]}));
    }
    [Fact] public async Task ImportPersistsAndDraftedCollegePlayerReceivesNormalRookieDeal()
    {
        var s=New();var file=File(s);s=ProDraftClassImport.Apply(s,file);var id=file.Players[0].PlayerId;
        s=s with{OffseasonStep=OffseasonStep.Draft,Contracts=[],SalaryCap=2_000_000_000};var pick=ProDraft.Current(s)!;
        s=ProDraft.Pick(s,id);Assert.Contains(s.Players,p=>p.Football.PlayerId==id&&p.Football.TeamName==pick.Owner);Assert.Equal(4,s.Contracts.Last(c=>c.PlayerId==id).Years.Count);
        var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());try{var store=new ProSaveStore(dir);await store.Save(s);var loaded=await store.Load(s.Id);Assert.Equal(s.ImportedDraftClass,loaded.ImportedDraftClass);Assert.Contains(loaded.DraftHistory,d=>d.Player.Football.PlayerId==id);}finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
    [Theory][InlineData(StaffRole.HeadCoach)][InlineData(StaffRole.OffensiveCoordinator)][InlineData(StaffRole.DefensiveCoordinator)]
    public void LeagueClassSetupDoesNotChangeCoachingAuthority(StaffRole role)
    {var s=New();s=s with{Career=s.Career! with{Role=role}};var next=ProDraftClassImport.Apply(s,File(s));Assert.Equal(s.Career,next.Career);}
    [Fact] public async Task ExchangeDeviceFixturePassesProductionLoader()
    {
        var s=ProDraft.Generate(New() with{Year=2027,Week=1,Games=[],Name="Exchange Pro QA"});
        var dir=Path.Combine(Path.GetTempPath(),"exchange-"+Guid.NewGuid());
        try{var store=new ProSaveStore(dir);await store.Save(s);var loaded=await store.Load(s.Id);Assert.Equal(32,loaded.League.Teams.Count);Assert.Equal(System.Text.Json.JsonSerializer.Serialize(s.Contracts),System.Text.Json.JsonSerializer.Serialize(loaded.Contracts));Assert.Equal(2027,loaded.Year);
            if(Environment.GetEnvironmentVariable("PRO_COORDINATOR_UI_FIXTURES") is string output){Directory.CreateDirectory(output);using var file=System.IO.File.Create(Path.Combine(output,"pro-staff-exchange-ui-fixture.json.gz"));using var zip=new System.IO.Compression.GZipStream(file,System.IO.Compression.CompressionLevel.SmallestSize);System.Text.Json.JsonSerializer.Serialize(zip,s,new System.Text.Json.JsonSerializerOptions{PropertyNamingPolicy=System.Text.Json.JsonNamingPolicy.CamelCase});}}
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }

    [Fact] public void SmallImportedClassCanCompleteSevenRoundsWithNormalUndraftedRemainder()
    {
        var s=New();var file=File(s);s=ProDraftClassImport.Apply(s,file);var classIds=s.Prospects.Select(p=>p.Football.PlayerId).ToHashSet();
        s=ProDraft.AutoUntilUser(s with{OffseasonStep=OffseasonStep.Draft},true);
        Assert.Null(ProDraft.Current(s));Assert.Equal(224,s.DraftHistory.Count(d=>d.Year==s.Year));Assert.Equal(96,s.Prospects.Count);
        Assert.All(s.DraftHistory.Where(d=>d.Year==s.Year),d=>Assert.Contains(d.Player.Football.PlayerId,classIds));
        Assert.Equal(s.Players.Count,s.Players.Select(p=>p.Football.PlayerId).Distinct().Count());
        Assert.Equal(3,s.ImportedDraftClass!.Players);
    }

    [Fact] public void FullCollegeSizedClassRetainsAllProspectsWithoutFillerAndCompletesDraft()
    {
        var s=New();var samples=s.Prospects.ToArray();var file=new PlayerExport(1,"College Football Dynasty Manager",2027,Enumerable.Range(0,2500).Select(i=>samples[i%samples.Length].Football with{PlayerId=Guid.NewGuid(),FullName="College graduate "+i,TeamName="College "+(i%50),ClassYear=4}).ToList());
        var json=PlayerExportService.Serialize(file);Assert.True(System.Text.Encoding.UTF8.GetByteCount(json)<PlayerExportService.MaxBytes);
        s=ProDraftClassImport.Apply(s,PlayerExportService.Parse(json));Assert.Equal(2500,s.Prospects.Count);Assert.Equal(0,s.ImportedDraftClass!.Fillers);
        s=ProDraft.AutoUntilUser(s with{OffseasonStep=OffseasonStep.Draft},true);Assert.Null(ProDraft.Current(s));Assert.Equal(224,s.DraftHistory.Count(d=>d.Year==s.Year));Assert.Equal(2276,s.Prospects.Count);
    }

}
