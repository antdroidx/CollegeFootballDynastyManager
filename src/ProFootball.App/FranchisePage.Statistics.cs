using DynastyManager.App;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    string statisticsCategory="Passing",statisticsPeriod="Season",statisticsTeam="All teams";
    bool statisticsPlayoffs;
    void Statistics()
    {
        var s=state!;body.Add(PageHeader("Player statistics","Regular season and playoff totals"));
        var category=new Picker{Title="Category",AutomationId="pro-stat-category",ItemsSource=PlayerStatCatalog.Categories,SelectedIndex=Array.IndexOf(PlayerStatCatalog.Categories,statisticsCategory)};
        var period=new Picker{AutomationId="pro-stat-period",ItemsSource=new[]{"Season","Career"},SelectedIndex=statisticsPeriod=="Career"?1:0};
        var competition=new Picker{AutomationId="pro-stat-competition",ItemsSource=new[]{"Regular season","Playoffs"},SelectedIndex=statisticsPlayoffs?1:0};
        var teams=new[]{"All teams"}.Concat(s.League.Teams.Select(t=>t.Name)).ToArray();var team=new Picker{ItemsSource=teams,SelectedIndex=Array.IndexOf(teams,statisticsTeam)};
        body.Add(CompactFilters.Create((category,160),(competition,160),(period,110),(team,190)));body.Add(Text("Separate regular-season/playoff totals · tap a column to sort · Tracked GP = games with detailed data",12));
        var list=new VerticalStackLayout();body.Add(list);
        void Draw()
        {
            list.Clear();
            var rows=ProPlayerStatistics.Totals(s,statisticsPlayoffs,statisticsPeriod=="Career",statisticsTeam=="All teams"?null:statisticsTeam)
                .Where(p=>PlayerStatCatalog.Eligible(p,statisticsCategory)).OrderByDescending(p=>PlayerStatCatalog.Value(p,PlayerStatCatalog.Fields[statisticsCategory][1])??0).ToArray();
            list.Add(Text(rows.Length+" players · "+statisticsCategory+" · "+(statisticsPlayoffs?"Playoffs":"Regular season")+" · "+statisticsPeriod,12));
            var columns=FootballStatViews.Columns(statisticsCategory).Select(c=>(c.Title=="School"?"Team":c.Title,(int)c.Width)).ToArray();
            list.Add(Table(columns,rows.Select(p=>FootballStatViews.Cells(p,statisticsCategory,(id,name)=>s.Players.FirstOrDefault(x=>x.Football.PlayerId==id) is {} player?Link(name,()=>Player(player)):Text(name),name=>s.League.Teams.FirstOrDefault(t=>t.Name==name) is {} club?TeamCell(club):Text(name),text=>Text(text),(n,d)=>(100d*n/Math.Max(1,d)).ToString("0.0"),(n,d)=>(n/(double)Math.Max(1,d)).ToString("0.0"),line=>ProStatistics.PasserRating(line).ToString("0.0")))));
        }
        competition.SelectedIndexChanged+=(_,_)=>{statisticsPlayoffs=competition.SelectedIndex==1;Draw();};
        category.SelectedIndexChanged+=(_,_)=>{statisticsCategory=(string)category.SelectedItem;Draw();};period.SelectedIndexChanged+=(_,_)=>{statisticsPeriod=(string)period.SelectedItem;Draw();};team.SelectedIndexChanged+=(_,_)=>{statisticsTeam=(string)team.SelectedItem;Draw();};Draw();
    }
    View PlayerStatisticsPanel(ProPlayer player)
    {
        var phase=new Picker{AutomationId="pro-profile-stat-competition",ItemsSource=new[]{"Regular season","Playoffs"},SelectedIndex=0};
        var period=new Picker{AutomationId="pro-profile-stat-period",ItemsSource=new[]{"Season","Career"},SelectedIndex=0};
        var category=new Picker{AutomationId="pro-profile-stat-category",ItemsSource=PlayerStatCatalog.Categories,SelectedIndex=Array.IndexOf(PlayerStatCatalog.Categories,player.Football.Position is Position.OL or Position.FB?"Blocking":player.Football.Position==Position.QB?"Passing":"Playing time")};
        var host=new ContentView();
        void Draw()
        {
            var lines=ProPlayerStatistics.Lines(state!,phase.SelectedIndex==1,period.SelectedIndex==1).Where(p=>p.PlayerId==player.Football.PlayerId).ToArray();
            var title=(phase.SelectedIndex==1?"Playoffs":"Regular season")+" · "+period.SelectedItem;
            if(lines.Length==0){host.Content=Panel(title,Text("No recorded "+(phase.SelectedIndex==1?"playoff":"regular-season")+" games yet."));return;}
            var total=PlayerStatTotals.Total(lines);var selected=(string)category.SelectedItem;
            if(!PlayerStatCatalog.Eligible(total,selected)){host.Content=Panel(title,Text("No recorded "+selected.ToLowerInvariant()+" statistics."));return;}
            var columns=FootballStatViews.Columns(selected).Skip(2).Select(c=>(c.Title,(int)c.Width)).ToArray();
            host.Content=Panel(title,Table(columns,[FootballStatViews.Cells(total,selected,(_,name)=>Text(name),name=>Text(name),text=>Text(text),(_,_)=>"",(_,_)=>"",line=>ProStatistics.PasserRating(line).ToString("0.0")).Skip(2).ToArray()]));
        }
        phase.SelectedIndexChanged+=(_,_)=>Draw();period.SelectedIndexChanged+=(_,_)=>Draw();category.SelectedIndexChanged+=(_,_)=>Draw();Draw();
        return new VerticalStackLayout{Spacing=10,Children={CompactFilters.Create((phase,160),(period,110),(category,160)),host}};
    }
}
