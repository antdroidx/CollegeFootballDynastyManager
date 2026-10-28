using DynastyManager.App;
using DynastyManager.Core.Models;
using ProFootball.Core;
namespace ProFootball.App;

public sealed partial class FranchisePage
{
    Task TradingBlock()
    {
        state = ProTradeMarket.Refresh(state!); var s = state;
        body.Clear(); body.Add(Action("‹ Front Office",()=>Open("Front Office"))); body.Add(Text("Trading block",25,true));
        body.Add(Text("Browse available players or list your own from their contract & roster actions. Interest is preliminary; cap and roster checks apply to every deal.",12));
        if(!ProTradeMarket.IsOpen(s)) body.Add(Text("Trades are closed until the offseason.",16,true));
        var list=new VerticalStackLayout(); var position=new Picker { ItemsSource=new[]{"All positions"}.Concat(Enum.GetNames<Position>()).ToArray(),SelectedIndex=0 };body.Add(position);body.Add(list);
        void Draw()
        {
            list.Clear();var players=s.Players.Where(p=>s.TradeBlock.Contains(p.Football.PlayerId)&&(position.SelectedIndex==0||p.Football.Position.ToString()==(string)position.SelectedItem)).OrderByDescending(p=>p.Football.OverallRating).ToArray();
            list.Add(Table([("Pos",48),("Player",190),("Team",180),("OVR",48),("Age",48),("Interest / action",230)],players.Select(p=>
            {
                var own=p.Football.TeamName==s.UserTeam;
                var actions=new VerticalStackLayout { Spacing=2 };
                if(own)
                {
                    foreach(var club in ProTradeMarket.InterestedTeams(s,p).Take(3)) { var partner=club; actions.Add(Link(partner,()=>Trade(partner,p.Football.PlayerId,true))); }
                    actions.Add(Action("Unlist",async()=>{await Update(x=>ProTradeMarket.List(x,x.UserTeam,p.Football.PlayerId,false));await TradingBlock();}));
                }
                else actions.Add(Action("Build offer",()=>Trade(p.Football.TeamName,p.Football.PlayerId,false)));
                actions.IsEnabled=ProTradeMarket.IsOpen(s);
                return new View[]{Text(p.Football.Position.ToString()),PlayerCell(p),TeamCell(s.League.Teams.Single(t=>t.Name==p.Football.TeamName)),Text(p.Football.OverallRating.ToString()),Text(p.Age.ToString()),actions};
            })));
        }
        position.SelectedIndexChanged+=(_,_)=>Draw();Draw();return Task.CompletedTask;
    }

    Task Trade(string? partner=null,Guid? initialPlayer=null,bool giveInitial=false)
    {
        state=ProEconomy.EnsurePickInventory(state!);var s=state!;body.Clear();body.Add(Action("‹ Front Office",()=>Open("Front Office")));body.Add(Text("Trade center",25,true));
        body.Add(Text($"Trade draft picks through {s.Year+3} · choose Draft picks under either team",12));
        var clubs=s.League.Teams.Where(t=>t.Name!=s.UserTeam).OrderBy(t=>t.Name).ToArray();
        var other=new Picker { Title="Trade partner",ItemsSource=clubs.Select(t=>t.Name).ToArray(),SelectedIndex=Math.Max(0,Array.FindIndex(clubs,t=>t.Name==partner)) };body.Add(other);
        var givePlayers=new HashSet<Guid>();var getPlayers=new HashSet<Guid>();var givePicks=new HashSet<Guid>();var getPicks=new HashSet<Guid>();
        if(initialPlayer is {} id)(giveInitial?givePlayers:getPlayers).Add(id);
        var summary=Text("",14,true);body.Add(summary);var chosen=new VerticalStackLayout{Spacing=6};body.Add(chosen);body.Add(Text("Choose assets below. Tap a selected asset to remove it. The CPU reviews value, cap and roster legality.",12));
        var workspace=new Grid {ColumnSpacing=10,RowSpacing=10};body.Add(workspace);
        void Layout()
        {
            var wide=Width>=850;
            foreach(var (view,i) in workspace.Children.OfType<View>().Select((v,i)=>(v,i))) {Grid.SetColumn(view,wide?i:0);Grid.SetRow(view,wide?0:i);}
            if(workspace.ColumnDefinitions.Count==(wide?2:1)&&workspace.RowDefinitions.Count==(wide?1:2)) return;
            workspace.ColumnDefinitions.Clear();workspace.RowDefinitions.Clear();
            workspace.RowDefinitions.Add(new(GridLength.Auto));if(!wide)workspace.RowDefinitions.Add(new(GridLength.Auto));
            workspace.ColumnDefinitions.Add(new(GridLength.Star));if(wide)workspace.ColumnDefinitions.Add(new(GridLength.Star));
            foreach(var (child,index) in workspace.Children.OfType<View>().Select((v,i)=>(v,i))) {Grid.SetColumn(child,wide?index:0);Grid.SetRow(child,wide?0:index);}
        }
        workspace.SizeChanged+=(_,_)=>Layout();
        string Recipient()=>(string)other.SelectedItem;
        void Summary()
        {
            var sent=ProTradeMarket.PackageValue(s,givePlayers,givePicks,Recipient());var requested=ProTradeMarket.PackageValue(s,getPlayers,getPicks,s.UserTeam);
            summary.Text=$"Your offer: {givePlayers.Count} players + {givePicks.Count} picks · Value {sent:0}\nTheir offer: {getPlayers.Count} players + {getPicks.Count} picks · Value {requested:0}\nCPU interest: "+(sent+requested==0?"Select assets":sent>=requested*1.05?"Value acceptable · legality review pending":$"Needs about {Math.Max(0,requested*1.05-sent):0} more value");
            chosen.Clear();
            void Selected(string title,HashSet<Guid> playerIds,HashSet<Guid> pickIds)
            {
                if(playerIds.Count+pickIds.Count==0)return;
                chosen.Add(Text(title,13,true));var row=new ActionRow();
                foreach(var id in playerIds.ToArray()){var item=s.Players.Single(p=>p.Football.PlayerId==id);var remove=new Button{Text=item.Football.FullName+" ×",FontSize=12};SemanticProperties.SetDescription(remove,"Remove "+item.Football.FullName+" from trade");remove.Clicked+=(_,_)=>{playerIds.Remove(id);Draw();};row.Add(remove);}
                foreach(var id in pickIds.ToArray()){var item=s.Picks.Single(p=>p.Id==id);var remove=new Button{Text=$"{item.Year} R{item.Round} · {item.OriginalTeam} ×",FontSize=12};remove.Clicked+=(_,_)=>{pickIds.Remove(id);Draw();};row.Add(remove);}chosen.Add(row);
            }
            Selected("You send",givePlayers,givePicks);Selected("You receive",getPlayers,getPicks);

        }
        View Side(string club,bool giving)
        {
            var selectedPlayers=giving?givePlayers:getPlayers;var selectedPicks=giving?givePicks:getPicks;
            View Select(Guid id,bool pick)
            {
                var set=pick?selectedPicks:selectedPlayers;var check=new CheckBox {IsChecked=set.Contains(id),Color=DynastyTheme.Accent};check.CheckedChanged+=(_,e)=>{if(e.Value)set.Add(id);else set.Remove(id);Summary();};return check;
            }
            var players=s.Players.Where(p=>p.Football.TeamName==club&&!p.Retired&&ProCareer.Controls(s,p.Football.Position)).OrderByDescending(p=>s.TradeBlock.Contains(p.Football.PlayerId)).ThenBy(p=>p.Football.Position).ThenByDescending(p=>p.Football.OverallRating);
            var search=new SearchBar{Placeholder="Find player",AutomationId=giving?"trade-your-search":"trade-their-search"};
            var position=new Picker{Title="Position",ItemsSource=new[]{"All positions"}.Concat(Enum.GetNames<Position>()).ToArray(),SelectedIndex=0};
            var playerList=new ContentView();
            void PlayerTable(){var filtered=players.Where(p=>(position.SelectedIndex==0||p.Football.Position.ToString()==(string)position.SelectedItem)&&(string.IsNullOrWhiteSpace(search.Text)||p.Football.FullName.Contains(search.Text,StringComparison.OrdinalIgnoreCase))).ToArray();playerList.Content=filtered.Length==0?Text("No players match these filters."):Table([("Select",54),("Pos",45),("Player",165),("OVR",48),("Age",48),("Value",65)],filtered.Select(p=>new View[]{Select(p.Football.PlayerId,false),Text(p.Football.Position.ToString()),PlayerCell(p),Text(p.Football.OverallRating.ToString()),Text(p.Age.ToString()),Text(ProTrade.PlayerValue(s,p,giving?Recipient():s.UserTeam).ToString("0"))}));}
            search.TextChanged+=(_,_)=>PlayerTable();position.SelectedIndexChanged+=(_,_)=>PlayerTable();PlayerTable();
            var table=new VerticalStackLayout{Spacing=6,Children={search,position,playerList}};

            var pickYear=new Picker {Title="Draft year",ItemsSource=new[]{"All draft years"}.Concat(s.Picks.Where(p=>p.Owner==club&&ProTrade.Available(s,p)).Select(p=>p.Year).Distinct().Order().Select(y=>y.ToString())).ToArray(),SelectedIndex=0};
            View PickTable()=>Table([("Select",54),("Year",65),("Round",55),("Original team",165),("Projected slot",105),("Value",65)],s.Picks.Where(p=>p.Owner==club&&ProTrade.Available(s,p)&&(pickYear.SelectedIndex==0||p.Year.ToString()==(string)pickYear.SelectedItem)).OrderBy(p=>p.Year).ThenBy(p=>p.Round).Select(p=>new View[]{Select(p.Id,true),Text(p.Year.ToString()),Text(p.Round.ToString()),Text(p.OriginalTeam,12),Text(ProPickProjection.Slot(s,p).ToString("0.0")),Text(ProTrade.PickValue(s,p).ToString("0"))}));
            var pickList=new ContentView {Content=PickTable()};pickYear.SelectedIndexChanged+=(_,_)=>pickList.Content=PickTable();
            var picks=new VerticalStackLayout {Children={pickYear,pickList}};
            var tabs=new Picker {ItemsSource=new[]{"Players","Draft picks"},SelectedIndex=0};
            var assets=new ContentView {Content=table};tabs.SelectedIndexChanged+=(_,_)=>assets.Content=tabs.SelectedIndex==0?table:picks;
            return Panel(club,Text($"Cap room {Money(ProEconomy.Room(s,club))} · Roster {ProRoster.CountForLimit(s,club)}/{ProRoster.Limit(s)}",12),tabs,new ScrollView {HeightRequest=300,Content=assets});
        }
        void Draw(){workspace.Clear();workspace.Add(Side(s.UserTeam,true));workspace.Add(Side(Recipient(),false));Layout();Summary();}
        other.SelectedIndexChanged+=(_,_)=>{givePlayers.Clear();getPlayers.Clear();givePicks.Clear();getPicks.Clear();Draw();};
        var actions=new ActionRow();
        actions.Add(Action("Suggest additional picks",async()=>
        {
            var shortfall=ProTradeMarket.PackageValue(s,getPlayers,getPicks,s.UserTeam)*1.05-ProTradeMarket.PackageValue(s,givePlayers,givePicks,Recipient());
            var picks=ProTradeMarket.CounterPicks(s,s.UserTeam,shortfall,givePicks);
            if(picks.Count==0){await GameDialogs.AlertAsync(this,"CPU evaluation",shortfall<=0?"Value is acceptable. Submit for cap and roster review.":"Available picks cannot bridge the gap. Change the requested players or add player value.","Close");return;}
            foreach(var id in picks)givePicks.Add(id);Draw();
        }));
        actions.Add(Action("Propose trade",async()=>
        {
            if(!await GameDialogs.AlertAsync(this,"Submit trade?",summary.Text+"\nSelected players and picks will change teams if approved.","Propose","Cancel"))return;
            await Update(x=>ProManagement.Submit(x,"Trade recommendation",givePlayers.Concat(getPlayers).ToArray(),0,y=>ProTrade.Propose(y,y.UserTeam,Recipient(),givePlayers.ToArray(),getPlayers.ToArray(),givePicks.ToArray(),getPicks.ToArray()),sharedPicks:givePicks.Count+getPicks.Count>0));
        }));actions.IsEnabled=ProTradeMarket.IsOpen(s);if(!actions.IsEnabled)body.Add(Text("Trading is closed for this stage of the season.",12));body.Remove(workspace);body.Add(actions);body.Add(workspace);Draw();return Task.CompletedTask;
    }
}
