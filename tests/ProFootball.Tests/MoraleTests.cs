using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class MoraleTests
{
    [Fact] public void WeeklyTalkHasMixedReactionsAndCannotBeSpammed()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var changed=ProMorale.Talk(s,CoachTalkTone.Praise,CoachTalkAudience.Team);
        var talk=Assert.Single(changed.WeeklyTalks);Assert.Contains(talk.Talk.Reactions,r=>r.Change>0);Assert.Contains(talk.Talk.Reactions,r=>r.Change<0);
        Assert.False(ProMorale.TalkAvailable(changed));Assert.Throws<InvalidOperationException>(()=>ProMorale.Talk(changed,CoachTalkTone.Encourage,CoachTalkAudience.Offense));
        Assert.True(ProMorale.TalkAvailable(changed with{TransactionTick=changed.TransactionTick+1}));
        var restored=System.Text.Json.JsonSerializer.Deserialize<ProFranchise>(System.Text.Json.JsonSerializer.Serialize(changed))!;Assert.False(ProMorale.TalkAvailable(restored));
    }
    [Fact] public void CoordinatorCanOnlyAddressOwnUnit()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",role:StaffRole.OffensiveCoordinator);var changed=ProMorale.Talk(s,CoachTalkTone.Challenge,CoachTalkAudience.Team);
        var talk=Assert.Single(changed.WeeklyTalks).Talk;Assert.Equal(CoachTalkAudience.Offense,talk.Audience);
        Assert.All(talk.Reactions,r=>Assert.True(GameDayCoachingService.IsOffense(s.Players.Single(p=>p.Football.PlayerId==r.PlayerId).Football.Position)));
    }
    [Fact] public void SigningReactionFadesAndRepeatedRefreshDoesNotDuplicateIt()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var source=s.Players.First();
        ProPlayer Player(int seed,Position pos,int ovr) {var bytes=new byte[16];bytes[0]=(byte)seed;bytes[1]=2;return source with{Football=source.Football with{PlayerId=new Guid(bytes),TeamName=s.UserTeam,Position=pos,OverallRating=ovr,DepthChartOrder=1},Morale=60,Age=24,Retired=false,PracticeSquad=false};}
        var starter=Player(1,Position.QB,75);var signing=Player(2,Position.QB,90);s=s with{Players=[starter,signing],Contracts=[]};
        var changed=ProMorale.Signing(s,signing.Football.PlayerId,s.UserTeam);Assert.Equal(58,changed.Players.Single(p=>p.Football.PlayerId==starter.Football.PlayerId).Morale);
        changed=ProMorale.Signing(changed,signing.Football.PlayerId,s.UserTeam);Assert.Equal(58,changed.Players.Single(p=>p.Football.PlayerId==starter.Football.PlayerId).Morale);
        for(var tick=1;tick<=16;tick++)changed=ProMorale.Advance(changed with{TransactionTick=tick});
        Assert.All(changed.Players,p=>Assert.Equal(60,p.Morale));Assert.Empty(changed.MoraleEffects);
        Assert.Same(changed,ProMorale.Advance(changed));
    }
    [Fact] public void SevereSustainedContractConcernCanCauseTradeRequest()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var original=s.Players.First(p=>p.Football.TeamName==s.UserTeam);var bytes=original.Football.PlayerId.ToByteArray();bytes[1]=1;var id=new Guid(bytes);
        for(var seed=0;seed<256;seed++){bytes[0]=(byte)seed;id=new Guid(bytes);if(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"contract-review/{s.Year}/{id}"))[0]<38)break;}
        var p=original with{Football=original.Football with{PlayerId=id,OverallRating=95},Age=28,Experience=8,RookieYear=s.Year-8,Morale=25,PracticeSquad=false,Retired=false};
        s=s with{Phase=ProPhase.RegularSeason,Week=9,Players=[p],Contracts=[new(id,s.UserTeam,s.Year-3,5,0,[new(s.Year,1_000_000,0),new(s.Year+1,1_000_000,0)])]};
        for(var tick=1;tick<=8;tick++)s=ProMorale.Advance(s with{TransactionTick=tick,Week=8+tick});
        Assert.True(s.Players.Single().TradeRequested);Assert.Contains(s.Transactions,t=>t.Contains("requested a trade"));
        var settled=ProMorale.ContractAgreement(s,id);Assert.False(settled.Players.Single().TradeRequested);Assert.Equal(0,settled.Players.Single().ContractConcernWeeks);
    }
}
