using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Presentation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private readonly VerticalStackLayout _statisticsRows = new() { Spacing = 10 };
    private readonly Picker _statisticsSeason = new() { Title = "Season" };
    private readonly Picker _statisticsScope = new() { ItemsSource = new[] { "Your team", "All schools" }, SelectedIndex = 0 };
    private bool _updatingStatisticsFilters;
    private int _statisticsPage;
    private string? _statisticsQuery;
    private int _statisticsHeaderColumn = 1;
    private bool _statisticsHeaderDescending = true;

    private View BuildStatisticsSection()
    {
        _statisticsSeason.SelectedIndexChanged += (_, _) => { if (!_updatingStatisticsFilters) RenderStatistics(); };
        _statisticsScope.SelectedIndexChanged += (_, _) => RenderStatistics();
        return new VerticalStackLayout { Spacing = 10, Children =
        {
            ResponsiveTableHeader(SectionTitle("Player statistics", "Season/career totals · tap a column heading to sort ↑↓"),
                new HorizontalStackLayout { Spacing = 8, Children = { _statisticsSeason, _statisticsScope } }),
            _statisticsRows
        } };
    }

    private void RenderStatistics()
    {
        if (_currentDynasty is null) return;
        var visibleStats = VisiblePlayerStatistics();
        var options = new[] { "Career totals" }.Concat(visibleStats.Select(s => s.SeasonYear)
            .Distinct().OrderDescending().Select(year => year.ToString())).ToArray();
        if (_statisticsSeason.ItemsSource is not string[] previous || !previous.SequenceEqual(options))
        {
            _updatingStatisticsFilters = true;
            var selected = _statisticsSeason.SelectedItem as string;
            _statisticsSeason.ItemsSource = options;
            _statisticsSeason.SelectedIndex = Math.Max(0, Array.IndexOf(options, selected));
            _updatingStatisticsFilters = false;
        }
        _statisticsRows.Children.Clear();
        var lines = visibleStats.Where(line => _statisticsScope.SelectedIndex == 1 || line.TeamName == _currentDynasty.UserTeamName);
        if (int.TryParse(_statisticsSeason.SelectedItem as string, out var year)) lines = lines.Where(line => line.SeasonYear == year);
        var totals = lines.GroupBy(line => line.PlayerId).Select(group =>
        {
            var latest = group.OrderByDescending(line => line.SeasonYear).First();
            return latest with { Games = group.Sum(s => s.Games), PassYards = group.Sum(s => s.PassYards),
                RushYards = group.Sum(s => s.RushYards), Receptions = group.Sum(s => s.Receptions),
                ReceivingYards = group.Sum(s => s.ReceivingYards), Tackles = group.Sum(s => s.Tackles),
                Sacks = group.Sum(s => s.Sacks), Interceptions = group.Sum(s => s.Interceptions),
                Touchdowns = group.Sum(s => s.Touchdowns), Turnovers = group.Sum(s => s.Turnovers) };
        });
        var query = $"{_currentDynasty.DynastyId}/{_statisticsSeason.SelectedItem}/{_statisticsScope.SelectedIndex}/{_statisticsHeaderColumn}/{_statisticsHeaderDescending}";
        if (_statisticsQuery != query) { _statisticsPage = 0; _statisticsQuery = query; }
        string? HeaderKey(PlayerSeasonStatLine line) => _statisticsHeaderColumn switch
        {
            0 => line.PlayerName, 1 => line.ScrimmageYards.ToString(System.Globalization.CultureInfo.InvariantCulture),
            2 => line.Touchdowns.ToString(), 3 => line.TeamName, 4 => line.Position.ToString(), 5 => line.Games.ToString(),
            6 => line.PassYards.ToString(System.Globalization.CultureInfo.InvariantCulture), 7 => line.RushYards.ToString(System.Globalization.CultureInfo.InvariantCulture),
            8 => line.ReceivingYards.ToString(), 9 => line.Tackles.ToString(), 10 => line.Sacks.ToString(),
            11 => line.Interceptions.ToString(), 12 => line.Turnovers.ToString(), 13 => line.OriginalRecruitingStars?.ToString(), _ => line.PlayerName
        };
        var ordered = TableOrdering.Sort(totals, HeaderKey, _statisticsHeaderDescending).ToArray();
        const int pageSize = 50;
        _statisticsPage = Math.Min(_statisticsPage, Math.Max(0, (ordered.Length - 1) / pageSize));
        var rows = ordered.Skip(_statisticsPage * pageSize).Take(pageSize).Select(line => new View[]
        {
            PlayerLink(line.PlayerId, line.PlayerName),
            TableText(line.ScrimmageYards.ToString("N0"), true, Color.FromArgb("#8FD8C1")),
            TableText(line.Touchdowns.ToString(), true, Color.FromArgb("#E9C76C")), SchoolCell(line.TeamName),
            TableText(line.Position.ToString()), TableText(line.Games.ToString()),
            TableText(line.PassYards.ToString("N0")), TableText(line.RushYards.ToString("N0")),
            TableText(line.ReceivingYards.ToString("N0")), TableText(line.Tackles.ToString()), TableText(line.Sacks.ToString()),
            TableText(line.Interceptions.ToString()), TableText(line.Turnovers.ToString()),
            TableText(line.OriginalRecruitingStars is int stars ? $"{stars}★" : "—")
        }).ToArray();
        _statisticsRows.Children.Add(new Label { Text = $"{ordered.Length} players · {_statisticsSeason.SelectedItem} · {_statisticsScope.SelectedItem}", TextColor = DynastyTheme.Muted, FontSize = 12 });
        if (rows.Length == 0) _statisticsRows.Children.Add(new Label { Text = "No player statistics yet. Current-season totals appear after the first completed game." });
        else _statisticsRows.Children.Add(DataTable([("Player", 165), ("Yards", 70), ("TD", 45), ("School", 195),
            ("Pos", 45), ("GP", 45), ("Pass yds", 80), ("Rush yds", 80), ("Rec yds", 75),
            ("Tkl", 50), ("Sack", 50), ("INT", 45), ("TO", 45), ("HS", 45)], rows, (column, descending) =>
            {
                _statisticsHeaderColumn = column; _statisticsHeaderDescending = descending; RenderStatistics();
            }, _statisticsHeaderColumn, _statisticsHeaderDescending));
        if (ordered.Length > pageSize)
        {
            var previousPage = new Button { Text = "Previous", IsEnabled = _statisticsPage > 0 };
            var next = new Button { Text = "Next", IsEnabled = (_statisticsPage + 1) * pageSize < ordered.Length };
            previousPage.Clicked += (_, _) => { _statisticsPage--; RenderStatistics(); };
            next.Clicked += (_, _) => { _statisticsPage++; RenderStatistics(); };
            _statisticsRows.Children.Insert(1, new HorizontalStackLayout { Spacing = 8, Children =
            {
                previousPage, new Label { Text = $"{_statisticsPage * pageSize + 1}–{Math.Min(ordered.Length, (_statisticsPage + 1) * pageSize)} of {ordered.Length}", FontSize = 12, VerticalOptions = LayoutOptions.Center }, next
            } });
        }
        _statisticsRows.Children.Add(new Label { Text = "Yards = passing + rushing. Career scope 'Your team' counts seasons played at your school; 'All schools' combines each player's recorded career. School shows the latest recorded team in that selection.", FontSize = 11, TextColor = DynastyTheme.Muted });
    }

    private async Task ShowPlayerStatisticsAsync(DynastyPlayer player)
    {
        PlayerSeasonStatLine[] lines = _currentDynasty is null ? [] : VisiblePlayerStatistics()
            .Where(line => line.PlayerId == player.PlayerId).OrderBy(line => line.SeasonYear).ToArray();
        var close = new Button { Text = "‹ Back", BackgroundColor = Colors.Transparent, TextColor = DynastyTheme.Accent };
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        var content = new VerticalStackLayout { Spacing = 12, Padding = 14, Children =
        {
            close, SchoolBanner(player.TeamName, $"{player.Position} {player.FullName} · {FormatOriginalRecruitingStars(player)}"),
            SectionTitle("Career statistics", $"{lines.Length} recorded seasons · {lines.Sum(s => s.PassYards + s.RushYards):N0} yards · {lines.Sum(s => s.Touchdowns)} TD")
        } };
        if (lines.Length == 0) content.Children.Add(new Label { Text = "No statistics recorded yet. Current-season totals appear after the player's team completes a game." });
        else
        {
            var rows = lines.Select(line => new View[] { TableText(line.SeasonYear.ToString()), SchoolCell(line.TeamName),
                TableText(line.Games.ToString()), TableText(line.PassYards.ToString("N0")), TableText(line.RushYards.ToString("N0")),
                TableText(line.ReceivingYards.ToString("N0")), TableText(line.Tackles.ToString()), TableText(line.Sacks.ToString()),
                TableText(line.Interceptions.ToString()), TableText(line.Touchdowns.ToString()), TableText(line.Turnovers.ToString()) }).ToList();
            rows.Add([TableText("Career", true), TableText("All schools", true), TableText(lines.Sum(s => s.Games).ToString(), true),
                TableText(lines.Sum(s => s.PassYards).ToString("N0"), true), TableText(lines.Sum(s => s.RushYards).ToString("N0"), true),
                TableText(lines.Sum(s => s.ReceivingYards).ToString("N0"), true), TableText(lines.Sum(s => s.Tackles).ToString(), true),
                TableText(lines.Sum(s => s.Sacks).ToString(), true), TableText(lines.Sum(s => s.Interceptions).ToString(), true),
                TableText(lines.Sum(s => s.Touchdowns).ToString(), true), TableText(lines.Sum(s => s.Turnovers).ToString(), true)]);
            content.Children.Add(DataTable([("Season", 70), ("School", 195), ("GP", 45), ("Pass yds", 80), ("Rush yds", 80),
                ("Rec yds", 75), ("Tkl", 50), ("Sack", 50), ("INT", 45), ("TD", 45), ("TO", 45)], rows));
        }
        content.Children.Add(new Label { Text = "Current-season totals update after every completed game. Passing, rushing and receiving production plus defensive tackles, sacks and interceptions are retained by season and career.", FontSize = 11, TextColor = DynastyTheme.Muted });
        await Navigation.PushModalAsync(new ContentPage { Content = new ScrollView { Content = content } });
    }

    private IReadOnlyList<PlayerSeasonStatLine> VisiblePlayerStatistics()
    {
        if (_currentDynasty is null) return [];
        var current = DynastyHistoryService.BuildCurrentPlayerStats(_currentDynasty);
        return _currentDynasty.PlayerSeasonStats
            .Where(line => line.SeasonYear != _currentDynasty.SeasonYear)
            .Concat(current)
            .ToArray();
    }
}
