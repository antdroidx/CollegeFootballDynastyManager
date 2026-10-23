using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private sealed class LiveBoxScorePage : ContentPage
    {
        public Func<Task>? CloseRequested { get; set; }
        protected override bool OnBackButtonPressed()
        {
            if (CloseRequested is not null) _ = CloseRequested();
            return true;
        }
    }

    private async Task ShowLiveBoxScoreAsync(GameDayBoxScore box)
    {
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var page = new LiveBoxScorePage { BackgroundColor = DynastyTheme.Background };
        var close = new Button { Text = "Return to game", FontSize = 12, MinimumHeightRequest = 38, HorizontalOptions = LayoutOptions.Start, BackgroundColor = DynastyTheme.Accent, TextColor = DynastyTheme.Background };
        var closing = false;
        async Task Close()
        {
            if (closing) return; closing = true;
            try { await Navigation.PopModalAsync(); }
            finally { closed.TrySetResult(); }
        }
        page.CloseRequested = Close; close.Clicked += async (_, _) => await Close();
        var content = new VerticalStackLayout { Padding = 14, Spacing = 12 };
        var heading = SectionTitle("Live box score", box.IsComplete ? "Final statistics" :
            $"Paused · {(box.Quarter > 4 ? $"OT {box.Quarter-4}" : $"Q{box.Quarter}")} {TimeSpan.FromSeconds(box.ClockSeconds):m\\:ss} · returns to the same game");
        if (box.Weather is not null) content.Children.Add(new Label { Text = box.Weather.Summary, FontSize = 11, TextColor = DynastyTheme.Muted });
        content.Children.Add(new Label { Text = $"Morale · {box.AwayTeamName}: {GameMoraleService.Label(box.AwayMorale)} ({box.AwayMorale}) · {box.HomeTeamName}: {GameMoraleService.Label(box.HomeMorale)} ({box.HomeMorale})", FontSize = 11, TextColor = DynastyTheme.Muted });
        if (box.PlayerOfGame is { } award) content.Children.Add(new Border { Padding = 12, StrokeThickness = 0, BackgroundColor = DynastyTheme.Palette("#343B28"),
            Content = new VerticalStackLayout { Spacing = 4, Children = {
                new Label { Text = "★ PLAYER OF THE GAME", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Palette("#E8C979") },
                new Label { Text = $"{award.PlayerName} · {award.TeamName}", FontSize = 16, FontAttributes = FontAttributes.Bold },
                new Label { Text = award.Summary, FontSize = 12 } } } });
        var scoreColumns = new[] { (Title:"Team",Width:140d) }.Concat(box.Periods.Select(p=>(Title:p.Period<=4?$"Q{p.Period}":$"OT{p.Period-4}",Width:38d)))
            .Append((Title:"Total",Width:56d)).ToArray();
        View[] ScoreRow(string team, bool home) => new View[] {TableText(team,true)}
            .Concat(box.Periods.Select(p=>(View)TableText(p.Period > box.Quarter ? "—" : (home?p.HomePoints:p.AwayPoints).ToString())))
            .Append(TableText((home?box.HomeScore:box.AwayScore).ToString(),true)).ToArray();
        content.Children.Add(DataTable(scoreColumns,[ScoreRow(box.AwayTeamName,false),ScoreRow(box.HomeTeamName,true)]));
        var teamTab = new Button {Text="Team totals",FontSize=12};
        var playerTab = new Button {Text="Player statistics",FontSize=12};
        content.Children.Add(new HorizontalStackLayout {Spacing=8,Children={teamTab,playerTab}});
        var teamHost = new VerticalStackLayout {Spacing=8};
        content.Children.Add(teamHost);
        var totals = new List<View[]>();
        void Total(string label, Func<GameTeamStats,string> value) => totals.Add([TableText(label),TableText(value(box.AwayStats)),TableText(value(box.HomeStats))]);
        Total("Total yards",s=>s.TotalYards.ToString());
        Total("Offensive plays",s=>(s.PassAttempts+s.RushAttempts).ToString());
        Total("Yards / play",s=>Average(s.TotalYards,s.PassAttempts+s.RushAttempts));
        Total("Passing (Cmp–Att)",s=>$"{s.PassCompletions}–{s.PassAttempts}");
        Total("Passing yards",s=>s.PassYards.ToString());
        Total("Rushing (Att–Yds)",s=>$"{s.RushAttempts}–{s.RushYards}");
        Total("Turnovers",s=>s.Turnovers.ToString());
        Total("Sacks allowed",s=>s.SacksAllowed.ToString());
        Total("20+ pass / run",s=>$"{s.ExplosivePasses20} / {s.ExplosiveRuns20}");
        Total("Yards after catch",s=>s.YardsAfterCatch.ToString());
        Total("Field goals",s=>$"{s.FieldGoalsMade}/{s.FieldGoalsAttempted}");
        Total("Extra points",s=>$"{s.ExtraPointsMade}/{s.ExtraPointsAttempted}");
        Total("Punts (Avg)",s=>$"{s.Punts} ({Average(s.PuntYards,s.Punts)})");
        Total("Kick returns / yards",s=>$"{s.KickReturns} / {s.KickReturnYards}");
        Total("Punt returns / yards",s=>$"{s.PuntReturns} / {s.PuntReturnYards}");
        teamHost.Children.Add(SectionTitle("Team comparison","Recorded snaps in this game · sacks count toward rushing losses"));
        teamHost.Children.Add(DataTable([("Statistic",160),(box.AwayTeamName,98),(box.HomeTeamName,98)],totals));
        var team = new Picker {Title="Team",ItemsSource=new[]{"Both teams",box.AwayTeamName,box.HomeTeamName},SelectedIndex=0};
        var category = new Picker {Title="Statistics",ItemsSource=new[]{"Passing","Rushing","Receiving","Defense","Kicking","Punting","Returns"},SelectedIndex=0};
        var filters = new Grid {ColumnSpacing=10,ColumnDefinitions=new ColumnDefinitionCollection{new(GridLength.Star),new(GridLength.Star)}};
        filters.Add(team,0,0);filters.Add(category,1,0);
        var defenseLegend = new Label { Text = "PBU = passes broken up / defended · INT = interceptions · FR = fumble recoveries", FontSize = 11, TextColor = DynastyTheme.Muted, IsVisible = false };
        var players = new ContentView();
        var playerHost = new VerticalStackLayout {Spacing=10,IsVisible=false,Children={filters,defenseLegend,players}};
        content.Children.Add(playerHost);
        void SelectPlayers(bool selected)
        {
            teamHost.IsVisible = !selected; playerHost.IsVisible = selected;
            teamTab.BackgroundColor = selected ? DynastyTheme.Palette("#253E59") : DynastyTheme.Accent;
            playerTab.BackgroundColor = selected ? DynastyTheme.Accent : DynastyTheme.Palette("#253E59");
            teamTab.TextColor = selected ? DynastyTheme.Text : DynastyTheme.Background;
            playerTab.TextColor = selected ? DynastyTheme.Background : DynastyTheme.Text;
        }
        teamTab.Clicked+=(_,_)=>SelectPlayers(false);playerTab.Clicked+=(_,_)=>SelectPlayers(true);SelectPlayers(false);
        void RenderPlayers()
        {
            var selected=category.SelectedItem?.ToString()??"Passing";
            defenseLegend.IsVisible = selected == "Defense";
            var lines=box.Players.Where(p=>(team.SelectedIndex==0||p.TeamName==team.SelectedItem?.ToString())&&CategoryEligible(p,selected)).ToArray();
            if(lines.Length==0) {players.Content=new Label {Text="No recorded statistics in this category yet.",TextColor=DynastyTheme.Muted};return;}
            var columns=CategoryColumns(selected);var keep=Enumerable.Range(0,columns.Length).Where(i=>columns[i].Title is not ("GP" or "YPG")).ToArray();
            var rows=lines.Select(line=> {var cells=CategoryCells(line,selected);cells[0]=TableText(line.PlayerName);cells[1]=TableText(_teamsByName.TryGetValue(line.TeamName,out var school)?school.Abbreviation:line.TeamName);return keep.Select(i=>cells[i]).ToArray();}).ToArray();
            players.Content=DataTable(keep.Select(i=>columns[i].Title=="School"?("School",85d):columns[i]).ToArray(),rows);
        }
        team.SelectedIndexChanged+=(_,_)=>RenderPlayers();category.SelectedIndexChanged+=(_,_)=>RenderPlayers();RenderPlayers();
        var header = new Grid {Padding=new Thickness(14,8),ColumnSpacing=10,ColumnDefinitions=new ColumnDefinitionCollection {new(GridLength.Star),new(GridLength.Auto)}};
        header.Add(heading,0,0);header.Add(close,1,0);
        var layout = new Grid {RowDefinitions=new RowDefinitionCollection {new(GridLength.Auto),new(GridLength.Star)}};
        layout.Add(header,0,0);layout.Add(new ScrollView {Content=content},0,1);page.Content=layout;
        await Navigation.PushModalAsync(page);await closed.Task;
    }
}
