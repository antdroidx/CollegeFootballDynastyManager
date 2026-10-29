using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
using System.Text.Json;
using Xunit;
namespace ProFootball.Tests;

public class RookieParticipationTests
{
    static GameSnapEvent Snap(string offense,string defense,SnapOutcome outcome,Guid[] attack,Guid[] defend) => new()
    {
        OffenseTeamName=offense,DefenseTeamName=defense,OffensivePlayId="test",DefensivePlayId="test",
        Outcome=outcome,OffensePersonnel=attack,DefensePersonnel=defend
    };
    static ProRookieSeasonUsage Usage(Guid id,int year,int played,int total,int games=17) => new(id,year)
    {
        OffensiveSnaps=played,TeamOffensiveSnaps=total,DefensiveSnaps=played,TeamDefensiveSnaps=total,
        RecordedGames=Enumerable.Range(1,games).Select(i=>year+"-"+i).ToList()
    };
    [Fact] public void RecordCountsRecordedScrimmagePersonnelIncludingZeroParticipationAndIsIdempotent()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");
        var game=s.Games.First(g=>g.Phase==ProPhase.RegularSeason&&g.Football.InvolvesTeam(s.UserTeam));
        var other=game.Football.HomeTeamName==s.UserTeam?game.Football.AwayTeamName:game.Football.HomeTeamName;
        var a=s.Players.First(p=>p.Football.TeamName==s.UserTeam) with{RookieYear=s.Year,DraftOverall=1};
        var inactive=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Football.PlayerId!=a.Football.PlayerId) with{RookieYear=s.Year,DraftOverall=2,Reserve=true};
        var later=a with{Football=a.Football with{PlayerId=Guid.NewGuid()},DraftOverall=33};
        s=s with{Phase=ProPhase.RegularSeason,Players=[a,inactive,later]};
        var id=a.Football.PlayerId;
        GameSnapEvent[] snaps=[Snap(s.UserTeam,other,SnapOutcome.Run,[id,id],[]),
            Snap(s.UserTeam,other,SnapOutcome.Penalty,[id],[]),Snap(s.UserTeam,other,SnapOutcome.Kickoff,[id],[]),
            Snap(other,s.UserTeam,SnapOutcome.Completion,[],[id])];
        var recorded=ProRookieParticipation.Record(s,game,snaps);
        Assert.Equal(2,recorded.RookieParticipation.Count);
        var own=recorded.RookieParticipation.Single(u=>u.PlayerId==id);
        Assert.Equal(1,own.OffensiveSnaps);Assert.Equal(1,own.DefensiveSnaps);
        Assert.Equal(1,own.TeamOffensiveSnaps);Assert.Equal(1,own.TeamDefensiveSnaps);
        Assert.Equal(0,own.UnknownOffenseGames);Assert.Equal(0,own.UnknownDefenseGames);
        var zero=recorded.RookieParticipation.Single(u=>u.PlayerId==inactive.Football.PlayerId);
        Assert.Equal(0,zero.OffensiveSnaps);Assert.Equal(1,zero.TeamOffensiveSnaps);
        var retry=ProRookieParticipation.Record(recorded,game,snaps);
        Assert.Equal(own,retry.RookieParticipation.Single(u=>u.PlayerId==id));
        Assert.Empty(ProRookieParticipation.Record(s,game with{Phase=ProPhase.Preseason},snaps).RookieParticipation);
        Assert.Empty(ProRookieParticipation.Record(s,game with{Phase=ProPhase.Playoffs},snaps).RookieParticipation);
        Assert.Same(s.Contracts,recorded.Contracts);Assert.Same(s.Players,recorded.Players);
        var loaded=JsonSerializer.Deserialize<ProFranchise>(JsonSerializer.Serialize(recorded))!;
        Assert.Equal(own.OffensiveSnaps,loaded.RookieParticipation.Single(u=>u.PlayerId==id).OffensiveSnaps);
    }
    [Fact] public void RealRegularSeasonFinalRecordsOnceAndOldSaveWithoutLedgerStaysUnknown()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");
        var player=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Football.Position==Position.QB);
        s=s with{Phase=ProPhase.RegularSeason,Players=s.Players.Select(p=>p==player?p with{RookieYear=s.Year,DraftOverall=1}:p).ToList()};
        var game=s.Games.First(g=>g.Phase==s.Phase&&g.Football.Week==s.Week&&g.Football.InvolvesTeam(s.UserTeam));
        var session=ProLeagueService.Start(s,game,GameDayMode.Simulate);
        GameDayEngine.SimulateToEnd(session);
        var final=ProLeagueService.Finish(s,session);
        var usage=final.RookieParticipation.Single(u=>u.PlayerId==player.Football.PlayerId);
        Assert.True(usage.TeamOffensiveSnaps>0);Assert.Single(usage.RecordedGames);
        Assert.Same(final,ProLeagueService.Finish(final,session));
        var legacy=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(final))!.AsObject();
        legacy.Remove(nameof(ProFranchise.RookieParticipation));
        var loaded=JsonSerializer.Deserialize<ProFranchise>(legacy.ToJsonString())!;
        Assert.Empty(loaded.RookieParticipation);
        Assert.Equal(final.Contracts.Sum(c=>ProEconomy.CapHit(c,s.Year)),loaded.Contracts.Sum(c=>ProEconomy.CapHit(c,s.Year)));
        Assert.False(ProRookieParticipation.MeetsPlayingTimeTier(loaded.RookieParticipation,player.Football.PlayerId,s.Year,true));
    }
    [Fact] public void TwoFullyObservedSeasonsCanProveTheTwoSeasonThresholdWithoutInventingAnOlderYear()
    {
        var id=Guid.NewGuid();var rows=new[]{Usage(id,2027,75,100),Usage(id,2028,75,100)};
        Assert.True(ProRookieParticipation.MeetsPlayingTimeTier(rows,id,2026,true));
        Assert.False(ProRookieParticipation.MeetsPlayingTimeTier(rows.Select(u=>u with{OffensiveSnaps=74}),id,2026,true));
        Assert.False(ProRookieParticipation.MeetsPlayingTimeTier(rows.Select(u=>u with{RecordedGames=u.RecordedGames.Take(16).ToList()}),id,2026,true));
    }
    [Fact] public void CumulativeThresholdUsesWeightedSnapsAndEverySeasonThresholdRequiresAllThreeYears()
    {
        var id=Guid.NewGuid();var rows=new[]{Usage(id,2026,90,100),Usage(id,2027,4,10),Usage(id,2028,4,10)};
        Assert.True(ProRookieParticipation.MeetsPlayingTimeTier(rows,id,2026,true));
        rows=[Usage(id,2026,50,100),Usage(id,2027,50,100),Usage(id,2028,50,100)];
        Assert.True(ProRookieParticipation.MeetsPlayingTimeTier(rows,id,2026,true));
        Assert.False(ProRookieParticipation.MeetsPlayingTimeTier(rows.Take(2),id,2026,true));
        Assert.False(ProRookieParticipation.MeetsPlayingTimeTier(rows.Select(u=>u with{UnknownOffenseGames=1}),id,2026,true));
        Assert.True(ProRookieParticipation.MeetsPlayingTimeTier(rows.Select(u=>u with{UnknownOffenseGames=1}),id,2026,false));
    }
    [Fact] public void FutureYearsOtherPlayersAndMissingDenominatorsCannotQualify()
    {
        var id=Guid.NewGuid();var rows=new[]{Usage(id,2026,1,0),Usage(id,2027,1,0),Usage(id,2028,1,0),Usage(id,2029,100,100)};
        Assert.False(ProRookieParticipation.MeetsPlayingTimeTier(rows,id,2026,true));
        Assert.False(ProRookieParticipation.MeetsPlayingTimeTier(rows,Guid.NewGuid(),2026,true));
        Assert.False(ProRookieParticipation.MeetsPlayingTimeTier([],id,2026,true));
    }
}
