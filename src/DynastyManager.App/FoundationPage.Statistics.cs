using Microsoft.Maui.Layouts;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Presentation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private readonly VerticalStackLayout _statisticsRows = new() { Spacing = 10 };
    private readonly Picker _statisticsSeason = new() { AutomationId="college-stat-season", Title = "Season" };
    private readonly Picker _statisticsScope = new() { AutomationId="college-stat-scope", ItemsSource = new[] { "Your team", "All schools" }, SelectedIndex = 0 };
    private readonly Picker _statisticsCategory = new() { AutomationId="college-stat-category", ItemsSource = PlayerStatCatalog.Categories, SelectedIndex = 0 };
    private bool _updatingStatisticsFilters;
    private int _statisticsPage;
    private string? _statisticsQuery;
    private int _statisticsHeaderColumn = 1;
    private bool _statisticsHeaderDescending = true;

    private View BuildStatisticsSection()
    {
        _statisticsSeason.SelectedIndexChanged += (_, _) => { if (!_updatingStatisticsFilters) RenderStatistics(); };
        _statisticsScope.SelectedIndexChanged += (_, _) => RenderStatistics();
        _statisticsCategory.SelectedIndexChanged += (_, _) => { _statisticsHeaderColumn = 3; _statisticsHeaderDescending = true; RenderStatistics(); };
        return new VerticalStackLayout { Spacing = 10, Children =
        {
            ResponsiveTableHeader(SectionTitle("Player statistics", "Season/career totals · tap a heading to sort · Tracked GP = games with detailed data"),
                new ActionRow { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, Children = { _statisticsSeason, _statisticsScope, _statisticsCategory } }),
            new Button { Text="Advanced team efficiency", AutomationId="college-efficiency", Command=new Command(async()=> {if(_currentDynasty is null)return;var close=new Button{Text="Close"};close.Clicked+=async(_,_)=>await Navigation.PopModalAsync();await Navigation.PushModalAsync(new ContentPage{BackgroundColor=DynastyTheme.Background,Content=new ScrollView{Content=new VerticalStackLayout{Padding=16,Spacing=10,Children={close,FootballEfficiencyView.Create(_currentDynasty.Schedule.Where(g=>g.SeasonYear==_currentDynasty.SeasonYear&&g.GameType is ScheduledGameType.Conference or ScheduledGameType.NonConference),_currentDynasty.UserTeamName)}}}});}) },
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
        var totals = lines.GroupBy(line=>line.PlayerId).Select(PlayerStatTotals.Total);
        var category = _statisticsCategory.SelectedItem as string ?? "Passing";
        totals = totals.Where(line => CategoryEligible(line, category));
        var query = $"{_currentDynasty.DynastyId}/{_statisticsSeason.SelectedItem}/{_statisticsScope.SelectedIndex}/{category}/{_statisticsHeaderColumn}/{_statisticsHeaderDescending}";
        if (_statisticsQuery != query) { _statisticsPage = 0; _statisticsQuery = query; }
        string? HeaderKey(PlayerSeasonStatLine line) => CategorySortValue(line, category, _statisticsHeaderColumn);
        var ordered = TableOrdering.Sort(totals, HeaderKey, _statisticsHeaderDescending).ToArray();
        const int pageSize = 50;
        _statisticsPage = Math.Min(_statisticsPage, Math.Max(0, (ordered.Length - 1) / pageSize));
        var rows = ordered.Skip(_statisticsPage * pageSize).Take(pageSize).Select(line => CategoryCells(line, category)).ToArray();
        _statisticsRows.Children.Add(new Label { Text = $"{ordered.Length} players · {category} · {_statisticsSeason.SelectedItem} · {_statisticsScope.SelectedItem}", TextColor = DynastyTheme.Muted, FontSize = 12 });
        if (category == "Defense") _statisticsRows.Children.Add(new Label { Text = "PBU = passes broken up / defended · recorded even when the defender has no tackles.", FontSize = 11, TextColor = DynastyTheme.Muted });
        if (rows.Length == 0) _statisticsRows.Children.Add(new Label { Text = "No player statistics yet. Current-season totals appear after the first completed game." });
        else _statisticsRows.Children.Add(DataTable(CategoryColumns(category), rows, (column, descending) =>
            {
                _statisticsHeaderColumn = column; _statisticsHeaderDescending = descending; RenderStatistics();
            }, _statisticsHeaderColumn, _statisticsHeaderDescending));
        if (ordered.Length > pageSize)
        {
            var previousPage = new Button { Text = "Previous", IsEnabled = _statisticsPage > 0 };
            var next = new Button { Text = "Next", IsEnabled = (_statisticsPage + 1) * pageSize < ordered.Length };
            previousPage.Clicked += (_, _) => { _statisticsPage--; RenderStatistics(); };
            next.Clicked += (_, _) => { _statisticsPage++; RenderStatistics(); };
            _statisticsRows.Children.Insert(1, new ActionRow { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, Children =
            {
                previousPage, new Label { Text = $"{_statisticsPage * pageSize + 1}–{Math.Min(ordered.Length, (_statisticsPage + 1) * pageSize)} of {ordered.Length}", FontSize = 12, VerticalOptions = LayoutOptions.Center }, next
            } });
        }
        _statisticsRows.Children.Add(new Label { Text = "Current-season totals update after each completed game. Career scope 'Your team' counts seasons played at your school; 'All schools' combines recorded production across transfers.", FontSize = 11, TextColor = DynastyTheme.Muted });
    }

    private static bool CategoryEligible(PlayerSeasonStatLine line, string category) => FootballStatViews.Eligible(line, category);
    private static (string Title, double Width)[] CategoryColumns(string category) => FootballStatViews.Columns(category);
    private View[] CategoryCells(PlayerSeasonStatLine line, string category) => FootballStatViews.Cells(line, category,
        (id, name) => PlayerLink(id, name), team => SchoolCell(team), text => TableText(text), Percent, Average, PasserRating);

    private static string? CategorySortValue(PlayerSeasonStatLine line, string category, int column)
    {
        if (column == 0) return line.PlayerName;
        if (column == 1) return line.TeamName;
        var field=PlayerStatCatalog.Fields[category][column-2];
        var value=field=="Rate"?PasserRatingValue(line):PlayerStatCatalog.Value(line,field);
        return value?.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static double Ratio(int numerator, int denominator) => denominator == 0 ? 0 : (double)numerator / denominator;
    private static string Average(int total, int count) => Ratio(total, count).ToString("F1");
    private static string Percent(int made, int attempts) => (Ratio(made, attempts) * 100).ToString("F1");
    private static double PasserRatingValue(PlayerSeasonStatLine line) => line.PassAttempts == 0 ? 0 :
        (8.4 * line.PassYards + 330 * line.PassingTouchdowns + 100 * line.PassCompletions - 200 * line.InterceptionsThrown) / line.PassAttempts;
    private static string PasserRating(PlayerSeasonStatLine line) => PasserRatingValue(line).ToString("F1");

    private async Task ShowPlayerStatisticsAsync(DynastyPlayer player)
    {
        PlayerSeasonStatLine[] lines = _currentDynasty is null ? [] : VisiblePlayerStatistics()
            .Where(line => line.PlayerId == player.PlayerId).OrderBy(line => line.SeasonYear).ToArray();
        var close = new Button { Text = "‹ Back", BackgroundColor = Colors.Transparent, TextColor = DynastyTheme.Accent };
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        var content = new VerticalStackLayout { Spacing = 12, Padding = 14, Children =
        {
            close, SchoolBanner(player.TeamName, $"{player.Position.DisplayName()} {player.FullName} · {FormatOriginalRecruitingStars(player)}"),
            SectionTitle("Career statistics", $"{lines.Length} recorded seasons · {lines.Sum(s => s.PassYards + s.RushYards):N0} yards · {lines.Sum(s => s.Touchdowns)} TD")
        } };
        content.Children.Add(PlayerEfficiencyView.Create(VisiblePlayerStatistics(),player.PlayerId));
        if (lines.Length == 0) content.Children.Add(new Label { Text = "No statistics recorded yet. Current-season totals appear after the player's team completes a game." });
        else
        {
            foreach (var category in PlayerStatCatalog.Categories)
            {
                var categoryLines = lines.Where(line => CategoryEligible(line, category)).ToArray();
                if (categoryLines.Length == 0) continue;
                content.Children.Add(SectionTitle(category, $"{categoryLines.Length} recorded season{(categoryLines.Length == 1 ? string.Empty : "s")}"));
                var columns = new[] { ("Season", 70d) }.Concat(CategoryColumns(category)).ToArray();
                var rows = categoryLines.Select(line => new[] { TableText(line.SeasonYear.ToString()) }
                    .Concat(CategoryCells(line, category)).ToArray());
                content.Children.Add(DataTable(columns, rows));
            }
        }
        content.Children.Add(new Label { Text = "Current-season totals update after every completed game. Offense, defense, kicking, punting and return production are retained by season and career.", FontSize = 11, TextColor = DynastyTheme.Muted });
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
