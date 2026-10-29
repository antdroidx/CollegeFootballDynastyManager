using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Core;
public static class ProGeneratedRoster
{
    public static List<ProPlayer> Create(ProLeague league,Random? random=null)
    {
        random??=new Random();var source=ProLeagueService.Snapshot().Players;var names=source.Select(p=>p.Football).ToArray();var result=new List<ProPlayer>();
        var baselines=source.Where(p=>ProDepthChart.Active(p)).GroupBy(p=>p.Football.Position).ToDictionary(g=>g.Key,g=>g.GroupBy(p=>p.Football.TeamName).SelectMany(team=>team.OrderBy(p=>p.Football.DepthChartOrder).ThenByDescending(p=>p.Football.OverallRating).Take(g.Key==Position.OL?5:g.Key is Position.WR or Position.CB?3:g.Key is Position.DE or Position.DT or Position.OLB?2:1)).Average(p=>p.Football.OverallRating));
        var context=new DynastyState{DynastyName="Generated pro roster",UserTeamName=league.Teams[0].Name};
        (Position Position,int Count)[] structure=[(Position.QB,3),(Position.RB,4),(Position.WR,6),(Position.TE,3),(Position.OL,9),(Position.DE,4),(Position.DT,4),(Position.MLB,3),(Position.OLB,4),(Position.CB,5),(Position.FS,3),(Position.SS,3),(Position.K,2)];
        foreach(var team in league.Teams)
        {
            var strength=random.Next(-3,4);
            foreach(var (position,count) in structure)for(var depth=0;depth<count;depth++)Add(position,depth,false);
            for(var i=0;i<16;i++)Add(structure[i%structure.Length].Position,10+i,true);
            void Add(Position pos,int depth,bool practice)
            {
                var tier=pos==Position.OL?depth/5:pos is Position.DE or Position.DT or Position.OLB?depth/2:pos is Position.WR or Position.CB?depth/3:depth;
                var target=Math.Clamp((practice?59:(int)Math.Round(baselines[pos])-tier*7)+strength+random.Next(-6,7),45,96);
                var age=practice?random.Next(22,25):22+(random.Next(0,12)+random.Next(0,12))/2;var experience=Math.Max(0,age-22);var rookie=2026-experience;
                var code=pos switch{Position.OL=>5+depth%5,Position.DE=>10+depth%2,Position.OLB=>depth%2==0?13:15,Position.K=>depth%2==0?19:20,_=>(int?)null};
                var template=new DynastyPlayer{FullName="Generated Player",TeamName=team.Name,Position=pos,NcaaPositionCode=code,DepthChartOrder=depth+1};
                var pool=names.Where(p=>p.Position==pos&&(pos!=Position.K||p.NcaaPositionCode==code)).OrderBy(p=>Math.Abs(p.OverallRating-target)).Take(12).ToArray();
                var archetype=pool[random.Next(pool.Length)];
                var football=FictionalRosterService.Player(template,names,random,context,target,4,archetype);
                football=football with{PotentialRating=Math.Clamp(football.OverallRating+random.Next(0,age<27?13:age<30?7:3),football.OverallRating,99)};
                result.Add(new(football,age,experience,practice){RookieYear=rookie,DraftOverall=random.NextDouble()<.16?null:random.Next(1,257),RatingSource="Generated",RosterStatus=practice?"PS":"ACT"});
            }
        }
        return result;
    }
}
