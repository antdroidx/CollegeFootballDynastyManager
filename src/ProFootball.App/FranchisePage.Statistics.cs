using DynastyManager.App;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    string statisticsCategory="Passing",statisticsPeriod="Season",statisticsTeam="All teams";
    void Statistics()
    {
        var s=state!;body.Add(Text("Player statistics",23,true));
        var category=new Picker{Title="Category",AutomationId="pro-stat-category",ItemsSource=PlayerStatCatalog.Categories,SelectedIndex=Array.IndexOf(PlayerStatCatalog.Categories,statisticsCategory)};
        var period=new Picker{AutomationId="pro-stat-period",ItemsSource=new[]{"Season","Career"},SelectedIndex=statisticsPeriod=="Career"?1:0};
        var teams=new[]{"All teams"}.Concat(s.League.Teams.Select(t=>t.Name)).ToArray();var team=new Picker{ItemsSource=teams,SelectedIndex=Array.IndexOf(teams,statisticsTeam)};
        body.Add(CompactFilters.Create((category,180),(period,130),(team,210)));body.Add(Text("Regular-season totals · tap a column to sort · Tracked GP = games with detailed data",12));
        var list=new VerticalStackLayout();body.Add(list);
        void Draw()
        {
            list.Clear();var games=(statisticsPeriod=="Career"?s.History.SelectMany(h=>h.Games).Concat(s.Games):s.Games).Where(g=>g.Phase==ProPhase.RegularSeason&&g.Football.HasPlayed).DistinctBy(g=>g.Football.GameId);
            var rows=games.SelectMany(g=>g.Football.PlayerStats).Where(p=>statisticsTeam=="All teams"||p.TeamName==statisticsTeam).GroupBy(p=>p.PlayerId).Select(g=>PlayerStatTotals.Total(g) with {TeamName=g.Select(p=>p.TeamName).Distinct().Count()>1?"Multiple teams":g.First().TeamName}).Where(p=>PlayerStatCatalog.Eligible(p,statisticsCategory)).OrderByDescending(p=>PlayerStatCatalog.Value(p,PlayerStatCatalog.Fields[statisticsCategory][1])??0).ToArray();
            list.Add(Text(rows.Length+" players · "+statisticsCategory+" · "+statisticsPeriod,12));
            var columns=FootballStatViews.Columns(statisticsCategory).Select(c=>(c.Title=="School"?"Team":c.Title,(int)c.Width)).ToArray();
            list.Add(Table(columns,rows.Select(p=>FootballStatViews.Cells(p,statisticsCategory,(id,name)=>s.Players.FirstOrDefault(x=>x.Football.PlayerId==id) is {} player?Link(name,()=>Player(player)):Text(name),name=>s.League.Teams.FirstOrDefault(t=>t.Name==name) is {} club?TeamCell(club):Text(name),text=>Text(text),(n,d)=>(100d*n/Math.Max(1,d)).ToString("0.0"),(n,d)=>(n/(double)Math.Max(1,d)).ToString("0.0"),line=>ProStatistics.PasserRating(line).ToString("0.0")))));
        }
        category.SelectedIndexChanged+=(_,_)=>{statisticsCategory=(string)category.SelectedItem;Draw();};period.SelectedIndexChanged+=(_,_)=>{statisticsPeriod=(string)period.SelectedItem;Draw();};team.SelectedIndexChanged+=(_,_)=>{statisticsTeam=(string)team.SelectedItem;Draw();};Draw();
    }
    View DetailedStatsPanel(IEnumerable<PlayerSeasonStatLine> source)
    {
        var rows=source.ToArray();if(rows.Length==0)return Panel("Statistics",Text("No recorded regular-season games yet."));
        var total=PlayerStatTotals.Total(rows);var sections=new List<View>();
        foreach(var category in PlayerStatCatalog.Categories.Where(c=>PlayerStatCatalog.Eligible(total,c)))
        {
            sections.Add(Text(category,16,true));var columns=FootballStatViews.Columns(category).Skip(2).Select(c=>(c.Title,(int)c.Width)).ToArray();
            sections.Add(Table(columns,[FootballStatViews.Cells(total,category,(_,name)=>Text(name),name=>Text(name),text=>Text(text),(_,_)=>"",(_,_)=>"",line=>ProStatistics.PasserRating(line).ToString("0.0")).Skip(2).ToArray()]));
        }
        return Panel("Regular-season statistics",sections.ToArray());
    }
}
