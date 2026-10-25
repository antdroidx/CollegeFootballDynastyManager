using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Core;

public static class ProMorale
{
    public static string Personality(ProPlayer p) => (p.Football.PlayerId.ToByteArray()[1]%3) switch {0=>"Team focused",1=>"Ambitious",_=>"Sensitive to change"};
    static int Memory(ProPlayer p)=>Personality(p)=="Sensitive to change"?4:Personality(p)=="Ambitious"?2:1;
    public static bool TalkAvailable(ProFranchise s)=>s.Career?.Unemployed!=true&&!s.WeeklyTalks.Any(t=>t.Year==s.Year&&t.Tick==s.TransactionTick&&t.Talk.TeamName==s.UserTeam);
    static ProFranchise Effects(ProFranchise s,IReadOnlyDictionary<Guid,(int Delta,string Reason)> changes)
    {
        var effects=s.MoraleEffects.ToList();var active=effects.Select(e=>(e.PlayerId,e.Reason)).ToHashSet();
        var players=s.Players.Select(p=>
        {
            if(!changes.TryGetValue(p.Football.PlayerId,out var change)||active.Contains((p.Football.PlayerId,change.Reason)))return p;
            var value=Math.Clamp(p.Morale+change.Delta,10,95);var actual=value-p.Morale;if(actual==0)return p;
            effects.Add(new(p.Football.PlayerId,change.Reason,actual,s.TransactionTick+Memory(p)));return p with{Morale=value};
        }).ToList();return s with{Players=players,MoraleEffects=effects.TakeLast(4000).ToList()};
    }
    static ProFranchise Effect(ProFranchise s,Guid id,int delta,string reason)=>Effects(s,new Dictionary<Guid,(int,string)>{{id,(delta,reason)}});
    public static ProFranchise Talk(ProFranchise s,CoachTalkTone tone,CoachTalkAudience audience)
    {
        if(!TalkAvailable(s))throw new InvalidOperationException("Your weekly coach talk has been used. Advance the calendar before speaking again.");
        var role=ProCareer.Role(s);if(role!=StaffRole.HeadCoach)audience=role==StaffRole.OffensiveCoordinator?CoachTalkAudience.Offense:CoachTalkAudience.Defense;
        var roster=s.Players.Where(p=>p.Football.TeamName==s.UserTeam&&!p.Retired&&(audience==CoachTalkAudience.Team||GameDayCoachingService.IsOffense(p.Football.Position)==(audience==CoachTalkAudience.Offense)&&p.Football.Position!=Position.K)).Select(p=>p.Football).ToArray();
        var form=s.Games.Where(g=>g.Phase==ProPhase.RegularSeason&&g.Football.HasPlayed&&g.Football.InvolvesTeam(s.UserTeam)).OrderByDescending(g=>g.Football.Week).Take(3).Sum(g=>g.Football.WinnerTeamName==s.UserTeam?1:-1);
        var talk=GameDayCoachingService.CreateTalk(roster,s.UserTeam,tone,s.Year,s.Week,0,s.TransactionTick,form,0) with{Audience=audience};
        s=Effects(s,talk.Reactions.ToDictionary(r=>r.PlayerId,r=>(r.Change,$"Weekly coach talk {s.Year}/{s.TransactionTick}")));
        return s with{WeeklyTalks=s.WeeklyTalks.Append(new(s.Year,s.Week,s.Phase,s.TransactionTick,talk)).TakeLast(200).ToList()};
    }
    public static ProFranchise Signing(ProFranchise s,Guid id,string team)
    {
        var signed=s.Players.Single(p=>p.Football.PlayerId==id);
        var changes=new Dictionary<Guid,(int,string)>();
        foreach(var p in s.Players.Where(p=>p.Football.TeamName==team&&!p.Retired&&p.Football.PlayerId!=id))
        {
            var threatened=p.Football.Position==signed.Football.Position&&p.Football.DepthChartOrder<=2&&signed.Football.OverallRating>p.Football.OverallRating;
            var delta=threatened?-2:Personality(p)=="Team focused"?1:Personality(p)=="Sensitive to change"?-1:0;
            changes[p.Football.PlayerId]=(delta,threatened?"New signing threatens role":"Reaction to team signing");
        }
        s=Effects(s,changes);
        return Effect(s,id,3,"Signed a new contract");
    }
    public static ProFranchise ContractAgreement(ProFranchise s,Guid id)
    {
        s=s with{Players=s.Players.Select(p=>p.Football.PlayerId==id?p with{ContractConcernWeeks=0,TradeRequested=false}:p).ToList()};
        return Effect(s,id,4,"Contract agreement");
    }
    public static ProFranchise Advance(ProFranchise s)
    {
        if(s.LastMoraleTick==s.TransactionTick)return s;
        var effects=new List<ProMoraleEffect>();var players=s.Players.ToDictionary(p=>p.Football.PlayerId);
        foreach(var e in s.MoraleEffects)
        {
            if(!players.TryGetValue(e.PlayerId,out var p)||p.Retired)continue;
            if(e.NextDecayTick>s.TransactionTick){effects.Add(e);continue;}
            var step=Math.Sign(e.RemainingChange);players[e.PlayerId]=p with{Morale=Math.Clamp(p.Morale-step,10,95)};
            if(e.RemainingChange!=step)effects.Add(e with{RemainingChange=e.RemainingChange-step,NextDecayTick=s.TransactionTick+Memory(p)});
        }
        var contracts=s.Contracts.Where(c=>c.Active).GroupBy(c=>c.PlayerId).ToDictionary(g=>g.Key,g=>g.Last());var transactions=new List<string>();
        foreach(var p in players.Values.Where(p=>!p.Retired&&!p.PracticeSquad&&p.Age>=25&&p.Football.OverallRating>=75&&Personality(p)=="Ambitious").ToArray())
        {
            contracts.TryGetValue(p.Football.PlayerId,out var c);var salary=c?.Years.FirstOrDefault(y=>y.Year==s.Year)?.Salary??0;
            var unhappy=salary>0&&salary<ProEconomy.MarketSalary(p)*.6&&c!.LastChangedYear!=s.Year;
            var weeks=unhappy?p.ContractConcernWeeks+1:Math.Max(0,p.ContractConcernWeeks-1);
            var request=p.TradeRequested||weeks>=6&&p.Morale<=30;
            players[p.Football.PlayerId]=p with{ContractConcernWeeks=weeks,TradeRequested=request,Morale=unhappy?Math.Max(10,p.Morale-1):p.Morale};
            if(request&&!p.TradeRequested)transactions.Add($"{s.Year} W{s.Week} · {p.Football.FullName} requested a trade after sustained contract dissatisfaction.");
        }
        return s with{Players=s.Players.Select(p=>players[p.Football.PlayerId]).ToList(),MoraleEffects=effects,LastMoraleTick=s.TransactionTick,Transactions=s.Transactions.Concat(transactions).TakeLast(250).ToList()};
    }
}
