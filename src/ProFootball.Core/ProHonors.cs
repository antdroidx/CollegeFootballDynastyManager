using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Core;
public static class ProHonors
{
    public static ProFranchise Publish(ProFranchise s)
    {
        if(s.Honors.Any(h=>h.Year==s.Year))return s;
        var games=s.Games.Where(g=>g.Phase==ProPhase.RegularSeason).ToArray();if(games.Length==0||games.Any(g=>!g.Football.HasPlayed))return s;
        var lines=games.SelectMany(g=>g.Football.PlayerStats).GroupBy(p=>p.PlayerId).ToDictionary(g=>g.Key,g=>g.ToArray());
        double Offense(ProPlayer p)=>lines.GetValueOrDefault(p.Football.PlayerId,[]).Sum(l=>l.PassYards/25d+l.PassingTouchdowns*4-l.InterceptionsThrown*2+l.RushYards/10d+l.RushingTouchdowns*6+l.ReceivingYards/10d+l.ReceivingTouchdowns*6-l.RushingFumbles*2-l.ReceivingFumbles*2);
        double Defense(ProPlayer p)=>lines.GetValueOrDefault(p.Football.PlayerId,[]).Sum(l=>l.Tackles*.5+l.TacklesForLoss*2+l.Sacks*6+l.Interceptions*8+l.PassesDefended+l.DefensiveTouchdowns*6+l.FumbleRecoveries*3);
        double Score(ProPlayer p)=>GameDayCoachingService.IsOffense(p.Football.Position)?Offense(p):p.Football.Position==Position.K?lines.GetValueOrDefault(p.Football.PlayerId,[]).Sum(l=>l.FieldGoalsMade*3-l.FieldGoalsAttempted+l.PuntsInside20*2+l.PuntYards/40d):Defense(p);
        var players=s.Players.Where(p=>!p.Retired&&s.League.Teams.Any(t=>t.Name==p.Football.TeamName)).ToArray();var awards=new List<ProAward>();
        void Award(string title,IEnumerable<ProPlayer> candidates,Func<ProPlayer,double> score)
        {
            var winner=candidates.Where(p=>lines.ContainsKey(p.Football.PlayerId)).OrderByDescending(score).ThenBy(p=>p.Football.PlayerId).FirstOrDefault();
            if(winner is not null)awards.Add(new(title,winner.Football.PlayerId,winner.Football.FullName,winner.Football.TeamName,Math.Round(score(winner),1)));
        }
        Award("Most Valuable Player",players,Score);Award("Offensive Player of the Year",players.Where(p=>GameDayCoachingService.IsOffense(p.Football.Position)),Offense);
        Award("Defensive Player of the Year",players.Where(p=>!GameDayCoachingService.IsOffense(p.Football.Position)&&p.Football.Position!=Position.K),Defense);
        Award("Offensive Rookie of the Year",players.Where(p=>p.Experience==0&&GameDayCoachingService.IsOffense(p.Football.Position)),Offense);
        Award("Defensive Rookie of the Year",players.Where(p=>p.Experience==0&&!GameDayCoachingService.IsOffense(p.Football.Position)&&p.Football.Position!=Position.K),Defense);
        var top=ProStandings.Build(s).First();var coach=s.Coaches.FirstOrDefault(c=>c.TeamId==top.Team.Id&&c.Role==StaffRole.HeadCoach&&!c.Vacant);
        if(coach is not null)awards.Add(new("Coach of the Year",Guid.Empty,coach.Name,top.Team.Name,top.Wins));
        var selections=new List<ProAllStarSelection>();
        var slots=new Dictionary<Position,int>{{Position.QB,3},{Position.FB,1},{Position.RB,3},{Position.WR,4},{Position.TE,2},{Position.OL,9},{Position.DE,4},{Position.DT,4},{Position.MLB,3},{Position.OLB,4},{Position.CB,4},{Position.FS,2},{Position.SS,2},{Position.K,2}};
        foreach(var conference in s.League.Teams.Select(t=>t.Conference).Distinct())
        {
            var clubs=s.League.Teams.Where(t=>t.Conference==conference).Select(t=>t.Name).ToHashSet();
            foreach(var (position,count) in slots)
            {
                var pool=players.Where(p=>clubs.Contains(p.Football.TeamName)&&p.Football.Position==position&&!p.PracticeSquad&&!p.Reserve).OrderByDescending(p=>Score(p)+p.Football.OverallRating*.1).ThenBy(p=>p.Football.PlayerId).ToArray();
                var selected=new List<ProPlayer>();
                if(position==Position.OL)foreach(var code in Enumerable.Range(5,5)){var p=pool.FirstOrDefault(p=>p.Football.NcaaPositionCode==code);if(p is not null)selected.Add(p);}
                if(position==Position.K)foreach(var code in new[]{19,20}){var p=pool.FirstOrDefault(p=>(p.Football.NcaaPositionCode??19)==code);if(p is not null)selected.Add(p);}
                selected.AddRange(pool.Where(p=>!selected.Any(x=>x.Football.PlayerId==p.Football.PlayerId)).Take(count-selected.Count));
                selections.AddRange(selected.Select(p=>new ProAllStarSelection(p,conference)));
            }
        }
        return s with{Honors=s.Honors.Append(new ProSeasonHonors(s.Year,awards,selections){AllPros=SelectAllPros(players,Score)}).ToList()};
    }
    public static List<ProAllProSelection> SelectAllPros(IEnumerable<ProPlayer> source,Func<ProPlayer,double>? score=null)
    {
        score??=p=>p.Football.OverallRating;var selected=new List<ProAllProSelection>();
        foreach(var group in source.Where(p=>!p.Retired&&!p.PracticeSquad&&!p.Reserve).DistinctBy(p=>p.Football.PlayerId).GroupBy(p=>p.Football.Position is Position.OL or Position.K?(p.Football.Position,p.Football.NcaaPositionCode??(p.Football.Position==Position.K?19:5)):(p.Football.Position,0)))
        {
            var count=group.Key.Item1 is Position.RB or Position.DE or Position.DT or Position.OLB or Position.CB?2:group.Key.Item1==Position.WR?3:1;
            var ranked=group.OrderByDescending(score).ThenBy(p=>p.Football.PlayerId).Take(count*2).ToArray();
            selected.AddRange(ranked.Select((p,i)=>new ProAllProSelection(p,i/count+1)));
        }
        return selected;
    }
    public static ProLeague ShowcaseLeague(ProSeasonHonors honors)
    {
        var conferences=honors.Selections.Select(p=>p.Conference).Distinct().ToArray();
        if(conferences.Length!=2)throw new InvalidOperationException("Two conference selection squads are required.");
        return new(1,"Pro Bowl",53,16,conferences.Select((c,i)=>new ProTeam("AS_"+i,c,"All-Stars",c.Length<=4?c:c[..3].ToUpperInvariant(),c,"Showcase",0,i==0?"#B52235":"#2056A0","#FFFFFF")).ToList());
    }
    public static GameDaySession StartShowcase(ProFranchise s,int year,GameDayMode mode,string? controlled=null)
    {
        var honors=s.Honors.Single(h=>h.Year==year);if(honors.Showcase?.HasPlayed==true)throw new InvalidOperationException("This showcase has already been completed.");
        if(year==s.Year&&s.Phase!=ProPhase.Offseason&&!(s.Phase==ProPhase.Playoffs&&s.Week==4))throw new InvalidOperationException("The Pro Bowl opens after the conference championship games.");
        var league=ShowcaseLeague(honors);var players=honors.Selections.Select(x=>x.Player with{Football=x.Player.Football with{TeamName=x.Conference+" All-Stars",CurrentInjury=null},PracticeSquad=false,Reserve=false,Retired=false}).ToList();
        var exhibition=s with{League=league,Players=players,GamePlans=[]};
        var game=new ScheduledGame{GameId=$"{s.Id}-{year}-showcase",SeasonYear=year,Week=19,HomeTeamName=league.Teams[1].Name,AwayTeamName=league.Teams[0].Name,IsNeutralSite=true,SimulationSeed=year*7919};
        var session=GameDayEngine.Start(game,ProLeagueService.Profile(exhibition,game.HomeTeamName),ProLeagueService.Profile(exhibition,game.AwayTeamName),players.Select(p=>p.Football).ToArray(),mode,rules:FootballRules.Professional);
        controlled??=game.HomeTeamName;if(!game.InvolvesTeam(controlled))throw new ArgumentException("Choose a showcase team.");session.ControlledTeamName=controlled;session.UserCoachRole=StaffRole.HeadCoach;return session;
    }
    public static ProFranchise FinishShowcase(ProFranchise s,int year,GameDaySession session)
    {
        if(!session.IsComplete||session.Game.GameId!=$"{s.Id}-{year}-showcase")throw new InvalidOperationException("Complete the correct showcase first.");
        var h=s.Honors.Single(h=>h.Year==year);if(h.Showcase?.HasPlayed==true)return s;
        return s with{Honors=s.Honors.Select(x=>x==h?x with{Showcase=GameDayEngine.Finish(session)}:x).ToList()};
    }
}
