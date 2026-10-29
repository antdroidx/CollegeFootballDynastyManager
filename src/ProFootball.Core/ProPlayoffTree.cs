namespace ProFootball.Core;
public sealed record ProPlayoffSlot(string Conference,int Round,int Slot,ProGame? Game,string? Home,string? Away,int? HomeSeed,int? AwaySeed,bool Projected);
public static class ProPlayoffTree
{
    public static IReadOnlyList<ProPlayoffSlot> Build(ProFranchise s,int year)
    {
        var games=s.Games.Concat(s.History.SelectMany(h=>h.Games)).Where(g=>g.Football.SeasonYear==year).DistinctBy(g=>g.Football.GameId).ToList();
        var season=s with{Year=year,Games=games};var result=new List<ProPlayoffSlot>();
        foreach(var conference in s.League.Teams.Select(t=>t.Conference).Distinct())
        {
            var seeds=ProStandings.Seeds(season,conference);
            foreach(var round in new[]{1,2,3})
            {
                var actual=games.Where(g=>g.Phase==ProPhase.Playoffs&&g.Football.Week==round&&s.League.Teams.Any(t=>t.Name==g.Football.HomeTeamName&&t.Conference==conference)).OrderBy(g=>g.Football.HomeSeed).ToArray();
                for(var slot=0;slot<(round==1?3:round==2?2:1);slot++)
                {
                    var game=actual.ElementAtOrDefault(slot);var home=game?.Football.HomeTeamName;var away=game?.Football.AwayTeamName;int? hs=game?.Football.HomeSeed,aws=game?.Football.AwaySeed;
                    if(game is null&&round==1){hs=slot+2;aws=7-slot;home=seeds[hs.Value-1].Team.Name;away=seeds[aws.Value-1].Team.Name;}
                    if(game is null&&round==2&&slot==0){hs=1;home=seeds[0].Team.Name;}
                    result.Add(new(conference,round,slot,game,home,away,hs,aws,game is null));
                }
            }
        }
        var final=games.SingleOrDefault(g=>g.Phase==ProPhase.Playoffs&&g.Football.Week==4);
        result.Add(new("Championship",4,0,final,final?.Football.HomeTeamName,final?.Football.AwayTeamName,final?.Football.HomeSeed,final?.Football.AwaySeed,final is null));return result;
    }
}
