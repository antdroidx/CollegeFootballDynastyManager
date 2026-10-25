using DynastyManager.Core.Models;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    string statisticsCategory = "Passing", statisticsPeriod = "Season", statisticsTeam = "All teams";
    static readonly Dictionary<string,string[]> StatisticColumns = new()
    {
        ["Passing"] = ["Games","PassCompletions","PassAttempts","PassYards","PassingTouchdowns","InterceptionsThrown"],
        ["Rushing"] = ["Games","RushAttempts","RushYards","RushingTouchdowns","RushingFumbles","Rushing20Plus"],
        ["Receiving"] = ["Games","Receptions","ReceivingYards","ReceivingTouchdowns","YardsAfterCatch","Drops","ReceivingFumbles","Receiving20Plus"],
        ["Defense"] = ["Games","Tackles","TacklesForLoss","Sacks","PassesDefended","Interceptions","DefensiveReturnYards","DefensiveTouchdowns","FumbleRecoveries"],
        ["Kicking"] = ["Games","FieldGoalsMade","FieldGoalsAttempted","ExtraPointsMade","ExtraPointsAttempted"],
        ["Punting"] = ["Games","Punts","PuntYards","LongPunt","PuntsInside20","PuntsInside10"],
        ["Returns"] = ["Games","KickReturns","KickReturnYards","KickReturnTouchdowns","PuntReturns","PuntReturnYards","PuntReturnTouchdowns"]
    };
    void Statistics()
    {
        var s = state!; body.Add(Text("Player statistics",23,true));
        var category = new Picker { Title = "Category", ItemsSource = StatisticColumns.Keys.ToArray(), SelectedIndex = Array.IndexOf(StatisticColumns.Keys.ToArray(),statisticsCategory) };
        var period = new Picker { ItemsSource = new[] { "Season", "Career" }, SelectedIndex = statisticsPeriod == "Career" ? 1 : 0 };
        var teams = new[] { "All teams" }.Concat(s.League.Teams.Select(t=>t.Name)).ToArray(); var team = new Picker { ItemsSource = teams, SelectedIndex = Array.IndexOf(teams,statisticsTeam) };
        var filters = new DynastyManager.App.ActionRow(); filters.Add(category); filters.Add(period); filters.Add(team); body.Add(filters);
        body.Add(Text("Regular-season totals · tap a column to sort",12));
        var list = new VerticalStackLayout(); body.Add(list);
        void Draw()
        {
            list.Clear(); var fields = StatisticColumns[statisticsCategory];
            var games = (statisticsPeriod == "Career" ? s.History.SelectMany(h=>h.Games).Concat(s.Games) : s.Games).Where(g=>g.Phase==ProPhase.RegularSeason && g.Football.HasPlayed).DistinctBy(g=>g.Football.GameId);
            var lines = games.SelectMany(g=>g.Football.PlayerStats).Where(p=>statisticsTeam=="All teams" || p.TeamName==statisticsTeam).GroupBy(p=>p.PlayerId).ToArray();
            int Total(IEnumerable<PlayerSeasonStatLine> values,string field) => field=="LongPunt" ? values.Max(p=>p.LongPunt) : values.Sum(p=>(int)typeof(PlayerSeasonStatLine).GetProperty(field)!.GetValue(p)!);
            var rows = lines.Where(g=>fields.Skip(1).Any(f=>Total(g,f)>0)).OrderByDescending(g=>Total(g,fields[statisticsCategory=="Passing"?3:statisticsCategory=="Rushing"?2:statisticsCategory=="Receiving"?2:1])).ToArray();
            list.Add(Text(rows.Length + " players · " + statisticsCategory + " · " + statisticsPeriod,12));
            var columns = new List<(string,int)> { ("Player",190),("Team",170) }; columns.AddRange(fields.Select(f=>(System.Text.RegularExpressions.Regex.Replace(f,"([a-z])([A-Z])","$1 $2"),100)));
            if(statisticsCategory is "Passing" or "Kicking") columns.Add((statisticsCategory=="Passing"?"Cmp%":"FG%",75));
            list.Add(Table(columns.ToArray(),rows.Select(g=>
            {
                var first = g.First(); var player = s.Players.FirstOrDefault(p=>p.Football.PlayerId==g.Key);
                var cells = new List<View> { player is null ? Text(first.PlayerName) : Link(first.PlayerName,()=>Player(player)), Text(g.Select(p=>p.TeamName).Distinct().Count()>1 ? "Multiple teams" : first.TeamName) };
                cells.AddRange(fields.Select(f=>Text(Total(g,f).ToString())));
                if(statisticsCategory=="Passing") cells.Add(Text((100d*Total(g,"PassCompletions")/Math.Max(1,Total(g,"PassAttempts"))).ToString("0.0")));
                if(statisticsCategory=="Kicking") cells.Add(Text((100d*Total(g,"FieldGoalsMade")/Math.Max(1,Total(g,"FieldGoalsAttempted"))).ToString("0.0")));
                return cells.ToArray();
            })));
        }
        category.SelectedIndexChanged += (_,_)=> { statisticsCategory=(string)category.SelectedItem; Draw(); };
        period.SelectedIndexChanged += (_,_)=> { statisticsPeriod=(string)period.SelectedItem; Draw(); };
        team.SelectedIndexChanged += (_,_)=> { statisticsTeam=(string)team.SelectedItem; Draw(); }; Draw();
    }
}
