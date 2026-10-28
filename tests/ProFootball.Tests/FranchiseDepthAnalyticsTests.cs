using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class FranchiseDepthAnalyticsTests
{
    static ProFranchise New()=>ProLeagueService.Create("Seattle Seahawks");
    [Fact] public void CombineImprovesStaffReportsWithoutExtraBudgetOrMutation()
    {
        var s=ProDraft.Generate(New() with {Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency});
        var old=s.Scouting.ToArray();var combine=ProDraft.AutoScout(s with{OffseasonStep=OffseasonStep.Draft});
        Assert.Equal(old.Length,combine.Scouting.Count);Assert.All(combine.Scouting,r=>Assert.InRange(r.Confidence,60,95));
        Assert.Contains(combine.Scouting,r=>r.Confidence>old.Single(o=>o.PlayerId==r.PlayerId&&o.Team==r.Team).Confidence);
        Assert.Equal(combine.Scouting,ProDraft.AutoScout(combine).Scouting);Assert.Equal(old,s.Scouting);
    }
    [Fact] public void ScoutingQualityUsesStaffAndRemainsUncertain()
    {
        var s=ProDraft.Generate(New() with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Draft});var player=s.Prospects[0];var id=s.League.Teams.Single(t=>t.Name==s.UserTeam).Id;
        var weak=s with{Coaches=s.Coaches.Select(c=>c.TeamId==id?c with{Development=40}:c).ToList()};
        var strong=s with{Coaches=s.Coaches.Select(c=>c.TeamId==id?c with{Development=95}:c).ToList()};
        Assert.True(ProScouting.Evaluate(strong,s.UserTeam,player).Confidence>ProScouting.Evaluate(weak,s.UserTeam,player).Confidence);
        Assert.Equal(ProScouting.Evaluate(strong,s.UserTeam,player),ProScouting.Evaluate(strong,s.UserTeam,player));
    }
    [Fact] public void StaffDevelopmentIsOncePerPlayedSeasonAndLeavesContractsAlone()
    {
        var s=New();var game=s.Games.First(g=>g.Phase==ProPhase.RegularSeason&&g.Football.InvolvesTeam(s.UserTeam));
        s=s with{Games=[game with{Football=game.Football with{HasPlayed=true,HomeScore=21,AwayScore=7}}]};
        var developed=ProStaff.DevelopSeason(s);Assert.Equal(developed.Coaches,ProStaff.DevelopSeason(developed).Coaches);
        Assert.Contains(developed.Coaches,c=>c.LastDevelopmentYear==s.Year);Assert.Equal(s.Contracts,developed.Contracts);Assert.Equal(s.Career,developed.Career);
        Assert.All(developed.Coaches,c=>{Assert.InRange(c.Development,40,95);Assert.InRange(c.Rating,40,95);});
    }
    [Fact] public void DraftOutcomeUsesDraftingClubAndSameClassRoundPeers()
    {
        var s=New();var first=s.Players.First(p=>p.Football.TeamName==s.UserTeam);var second=s.Players.First(p=>p.Football.TeamName!=s.UserTeam);
        var a=first with{Football=first.Football with{OverallRating=70}};var b=second with{Football=second.Football with{OverallRating=70}};
        s=s with{DraftHistory=[new(2026,1,1,s.UserTeam,a),new(2026,1,2,second.Football.TeamName,b)],Players=[a with{Football=a.Football with{TeamName="Traded club",OverallRating=80}},b with{Football=b.Football with{OverallRating=72}}]};
        var row=Assert.Single(ProAnalytics.DraftOutcomes(s,s.UserTeam));Assert.Equal(10,row.TeamGrowth);Assert.Equal(6,row.LeagueGrowth);Assert.Equal(70,row.DraftOverall);
    }
    [Fact] public void CareerRecordsExcludePreseasonAndDuplicateArchives()
    {
        var s=New();var g=s.Games.First(g=>g.Phase==ProPhase.RegularSeason);var p=s.Players[0].Football;
        var game=g with{Football=g.Football with{HasPlayed=true,HomeScore=10,AwayScore=7,PlayerStats=[new(){PlayerId=p.PlayerId,PlayerName=p.FullName,TeamName=s.UserTeam,PassYards=250}]}};
        s=s with{Games=[game,game with{Phase=ProPhase.Preseason,Football=game.Football with{GameId="pre"}}],History=[new(2026,s.UserTeam,[game])]};
        Assert.Equal(250,Assert.Single(ProAnalytics.CareerRecords(s,s.UserTeam)).Total);
    }
    [Fact] public void WeeklyReviewAndRaceAreFactualIdempotentAndWaitForCompleteWeek()
    {
        var s=New();var games=s.Games.Where(g=>g.Phase==ProPhase.RegularSeason&&g.Football.Week==14).ToList();
        s=s with{Games=games.Select(g=>g with{Football=g.Football with{HasPlayed=true,HomeScore=21,AwayScore=10}}).ToList()};
        var news=ProNews.Refresh(s);Assert.Single(news.News.Where(n=>n.Category=="Weekly review"));Assert.Equal(2,news.News.Count(n=>n.Category=="Playoff race"));
        Assert.Equal(news.News,ProNews.Refresh(news).News);Assert.All(news.News.Where(n=>n.Category=="Playoff race"),n=>Assert.Contains("projected",n.Body));
        s=s with{Games=s.Games.Select((g,i)=>i==0?g with{Football=g.Football with{HasPlayed=false,HomeScore=null,AwayScore=null}}:g).ToList()};
        Assert.DoesNotContain(ProNews.Refresh(s).News,n=>n.Category is "Weekly review" or "Playoff race");
    }
    [Fact] public async Task ManagementDeviceFixturesPassSaveLoader()
    {
        var s=New();var own=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&!p.Retired);
        s=s with {Name="Roadmap QA",Phase=ProPhase.RegularSeason,Week=15,Games=s.Games.Select(g=>g.Phase==ProPhase.RegularSeason&&g.Football.Week<=14?g with{Football=g.Football with{HasPlayed=true,HomeScore=21,AwayScore=10,PlayerStats=g.Football.InvolvesTeam(s.UserTeam)?[new(){PlayerId=own.Football.PlayerId,PlayerName=own.Football.FullName,TeamName=s.UserTeam,PassYards=240}]:[]}}:g).ToList(),DraftHistory=[new(s.Year-1,1,12,s.UserTeam,own with{Football=own.Football with{OverallRating=Math.Max(40,own.Football.OverallRating-4)}})]};
        s=ProNews.Refresh(s);
        var draft=ProDraft.AutoUntilUser(ProDraft.Generate(s with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Draft}));
        var retention=s with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Retention,Contracts=s.Contracts.Select(c=>c.Active&&c.PlayerId==own.Football.PlayerId?c with{Years=c.Years.Where(y=>y.Year<=s.Year).ToList()}:c).ToList()};
        var root=Path.Combine(Path.GetTempPath(),"pro-roadmap-"+Guid.NewGuid());
        try
        {
            var store=new ProSaveStore(root);
            foreach(var fixture in new[]{s,draft,retention}){await store.Save(fixture);var loaded=await store.Load(fixture.Id);Assert.Equal(fixture.Career,loaded.Career);Assert.Equal(fixture.Phase,loaded.Phase);Assert.Equal(fixture.Prospects.Count,loaded.Prospects.Count);}
            if(Environment.GetEnvironmentVariable("PRO_COORDINATOR_UI_FIXTURES") is string dir)
            {
                Directory.CreateDirectory(dir);using var file=File.Create(Path.Combine(dir,"pro-staff-roadmap-ui-fixture.json.gz"));using var gzip=new System.IO.Compression.GZipStream(file,System.IO.Compression.CompressionLevel.SmallestSize);
                System.Text.Json.JsonSerializer.Serialize(gzip,new{regular=s,draft,retention},new System.Text.Json.JsonSerializerOptions{PropertyNamingPolicy=System.Text.Json.JsonNamingPolicy.CamelCase});
            }
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }

}
