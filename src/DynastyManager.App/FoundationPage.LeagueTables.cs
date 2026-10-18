using DynastyManager.Core.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private readonly Picker _rankingsRange = new() { ItemsSource = new[] { "Top 25", "All schools" }, SelectedIndex = 0 };
    private readonly Picker _scheduleVenue = new() { ItemsSource = new[] { "All games", "Home games", "Away games" }, SelectedIndex = 0 };
    private readonly ContentView _scheduleBanner = new();

    private View BuildOrganizedScheduleSection()
    {
        _scheduleVenue.SelectedIndexChanged += (_, _) => RenderUserSchedule();
        return new VerticalStackLayout { Spacing = 10, Children =
        {
            ResponsiveTableHeader(_scheduleBanner, new VerticalStackLayout { Spacing = 8, Children =
            {
                SectionTitle("Season schedule", "Opponent colors identify each school · W/L and scores remain explicit."), _scheduleVenue
            } }), _scheduleList
        } };
    }

    private View BuildOrganizedRankingsSection()
    {
        _rankingsRange.SelectedIndexChanged += (_, _) => RenderNationalRankings();
        View[] panels =
        [
            new VerticalStackLayout { Spacing = 10, Children = { ResponsiveTableHeader(
                new VerticalStackLayout { Spacing = 6, Children = { SectionTitle("National rankings", "School colors · records · scoring · ranking score"), _nationalRankingStatus } },
                _rankingsRange), _nationalRankingsList } },
            new VerticalStackLayout { Spacing = 10, IsVisible = false, Children = { SectionTitle("Conference race", "Conference record, overall record and conference point differential"), _conferenceChampionshipStatus, _conferenceStandingsList } },
            new VerticalStackLayout { Spacing = 10, IsVisible = false, Children = { SectionTitle("Postseason", "Playoff field, bracket results and bowl games"), _postseasonStatus, _postseasonList } }
        ];
        var tabs = new Grid { ColumnSpacing = 6, ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) } };
        var buttons = new List<Button>();
        foreach (var (name, index) in new[] { ("National", 0), ("Conference", 1), ("Postseason", 2) })
        {
            var button = new Button { Text = name, HorizontalOptions = LayoutOptions.Fill, FontSize = 12,
                BackgroundColor = index == 0 ? Color.FromArgb("#31597D") : Color.FromArgb("#253342") };
            buttons.Add(button);
            button.Clicked += (_, _) => _selectRankingsPanel?.Invoke(index);
            tabs.Add(button, index, 0);
        }
        _selectRankingsPanel = index =>
        {
            for (var item = 0; item < panels.Length; item++)
            {
                panels[item].IsVisible = item == index;
                buttons[item].BackgroundColor = Color.FromArgb(item == index ? "#31597D" : "#253342");
            }
        };
        return new VerticalStackLayout { Spacing = 12, Children = { tabs, panels[0], panels[1], panels[2] } };
    }

    private void RenderScheduleGrid()
    {
        _scheduleList.Children.Clear();
        if (_currentDynasty is null) return;
        var team = _currentDynasty.UserTeamName;
        var games = _currentDynasty.Schedule.Where(g => g.SeasonYear == _currentDynasty.SeasonYear && g.InvolvesTeam(team)).OrderBy(g => g.Week).ToArray();
        var played = games.Where(g => g.HasPlayed).ToArray();
        var wins = played.Count(g => g.WinnerTeamName == team);
        _scheduleBanner.Content = SchoolBanner(team, $"{_currentDynasty.SeasonYear} season · {wins}-{played.Length - wins} · {games.Length} scheduled games");
        var rows = new List<View[]>();
        foreach (var game in games)
        {
            var home = game.HomeTeamName.Equals(team, StringComparison.OrdinalIgnoreCase);
            if (_scheduleVenue.SelectedIndex == 1 && !home || _scheduleVenue.SelectedIndex == 2 && home) continue;
            var result = "Upcoming";
            var resultColor = DynastyTheme.Muted;
            if (game.HasPlayed && game.HomeScore is int homeScore && game.AwayScore is int awayScore)
            {
                var userScore = home ? homeScore : awayScore;
                var otherScore = home ? awayScore : homeScore;
                result = $"{(userScore > otherScore ? "W" : "L")} {userScore}-{otherScore}";
                resultColor = userScore > otherScore ? PositiveChangeColor : NegativeChangeColor;
            }
            var type = game.GameType switch { ScheduledGameType.Conference => "CONF", ScheduledGameType.ConferenceChampionship => "CCG",
                ScheduledGameType.CollegeFootballPlayoff => "CFP", ScheduledGameType.Bowl => "BOWL", _ => "OOC" };
            var stats = home ? game.HomeStats : game.AwayStats;
            rows.Add([TableText(game.Week.ToString(), game.Week == _currentDynasty.Week), SchoolCell(home ? game.AwayTeamName : game.HomeTeamName),
                TableText(result, true, resultColor), TableText(home ? "Home" : "Away"), TableText(type),
                TableText(stats?.TotalYards.ToString("N0") ?? "—"), TableText(stats?.Turnovers.ToString() ?? "—")]);
        }
        if (rows.Count == 0) _scheduleList.Children.Add(new Label { Text = "No games match this selection." });
        else _scheduleList.Children.Add(DataTable([("Week", 45), ("Opponent", 195), ("Result", 100), ("Venue", 60), ("Type", 55), ("Yards", 65), ("TO", 45)], rows));
    }

    private View CreateTeamRosterTable(IEnumerable<DynastyPlayer> roster) => DataTable(
        [("Pos", 45), ("Player", 172), ("OVR", 54), ("Year", 50), ("Potential", 75), ("HS", 45), ("Profile", 110)],
        roster.OrderBy(p => p.Position).ThenBy(p => p.DepthChartOrder).Select(player => new View[]
        {
            TableText(player.Position.ToString()), PlayerLink(player), RatingCell(player.OverallRating),
            TableText(FormatClassYear(player.ClassYear, player.IsRedshirted || player.HasRedshirted)), TableText(FormatPotentialStars(player.PotentialRating)),
            TableText(player.OriginalRecruitingStars is int stars ? $"{stars}★" : "—"), CreateCareerStatsButton(player)
        }));
}
