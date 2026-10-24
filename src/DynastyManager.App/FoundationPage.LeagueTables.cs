using Microsoft.Maui.Layouts;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private readonly Picker _rankingsRange = new() { ItemsSource = new[] { "Top 25", "All schools" }, SelectedIndex = 0 };
    private readonly Picker _scheduleVenue = new() { ItemsSource = new[] { "All games", "Home games", "Away games" }, SelectedIndex = 0 };
    private readonly Picker _scheduleScope = new() { ItemsSource = new[] { "Your team", "All teams" }, SelectedIndex = 0 };
    private readonly Picker _scheduleWeek = new() { Title = "Week", SelectedIndex = 0 };
    private readonly Picker _conferencePicker = new() { Title = "Conference" };
    private readonly ContentView _scheduleBanner = new();
    private bool _updatingScheduleWeeks;
    private bool _defaultLeagueWeek;

    private View BuildOrganizedScheduleSection()
    {
        _scheduleVenue.SelectedIndexChanged += (_, _) => RenderUserSchedule();
        _scheduleScope.SelectedIndexChanged += (_, _) =>
        {
            _defaultLeagueWeek = _scheduleScope.SelectedIndex == 1;
            RenderUserSchedule();
        };
        _scheduleWeek.SelectedIndexChanged += (_, _) => { if (!_updatingScheduleWeeks) RenderUserSchedule(); };
        return new VerticalStackLayout { Spacing = 10, Children =
        {
            ResponsiveTableHeader(_scheduleBanner, new VerticalStackLayout { Spacing = 8, Children =
            {
                SectionTitle("Season schedule", "Browse your schedule or every game and result by week."),
                new ActionRow { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, Children = { _scheduleScope, _scheduleWeek, _scheduleVenue } }
            } }), _scheduleList
        } };
    }

    private View BuildOrganizedRankingsSection()
    {
        _rankingsRange.SelectedIndexChanged += (_, _) => RenderNationalRankings();
        _pollKind.SelectedIndexChanged += (_, _) => RenderNationalRankings();
        _conferencePicker.SelectedIndexChanged += (_, _) => RenderConferenceRace();
        View[] panels =
        [
            new VerticalStackLayout { Spacing = 10, Children = { ResponsiveTableHeader(
                new VerticalStackLayout { Spacing = 6, Children = { SectionTitle("National rankings", "School colors · records · scoring · ranking score"), _nationalRankingStatus } },
                new ActionRow { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, Children = { _pollKind, _rankingsRange } }), _nationalRankingsList } },
            new VerticalStackLayout { Spacing = 10, IsVisible = false, Children =
            {
                ResponsiveTableHeader(
                    SectionTitle("Conference race", "Choose any conference · conference record, overall record and point differential"),
                    _conferencePicker),
                _conferenceChampionshipStatus, _conferenceStandingsList
            } },
            new VerticalStackLayout { Spacing = 10, IsVisible = false, Children = { SectionTitle("Postseason", "Playoff field, bracket results and bowl games"), _postseasonStatus, _postseasonList } }
        ];
        var tabs = new Grid { ColumnSpacing = 6, ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) } };
        var buttons = new List<Button>();
        foreach (var (name, index) in new[] { ("National", 0), ("Conference", 1), ("Postseason", 2) })
        {
            var button = new Button { Text = name, HorizontalOptions = LayoutOptions.Fill, FontSize = 12,
                BackgroundColor = index == 0 ? DynastyTheme.Palette("#31597D") : DynastyTheme.Palette("#253342") };
            buttons.Add(button);
            button.Clicked += (_, _) => _selectRankingsPanel?.Invoke(index);
            tabs.Add(button, index, 0);
        }
        _selectRankingsPanel = index =>
        {
            for (var item = 0; item < panels.Length; item++)
            {
                panels[item].IsVisible = item == index;
                buttons[item].BackgroundColor = DynastyTheme.Palette(item == index ? "#31597D" : "#253342");
            }
        };
        return new VerticalStackLayout { Spacing = 12, Children = { tabs, panels[0], panels[1], panels[2] } };
    }

    private void RenderScheduleGrid()
    {
        _scheduleList.Children.Clear();
        if (_currentDynasty is null) return;
        var team = _currentDynasty.UserTeamName;
        var seasonGames = _currentDynasty.Schedule.Where(g => g.SeasonYear == _currentDynasty.SeasonYear).ToArray();
        var weekValues = seasonGames.Select(game => game.Week).Append(_currentDynasty.Week).Distinct().Order().ToArray();
        var weekOptions = new[] { "All weeks" }.Concat(weekValues.Select(FormatScheduleWeek)).ToArray();
        if (_scheduleWeek.ItemsSource is not string[] oldWeeks || !oldWeeks.SequenceEqual(weekOptions))
        {
            _updatingScheduleWeeks = true;
            var prior = _scheduleWeek.SelectedItem as string;
            _scheduleWeek.ItemsSource = weekOptions;
            _scheduleWeek.SelectedIndex = Math.Max(0, Array.IndexOf(weekOptions, prior));
            _updatingScheduleWeeks = false;
        }
        if (_defaultLeagueWeek)
        {
            _updatingScheduleWeeks = true;
            var week = _currentDynasty.Week;
            _scheduleWeek.SelectedIndex = Math.Max(0, Array.IndexOf(weekValues, week) + 1);
            _defaultLeagueWeek = false;
            _updatingScheduleWeeks = false;
        }
        var selectedWeek = _scheduleWeek.SelectedIndex > 0 ? weekValues[_scheduleWeek.SelectedIndex - 1] : (int?)null;
        var leagueWide = _scheduleScope.SelectedIndex == 1;
        var games = seasonGames.Where(game => (leagueWide || game.InvolvesTeam(team)) &&
                (selectedWeek is null || game.Week == selectedWeek))
            .OrderBy(game => game.Week).ThenBy(game => game.GameType).ThenBy(game => game.HomeTeamName).ToArray();
        var userGames = seasonGames.Where(game => game.InvolvesTeam(team)).ToArray();
        var played = userGames.Where(g => g.HasPlayed).ToArray();
        var wins = played.Count(g => g.WinnerTeamName == team);
        _scheduleBanner.Content = SchoolBanner(team, leagueWide
            ? $"{_currentDynasty.SeasonYear} league scoreboard · {games.Length} games shown"
            : $"{_currentDynasty.SeasonYear} season · {wins}-{played.Length - wins} · {userGames.Length} scheduled games");
        var rows = new List<View[]>();
        foreach (var game in games)
        {
            var home = game.HomeTeamName.Equals(team, StringComparison.OrdinalIgnoreCase);
            if (!leagueWide && (_scheduleVenue.SelectedIndex == 1 && !home || _scheduleVenue.SelectedIndex == 2 && home)) continue;
            var result = "Upcoming";
            var resultColor = DynastyTheme.Muted;
            if (game.HasPlayed && game.HomeScore is int homeScore && game.AwayScore is int awayScore)
            {
                if (leagueWide)
                {
                    result = $"{awayScore}-{homeScore}";
                    resultColor = DynastyTheme.Palette("#E6EBF1");
                }
                else
                {
                    var userScore = home ? homeScore : awayScore;
                    var otherScore = home ? awayScore : homeScore;
                    result = $"{(userScore > otherScore ? "W" : "L")} {userScore}-{otherScore}";
                    resultColor = userScore > otherScore ? PositiveChangeColor : NegativeChangeColor;
                }
            }
            var type = game.GameType switch { ScheduledGameType.Conference => "CONF", ScheduledGameType.ConferenceChampionship => "CCG",
                ScheduledGameType.CollegeFootballPlayoff => "CFP", ScheduledGameType.Bowl => "BOWL", _ => "OOC" };
            var stats = home ? game.HomeStats : game.AwayStats;
            rows.Add([TableText(FormatScheduleWeek(game.Week), game.Week == _currentDynasty.Week),
                leagueWide ? SchoolCell(game.AwayTeamName) : SchoolCell(home ? game.AwayTeamName : game.HomeTeamName),
                leagueWide ? SchoolCell(game.HomeTeamName) : TableText(home ? "Home" : "Away"),
                TableText(result, true, resultColor), TableText(type),
                TableText(stats?.TotalYards.ToString("N0") ?? "—"), TableText(stats?.Turnovers.ToString() ?? "—"), GameDetailCell(game)]);
        }
        if (rows.Count == 0) _scheduleList.Children.Add(new Label { Text = "No games match this selection." });
        else _scheduleList.Children.Add(DataTable(leagueWide
            ? [("Week", 95), ("Away", 195), ("Home", 195), ("Score", 90), ("Type", 55), ("Yards", 65), ("TO", 45), ("Game center", 230)]
            : [("Week", 95), ("Opponent", 195), ("Venue", 60), ("Result", 100), ("Type", 55), ("Yards", 65), ("TO", 45), ("Game center", 230)], rows));
    }

    private static string FormatScheduleWeek(int week) => week switch
    {
        CollegeFootballPlayoffService.FirstRoundWeek => "CFP First Round",
        CollegeFootballPlayoffService.QuarterfinalWeek => "CFP Quarterfinal",
        CollegeFootballPlayoffService.SemifinalWeek => "CFP Semifinal",
        CollegeFootballPlayoffService.NationalChampionshipWeek => "CFP Final",
        14 => "Championship Week",
        _ => $"Week {week}"
    };

    private View CreateTeamRosterTable(IEnumerable<DynastyPlayer> roster) => DataTable(
        [("Pos", 45), ("Player", 172), ("OVR", 54), ("Δ", 45), ("Morale", 60), ("Year", 50), ("Potential", 75), ("HS", 45), ("Profile", 110), ("Height", 64), ("Wt lb", 60)],
        roster.OrderBy(p => p.Position).ThenBy(p => p.DepthChartOrder).Select(player => new View[]
        {
            TableText(OffensiveLineService.Label(player)), PlayerLink(player), RatingCell(player.OverallRating),
            TableText(FormatSigned(GameMoraleService.SeasonDelta(_currentDynasty!, player))), MoraleIcon(player),
            TableText(FormatClassYear(player.ClassYear, player.IsRedshirted || player.HasRedshirted)), TableText(FormatPotentialStars(player.PotentialRating)),
            TableText(player.OriginalRecruitingStars is int stars ? $"{(player.RecruitingStarsEstimated ? "~" : "")}{stars}★" : "—"), CreateCareerStatsButton(player),
            HeightCell(player.HeightInches), TableText(player.WeightPounds > 0 ? player.WeightPounds.ToString() : "—")
        }));
}
