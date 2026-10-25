using DynastyManager.Core.Models;
namespace ProFootball.Core;
public static class ProSchedule
{
    public static List<ProGame> Build(ProLeague league,int year)
    {
        ProLeagueService.Validate(league);
        var conferences=league.Teams.GroupBy(t=>t.Conference).Select(c=>c.GroupBy(t=>t.Division).Select(d=>d.OrderBy(t=>t.SeedRank).ToArray()).ToArray()).ToArray();
        var result=new List<ProGame>();var seed=year*1000;
        void Game(ProTeam home,ProTeam away,int week,ProPhase phase)=>result.Add(new(new ScheduledGame{GameId=$"{year}-{phase}-{week}-{home.Id}-{away.Id}",SeasonYear=year,Week=week,HomeTeamName=home.Name,AwayTeamName=away.Name,SimulationSeed=seed++},phase));
        for(var pre=0;pre<3;pre++)for(var d=0;d<4;d++)for(var rank=0;rank<4;rank++)
            Game(conferences[pre%2][d][rank],conferences[1-pre%2][(d+pre)%4][rank],pre+1,ProPhase.Preseason);
        void Regular(ProTeam home,ProTeam away,int round)
        { var week=round<8?round+1:round==8?(home.Conference==conferences[0][0][0].Conference?9:10):round+2;Game(home,away,week,ProPhase.RegularSeason); }
        int[][] pairs=[[0,3,1,2],[0,2,3,1],[0,1,2,3]];
        for(var round=0;round<6;round++)foreach(var c in conferences)foreach(var division in c)
            for(var pair=0;pair<4;pair+=2)
            { var a=division[pairs[round%3][pair]];var b=division[pairs[round%3][pair+1]];Regular(round<3?a:b,round<3?b:a,round); }
        var rotation=(year-2026)%3+1;
        for(var round=0;round<4;round++)foreach(var c in conferences)for(var d=0;d<4;d++)
        { var other=d^rotation;if(d>other)continue;for(var rank=0;rank<4;rank++)
            { var a=c[d][rank];var b=c[other][(rank+round)%4];Regular(round%2==0?a:b,round%2==0?b:a,6+round); } }
        var otherMatches=new[]{1,2,3}.Where(m=>m!=rotation).ToArray();
        int[] cycle=[0,otherMatches[0],otherMatches[0]^otherMatches[1],otherMatches[1]];
        for(var m=0;m<2;m++)foreach(var c in conferences)for(var d=0;d<4;d++)
        { var other=d^otherMatches[m];if(d>other)continue;var home=cycle[(Array.IndexOf(cycle,d)+1)%4]==other?d:other;var away=home==d?other:d;
          for(var rank=0;rank<4;rank++) Regular(c[home][rank],c[away][rank],10+m); }
        var cross=(year-2026)%4;
        for(var round=0;round<4;round++)for(var d=0;d<4;d++)for(var rank=0;rank<4;rank++)
        { var a=conferences[0][d][rank];var b=conferences[1][(d+cross)%4][(rank+round)%4];Regular(round%2==0?a:b,round%2==0?b:a,12+round); }
        for(var d=0;d<4;d++)for(var rank=0;rank<4;rank++)
        { var a=conferences[0][d][rank];var b=conferences[1][(d+cross+1)%4][rank];Regular(year%2==0?a:b,year%2==0?b:a,16); }
        return result;
    }
}
